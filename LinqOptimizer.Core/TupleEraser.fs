namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent
    open System.Collections.ObjectModel

    [<AutoOpen>]
    module private TupleEraserHelpers =
    
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

            // Check if $tupledArg = new Tuple($e1, ...., $en)
            let (|TupledArgAssignment|_|) (expr : Expression) =
                match expr with
                | :? BinaryExpression as expr 
                    when expr.NodeType = ExpressionType.Assign 
                         && expr.Left.NodeType = ExpressionType.Parameter
                         && expr.Right.NodeType = ExpressionType.New ->
                    let left = expr.Left :?> ParameterExpression
                    let right = expr.Right :?> NewExpression
                    if left.Name = specialTupleName && isTupleType left.Type then
                        Some (left, right.Arguments)
                    else None
                | _ -> None

    type private TupleLambdaEraser () =
        inherit ExpressionVisitor() with

            // $a <-> tupledArg.Item$i
            let tupleMappings = new Stack<Dictionary<ParameterExpression, Expression>>()

            let tm = new Dictionary<ParameterExpression * string, ParameterExpression>() 
            member __.TupleMappings with get () = tm

            override this.VisitMethodCall(expr : MethodCallExpression) =

                // Looking for pattern ($id => %body).Invoke($tupledArg.Item$i)
                // and substitute with %body
                
                //default case
                let pass () = expr.Update(this.Visit expr.Object, this.Visit expr.Arguments) :> Expression

                if expr.Object <> null then
                    match expr.Object.NodeType with
                    | ExpressionType.Lambda when Seq.length expr.Arguments = 1 ->
                        let lambda = expr.Object :?> LambdaExpression
                        let arg = expr.Arguments.Single()
                        let param = lambda.Parameters.Single()
                        let funcTy = typedefof<Func<_,_>>.MakeGenericType([| arg.Type; lambda.ReturnType |])
                        let invoke = funcTy.GetMethod("Invoke")
                        let isInvoke = expr.Method = invoke
                        match arg with 
                        | SpecialTupleArgExpression(tupledArg, mi) when isInvoke -> 
                            if not <| isWildcardParameter param then
                                tupleMappings.Peek().Add(param, arg)
                                this.TupleMappings.Add((tupledArg, mi.Name), param)
                            this.Visit(lambda.Body)
                        | arg ->
                            Expression.Block([param], Expression.Assign(param, arg), this.Visit(lambda.Body)) :> _
                    | _ ->  
                        pass()
                else
                    pass()

            override this.VisitBlock(expr : BlockExpression) =
                tupleMappings.Push(new Dictionary<_,_>())
                let exprs = this.Visit(expr.Expressions)
                let current = tupleMappings.Pop()

                let parameters = Seq.toArray current.Keys
                let vars = Seq.append expr.Variables parameters

                Expression.Block(vars, exprs ) :> _

    // Convert $tupledArg = new ($x, $y) to:
    // map[tupledArg.Item1] = $x
    // map[tupledArg.Item2] = $y
    // ...
    // and remove $tupledArg parameters
    type private TupleAssignmentEraser (tupleMappings : Dictionary<ParameterExpression * string, ParameterExpression>) =
        inherit ExpressionVisitor() with
            
            let tupleArgParameters = tupleMappings.Keys |> Seq.map fst |> Seq.toArray

            override this.VisitBinary(expr : BinaryExpression) =
                match expr with
                | TupledArgAssignment(tupledArg, args) ->
                    let exprs = args 
                                |> Seq.mapi (fun i arg -> 
                                    match tupleMappings.TryGetValue((tupledArg, sprintf "Item%d" (i+1))) with
                                    | true, pexpr -> Some(Expression.Assign(pexpr, arg) :> Expression)
                                    | false, _ -> None)
                                |> Seq.filter Option.isSome
                                |> Seq.map Option.get
                    Expression.Block(exprs) :> _
                | _ -> expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> Expression

            override this.VisitBlock(expr : BlockExpression) =
                let vars = expr.Variables |> Seq.filter (fun p -> not <| tupleArgParameters.Contains(p))
                Expression.Block(vars, this.Visit expr.Expressions) :> _

    module TupleEraser =
        let apply (expr : Expression) =
            let tle = TupleLambdaEraser()
            let expr = tle.Visit(expr)
            let tae = TupleAssignmentEraser(tle.TupleMappings)
            let expr = tae.Visit(expr)
            expr 