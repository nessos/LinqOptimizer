namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent
    open System.Collections.ObjectModel

    // 1st step : Escape analysis, find aliases, remove wierd lambdas and keep possible mappings.
    // 2nd step : For tuples that do not escape modify construction site and .Item calls.

    type private Parameter = {
        Expr            : ParameterExpression
        Aliases         : List<Parameter>
        mutable Escapes : bool
        MemberMappings  : Dictionary<Expression, ParameterExpression> } 
    with 
        static member create (expr : ParameterExpression) =
            {Expr = expr; Aliases = new List<_>(); Escapes = false; MemberMappings = new Dictionary<_,_>() }

        static member aliasesEscape (parameter : Parameter) =
            parameter.Aliases |> Seq.exists (fun p -> p.Escapes || Parameter.aliasesEscape p)

    [<AutoOpen>]
    module private Helpers =
    
            let tupleTypes = [ typedefof<Tuple>
                               typedefof<Tuple<_,_>>
                               typedefof<Tuple<_,_,_>>
                               typedefof<Tuple<_,_,_,_>>
                               typedefof<Tuple<_,_,_,_,_>>
                               typedefof<Tuple<_,_,_,_,_,_>>
                               typedefof<Tuple<_,_,_,_,_,_,_>>
                               typedefof<Tuple<_,_,_,_,_,_,_,_>>
                             ]

            let specialTupleName = "tupledArg"

            let isTupleType (ty : Type) = 
                ty.IsGenericType 
                && tupleTypes.Contains(ty.GetGenericTypeDefinition())

            let isWildcardParameter (expr : ParameterExpression) =
                expr.Name.StartsWith "_arg" && 
                    match Int32.TryParse(expr.Name.Substring(4)) with | b, _ -> b


            // Check if $tupledArg.Item$i
            let (|SpecialTupleArgExpression|_|) (expr : Expression) = 
                match expr with
                | :? MemberExpression as mexpr -> 
                    match mexpr.Expression with
                    | :? ParameterExpression as param ->
                        let ty = param.Type.GetGenericTypeDefinition()
                        if param.Name = specialTupleName && tupleTypes.Contains(ty) then
                            Some(param, mexpr.Member)
                        else
                            None
                    | _ -> None
                | _ -> None

            // Check if $id = new Tuple($e1, ...., $en)
            let (|TupleAssignment|_|) (expr : Expression) =
                match expr with
                | :? BinaryExpression as expr 
                    when expr.NodeType = ExpressionType.Assign 
                         && expr.Left.NodeType = ExpressionType.Parameter
                         && expr.Right.NodeType = ExpressionType.New ->
                    let left = expr.Left :?> ParameterExpression
                    let right = expr.Right :?> NewExpression
                    if isTupleType left.Type then
                        Some (left, right.Arguments)
                    else None
                | _ -> None

    type private EscapeAnalysisVisitor () =
        inherit ExpressionVisitor() with

            let parameters = new List<Parameter>()

            member this.Parameters with get () = parameters

            // Populate parameters list
            override this.VisitBlock(expr : BlockExpression) =
                let blockVars = expr.Variables
                blockVars |> Seq.filter (fun v -> isTupleType v.Type)
                          |> Seq.iter   (fun v -> parameters.Add(Parameter.create(v)))

                Expression.Block(blockVars, this.Visit expr.Expressions) :> _

            // Remova the weird lambdas and keep mappings
            // Looking for pattern ($id => %body).Invoke($var.Item$i)
            // and substitute with %body
            override this.VisitMethodCall(expr : MethodCallExpression) =

                //default case
                let pass () = expr.Update(this.Visit expr.Object, this.Visit expr.Arguments) :> Expression

                if expr.Object <> null then
                    match expr.Object.NodeType with
                    | ExpressionType.Lambda when Seq.length expr.Arguments = 1 ->
                        //Diagnostics.Debugger.Break()
                        
                        let lambda = expr.Object :?> LambdaExpression
                        let arg = expr.Arguments.Single() // $var.Item$i, $var
                        let param = lambda.Parameters.Single() // the mapping ($id)
                        
                        let funcTy = typedefof<Func<_,_>>.MakeGenericType([| arg.Type; lambda.ReturnType |])
                        let invoke = funcTy.GetMethod("Invoke")
                        let isInvoke = expr.Method = invoke

                        if isInvoke then 
                            this.Visit(Expression.Block([param], Expression.Assign(param, arg), this.Visit(lambda.Body)))
                        else pass ()
                    | _ ->  
                        pass()
                else
                    pass()

            // Check for aliases %alias = %parameter
            override this.VisitBinary(expr : BinaryExpression) =
                match expr.NodeType with
                | ExpressionType.Assign when isTupleType expr.Left.Type && (expr.Left :? ParameterExpression) && (expr.Right :? ParameterExpression) ->
                    let left  = expr.Left  :?> ParameterExpression
                    let right = expr.Right :?> ParameterExpression
                    let lpar = parameters.Find(fun p -> p.Expr = left)
                    let rpar = parameters.Find(fun p -> p.Expr = right)
                    rpar.Aliases.Add(lpar)
                    expr :> _
                | ExpressionType.Assign when isTupleType expr.Left.Type && (expr.Left :? ParameterExpression) ->
                    // left does not escape
                    //Diagnostics.Debugger.Break()
                    expr.Update(expr.Left, null, this.Visit(expr.Right)) :> _
                | _ -> 
                    expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> _

            // Standalone appearance of ParameterExpression implies escape.
            override this.VisitParameter(expr : ParameterExpression) =
                //Diagnostics.Debugger.Break()
                
                match Seq.tryFind (fun p -> p.Expr = expr) parameters with
                | Some param -> 
                    //Diagnostics.Debugger.Break()
                    param.Escapes <- true
                | None -> ()
                expr :> _

            // &tuple.Item$i 
            override this.VisitMember(expr : MemberExpression) =
                //Diagnostics.Debugger.Break()
                
                expr.Update(expr.Expression) :> _


    type private TupleEliminationVisitor (parameters : List<Parameter>) =
        inherit ExpressionVisitor() with

            let parameterExprs = parameters |> Seq.map(fun p -> p.Expr) |> Seq.toArray

            override this.VisitBlock(expr : BlockExpression) =
                let blockVars = expr.Variables |> Seq.filter(fun v -> not(parameterExprs.Contains(v))) |> Seq.toArray
                Expression.Block(blockVars, this.Visit(expr.Expressions)) :> _


    module TupleElimination =
        let apply(expr : Expression) =
            let te = new EscapeAnalysisVisitor()
            let expr = te.Visit(expr)
            let ps = te.Parameters
            let pps = te.Parameters.Select(fun p -> Parameter.aliasesEscape p).ToArray()
            expr