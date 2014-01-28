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
        mutable Alias   : Parameter option
        mutable Source  : Parameter option
        mutable Escapes : bool
        MemberMappings  : List<Expression>            // mappings to tuple ctor arguments
        MemberBindings  : List<ParameterExpression> } // mappings from ctor arguments to variables they're bound to.
    with 
        static member create (expr : ParameterExpression) =
            {Expr = expr; Alias = None; Escapes = false; Source = None; MemberMappings = new List<_>(); MemberBindings = new List<_>() }

        static member escapes (parameter : Parameter) =
            match parameter.Alias, parameter.Source with
            | Some alias, Some source -> parameter.Escapes ||  alias  .Escapes
            | None, Some source       -> parameter.Escapes ||  Parameter.escapes source
            | Some alias, None        -> parameter.Escapes ||  alias  .Escapes
            | None, None              -> parameter.Escapes

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

            // Check if $id = new Tuple($e1, ...., $en)
            let (|TupleNewAssignment|_|) (expr : Expression) =
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

            let (|TupleAssignment|_|) (expr : Expression) =
                match expr with
                | :? BinaryExpression as expr 
                    when expr.NodeType = ExpressionType.Assign 
                         && expr.Left.NodeType  = ExpressionType.Parameter
                         && expr.Right.NodeType = ExpressionType.Parameter
                         && isTupleType expr.Left.Type ->
                    Some(expr.Left :?> ParameterExpression, expr.Right :?> ParameterExpression)
                | _ -> None

            // $foo.Item$id -> $id
            let getItemPosition(expr : MemberExpression) =
                int(expr.Member.Name.Substring(4))

            let isTupleAccess(expr : Expression) =
                match expr with 
                | :? MemberExpression as mexpr ->
                    isTupleType mexpr.Expression.Type 
                    && mexpr.Expression.NodeType = ExpressionType.Parameter
                    && mexpr.Member.Name.StartsWith("Item")
                | _ -> false

    type private ReshapeVisitor () =
        inherit ExpressionVisitor() with

            let env = new Stack<List<ParameterExpression>>()
            let memberAssignments = new Dictionary<ParameterExpression, Expression list>()

            // Populate parameters list
            override this.VisitBlock(expr : BlockExpression) =
                
                let blockVars = expr.Variables
 
                env.Push(new List<_>())
                let newExprs = this.Visit(expr.Expressions)

                let rec flatten exprs acc =
                    match exprs with
                    | [] -> List.rev acc
                    | h :: t ->
                        match h with
                        | TupleNewAssignment (left, args) ->
                            match memberAssignments.TryGetValue(left) with
                            | true, xs -> flatten ((List.rev xs) @ t) (h :: acc) //t (h :: xs @ acc)
                            | false, _ -> 
                                // Generate some assignments (usually in C# this is not done)
                                let ys = args 
                                         |> Seq.toList 
                                         |> List.mapi (fun i arg -> 
                                            let i = i + 1
                                            let p = Expression.Parameter(arg.Type, left.Name + string i)
                                            env.Peek().Add(p)
                                            let itemExpr = Expression.MakeMemberAccess(left, left.Type.GetProperty("Item" + string i))
                                            Expression.Assign(p, itemExpr) :> Expression)
                                flatten (ys @ t) (h :: acc)
                        | TupleAssignment    (left, _)    ->
                            match memberAssignments.TryGetValue(left) with
                            | true, xs -> flatten ((List.rev xs) @ t) (h :: acc)
                            | false, _ -> flatten t (h :: acc)
                        | _ -> flatten t (h :: acc)

                let flat = flatten (List.ofSeq newExprs) []
                let newBlockVars = Seq.append blockVars (env.Pop())

                Expression.Block(newBlockVars, flat) :> _

            // Remove the weird lambdas and keep mappings
            // Looking for pattern ($id => %body).Invoke($var.Item$i)
            // and substitute with %body
            override this.VisitMethodCall(expr : MethodCallExpression) =

                //default case
                let pass () = expr.Update(this.Visit expr.Object, this.Visit expr.Arguments) :> Expression

                if expr.Object <> null then
                    match expr.Object.NodeType with
                    | ExpressionType.Lambda when Seq.length expr.Arguments = 1 ->
                        let lambda = expr.Object :?> LambdaExpression
                        let arg = expr.Arguments.Single() // $var.Item$i, $var
                        let param = lambda.Parameters.Single() // the mapping ($id)
                        
                        let funcTy = typedefof<Func<_,_>>.MakeGenericType([| arg.Type; lambda.ReturnType |])
                        let invoke = funcTy.GetMethod("Invoke")
                        let isInvoke = expr.Method = invoke

                        if isInvoke then 
                            env.Peek().Add(param)

                            let target = 
                                match arg with
                                | :? MemberExpression as me -> Some(me.Expression :?> ParameterExpression)
                                | :? ParameterExpression as pe -> Some pe 
                                | _ -> None //failwithf "Invalid target %A" arg
                            if target = None then pass ()
                            else
                                let target = target.Value
                                match memberAssignments.TryGetValue(target) with
                                | true, xs -> memberAssignments.[target] <- Expression.Assign(param, arg) :> _ :: xs
                                | false, _ -> memberAssignments.Add(target, [Expression.Assign(param, arg)])
                                this.Visit(lambda.Body)
                        else pass ()
                    | _ ->  
                        pass()
                else
                    pass()

    type private EscapeAnalysisVisitor () =
        inherit ExpressionVisitor() with

            let parameters = new Dictionary<ParameterExpression, Parameter>()

            member this.GetNotEscapingParameters() =
                parameters.Values
                |> Seq.filter(fun p -> not (Parameter.escapes p))
                |> Seq.toArray

            // Populate parameters list
            override this.VisitBlock(expr : BlockExpression) =
                let blockVars = expr.Variables
                blockVars |> Seq.filter (fun v -> isTupleType v.Type)
                          |> Seq.iter   (fun v -> parameters.Add(v, Parameter.create(v)))

                Expression.Block(blockVars, this.Visit expr.Expressions) :> _


            // Check for aliases %alias = %parameter
            override this.VisitBinary(expr : BinaryExpression) =
                match expr with
                | TupleNewAssignment(left, arguments) ->
                        let p = parameters.[left]
                        Seq.iter(fun arg -> p.MemberMappings.Add(arg)) arguments
                        expr.Update(expr.Left, null, this.Visit(expr.Right)) :> _
                | TupleAssignment(left, right) -> 
                    let lpar = parameters.[left]
                    let rpar = parameters.[right]
                    lpar.Source <- Some rpar
                    rpar.Alias <- Some lpar
                    expr :> _
                | _ ->
                    //Diagnostics.Debugger.Break()
                    if expr.NodeType = ExpressionType.Assign && expr.Left.NodeType = ExpressionType.Parameter && isTupleAccess expr.Right then
                        let right = expr.Right :?> MemberExpression
                        let p = parameters.[right.Expression :?> ParameterExpression]
                        let idx = getItemPosition right
                        p.MemberBindings.Add(expr.Left :?> ParameterExpression)
                        expr :> _
                    else
                        expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> _

            // Standalone appearance of ParameterExpression implies escape.
            override this.VisitParameter(expr : ParameterExpression) =
                match parameters.TryGetValue(expr) with
                | true, param -> 
                    param.Escapes <- true
                | false, _ -> ()
                expr :> _

            // &tuple.Item$i 
            override this.VisitMember(expr : MemberExpression) =
                expr.Update(expr.Expression) :> _

    type private TupleEliminationVisitor (parameters : Parameter []) =
        inherit ExpressionVisitor() with

            let parameterExprs = parameters |> Array.map(fun p -> p.Expr)
            let getParameter (expr : ParameterExpression) =
                let p = parameters.FirstOrDefault(fun p -> p.Expr = expr) 
                if box p = null then None else Some p

            let membersVisited = new List<Expression * int>()

            // Remove non-escaping parameters from block variables
            override this.VisitBlock(expr : BlockExpression) =
                let blockVars = expr.Variables |> Seq.filter(fun v -> not(parameterExprs.Contains(v))) |> Seq.toArray
                Expression.Block(blockVars, this.Visit(expr.Expressions)) :> _

            // Remove any assignments to those parameters
            override this.VisitBinary(expr : BinaryExpression) =
                let pass () = expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> Expression
                match expr.NodeType with
                | ExpressionType.Assign when (expr.Left :? ParameterExpression) ->
                    let left = expr.Left :?> ParameterExpression
                    if parameterExprs.Contains(left) then
                        match expr.Right with
                        | :? ParameterExpression as right when not(parameterExprs.Contains(right)) ->
                            pass()
                        | _ -> 
//                            let tupleParam = getParameter(left).Value
//                            // if there are no member bindings (usually in C#) then generate them
//                            if tupleParam.MemberBindings.Count = 0 then 
//                                let (TupleNewAssignment(_,args)) = expr
//                            else
                                Expression.Empty() :> _
                    else
                        pass()
                | _ -> pass()

            override this.VisitMember(expr : MemberExpression) =
                if expr.Expression <> null && expr.Expression :? ParameterExpression && parameterExprs.Contains(expr.Expression :?> ParameterExpression) then
                    let par = (getParameter (expr.Expression :?> ParameterExpression)).Value
                    let exprIdentity = expr.Expression, getItemPosition expr
                    match par.Source with
                    | None when membersVisited.Contains(exprIdentity) ->
                        par.MemberBindings.[getItemPosition expr - 1] :> _
                    | None ->
                        membersVisited.Add(exprIdentity)
                        this.Visit(par.MemberMappings.[getItemPosition expr - 1])
                    | Some src when parameters |> Seq.exists (fun p -> p.Expr = src.Expr) ->
                        src.MemberBindings.[getItemPosition expr - 1] :> _
                    | _ ->
                        expr.Update(this.Visit(expr.Expression)) :> _
                else
                    expr.Update(this.Visit(expr.Expression)) :> _

    module TupleElimination =
        let apply(expr : Expression) =
            let rsv = new ReshapeVisitor()
            let expr = rsv.Visit(expr)
            let eav = new EscapeAnalysisVisitor()
            let expr = eav.Visit(expr)
            let ps = eav.GetNotEscapingParameters()
            let tev = new TupleEliminationVisitor(ps)
            let expr = tev.Visit(expr)
            expr