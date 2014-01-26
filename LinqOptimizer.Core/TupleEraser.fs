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
                                |> Seq.toList
                    Expression.Block((expr :> Expression) :: exprs) :> _
                | _ -> expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> Expression

            override this.VisitBlock(expr : BlockExpression) =
                let vars = expr.Variables //|> Seq.filter (fun p -> not <| tupleArgParameters.Contains(p))
                Expression.Block(vars, this.Visit expr.Expressions) :> _

    module TupleEraser =
        let apply (expr : Expression) =
            let tle = TupleLambdaEraser()
            let expr = tle.Visit(expr)
            let tae = TupleAssignmentEraser(tle.TupleMappings)
            let expr = tae.Visit(expr)
            expr 


//namespace LinqOptimizer.Core
//    
//    open System
//    open System.Collections
//    open System.Collections.Generic
//    open System.Linq
//    open System.Linq.Expressions
//    open System.Reflection
//    open System.Collections.Concurrent
//    open System.Collections.ObjectModel
//
//    // 1st step : Escape analysis, find aliases, remove wierd lambdas and keep possible mappings.
//    // 2nd step : For tuples that do not escape modify construction site and .Item calls.
//
//    type private Parameter = {
//        Expr            : ParameterExpression
//        Aliases         : List<Parameter>
//        mutable Escapes : bool
//        mutable Original: Parameter option
//        MemberMappings  : List<Expression> } 
//    with 
//        static member create (expr : ParameterExpression) =
//            {Expr = expr; Aliases = new List<_>(); Escapes = false; Original = None; MemberMappings = new List<_>() }
//
//        static member aliasesEscape (parameter : Parameter) =
//            parameter.Aliases |> Seq.exists (fun p -> p.Escapes || Parameter.aliasesEscape p)
//
//    [<AutoOpen>]
//    module private Helpers =
//    
//            let tupleTypes = [ typedefof<Tuple>
//                               typedefof<Tuple<_,_>>
//                               typedefof<Tuple<_,_,_>>
//                               typedefof<Tuple<_,_,_,_>>
//                               typedefof<Tuple<_,_,_,_,_>>
//                               typedefof<Tuple<_,_,_,_,_,_>>
//                               typedefof<Tuple<_,_,_,_,_,_,_>>
//                               typedefof<Tuple<_,_,_,_,_,_,_,_>>
//                             ]
//
//            let specialTupleName = "tupledArg"
//
//            let isTupleType (ty : Type) = 
//                ty.IsGenericType 
//                && tupleTypes.Contains(ty.GetGenericTypeDefinition())
//
//            // Check if $id = new Tuple($e1, ...., $en)
//            let (|TupleNewAssignment|_|) (expr : Expression) =
//                match expr with
//                | :? BinaryExpression as expr 
//                    when expr.NodeType = ExpressionType.Assign 
//                         && expr.Left.NodeType = ExpressionType.Parameter
//                         && expr.Right.NodeType = ExpressionType.New ->
//                    let left = expr.Left :?> ParameterExpression
//                    let right = expr.Right :?> NewExpression
//                    if isTupleType left.Type then
//                        Some (left, right.Arguments)
//                    else None
//                | _ -> None
//
//            let (|TupleAssignment|_|) (expr : Expression) =
//                match expr with
//                | :? BinaryExpression as expr 
//                    when expr.NodeType = ExpressionType.Assign 
//                         && expr.Left.NodeType  = ExpressionType.Parameter
//                         && expr.Right.NodeType = ExpressionType.Parameter
//                         && isTupleType expr.Left.Type ->
//                    Some(expr.Left :?> ParameterExpression, expr.Right :?> ParameterExpression)
//                | _ -> None
//
//            // $foo.Item$id -> $id
//            let getItemPosition(expr : MemberExpression) =
//                int(expr.Member.Name.Substring(4))
//
//            let isTupleAccess(expr : Expression) =
//                match expr with 
//                | :? MemberExpression as mexpr ->
//                    isTupleType mexpr.Expression.Type 
//                    && mexpr.Expression.NodeType = ExpressionType.Parameter
//                    && mexpr.Member.Name.StartsWith("Item")
//                | _ -> false
//
//    type private ReshapeVisitor () =
//        inherit ExpressionVisitor() with
//
//            let env = new Stack<List<ParameterExpression>>()
//            let memberAssignments = new Dictionary<ParameterExpression, Expression list>()
//
//            // Populate parameters list
//            override this.VisitBlock(expr : BlockExpression) =
//                
//                let blockVars = expr.Variables
// 
//                env.Push(new List<_>())
//                let newExprs = this.Visit(expr.Expressions)
//                let newBlockVars = Seq.append blockVars (env.Pop())
////                let flat = 
////                    newExprs 
////                    |> Seq.collect (fun expr ->
////                        match expr with
////                        | TupleNewAssignment (left, _) 
////                        | TupleAssignment    (left, _)    ->
////                            match memberAssignments.TryGetValue(left) with
////                            | true, xs -> expr :: xs
////                            | false, _ -> [expr]
////                        | _ -> [expr])
////                    |> Seq.toArray
//
//                let rec flatten exprs acc =
//                    match exprs with
//                    | [] -> List.rev acc
//                    | h :: t ->
//                        match h with
//                        | TupleNewAssignment (left, _) 
//                        | TupleAssignment    (left, _)    ->
//                            match memberAssignments.TryGetValue(left) with
//                            | true, xs -> flatten (xs @ t) (h :: acc)
//                            | false, _ -> flatten t (h :: acc)
//                        | _ -> flatten t (h :: acc)
//
//                let flat = flatten (List.ofSeq newExprs) []
//
//                Expression.Block(newBlockVars, flat) :> _
//
//            // Remove the weird lambdas and keep mappings
//            // Looking for pattern ($id => %body).Invoke($var.Item$i)
//            // and substitute with %body
//            override this.VisitMethodCall(expr : MethodCallExpression) =
//
//                //default case
//                let pass () = expr.Update(this.Visit expr.Object, this.Visit expr.Arguments) :> Expression
//
//                if expr.Object <> null then
//                    match expr.Object.NodeType with
//                    | ExpressionType.Lambda when Seq.length expr.Arguments = 1 ->
//                        let lambda = expr.Object :?> LambdaExpression
//                        let arg = expr.Arguments.Single() // $var.Item$i, $var
//                        let param = lambda.Parameters.Single() // the mapping ($id)
//                        
//                        let funcTy = typedefof<Func<_,_>>.MakeGenericType([| arg.Type; lambda.ReturnType |])
//                        let invoke = funcTy.GetMethod("Invoke")
//                        let isInvoke = expr.Method = invoke
//
//                        if isInvoke then 
//                            env.Peek().Add(param)
//
//                            let target = 
//                                match arg with
//                                | :? MemberExpression as me -> me.Expression :?> ParameterExpression
//                                | :? ParameterExpression as pe -> pe 
//                                | _ -> failwithf "Invalid target %A" arg
//
//                            match memberAssignments.TryGetValue(target) with
//                            | true, xs -> memberAssignments.[target] <- Expression.Assign(param, arg) :> _ :: xs
//                            | false, _ -> memberAssignments.Add(target, [Expression.Assign(param, arg)])
//                            this.Visit(lambda.Body)
//                        else pass ()
//                    | _ ->  
//                        pass()
//                else
//                    pass()
//
//    type private EscapeAnalysisVisitor () =
//        inherit ExpressionVisitor() with
//
//            let parameters = new Dictionary<ParameterExpression, Parameter>()
//
//            member this.GetNotEscapingParameters() =
//                parameters.Values
//                |> Seq.filter(fun p -> not p.Escapes && not (Parameter.aliasesEscape p))
//                //|> Seq.filter(fun p -> Option.isNone p.Original)
//                |> Seq.toArray
//
//            // Populate parameters list
//            override this.VisitBlock(expr : BlockExpression) =
//                let blockVars = expr.Variables
//                blockVars |> Seq.filter (fun v -> isTupleType v.Type)
//                          |> Seq.iter   (fun v -> parameters.Add(v, Parameter.create(v)))
//
//                Expression.Block(blockVars, this.Visit expr.Expressions) :> _
//
//
//            // Check for aliases %alias = %parameter
//            override this.VisitBinary(expr : BinaryExpression) =
//                match expr with
//                | TupleNewAssignment(left, arguments) ->
//                        // left does not escape
//                        let p = parameters.[left]
//                        arguments 
//                        |> Seq.iter(fun arg -> p.MemberMappings.Add(arg))
//                        expr.Update(expr.Left, null, this.Visit(expr.Right)) :> _
//                | TupleAssignment(left, right) -> 
//                    let lpar = parameters.[left]
//                    let rpar = parameters.[right]
//                    lpar.Original <- Some rpar
//                    rpar.Aliases.Add(lpar)
//                    //lpar.MemberMappings.AddRange(rpar.MemberMappings)
//                    expr :> _
//                | _ ->
////                    if expr.NodeType = ExpressionType.Assign 
////                        && expr.Left.NodeType = ExpressionType.Parameter 
////                        && isTupleAccess expr.Right then
//                    expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> _
//
//            // Standalone appearance of ParameterExpression implies escape.
//            override this.VisitParameter(expr : ParameterExpression) =
//                match parameters.TryGetValue(expr) with
//                | true, param -> 
//                    param.Escapes <- true
//                | false, _ -> ()
//                expr :> _
//
//            // &tuple.Item$i 
//            override this.VisitMember(expr : MemberExpression) =
//                expr.Update(expr.Expression) :> _
//
//    type private TupleEliminationVisitor (parameters : Parameter []) =
//        inherit ExpressionVisitor() with
//
//            let parameterExprs = parameters |> Array.map(fun p -> p.Expr)
//            let aliasMappings = new Dictionary<ParameterExpression * int, ParameterExpression>()
//            let getParameter (expr : ParameterExpression) =
//                let p = parameters.FirstOrDefault(fun p -> p.Expr = expr) 
//                if box p = null then None else Some p
//
//            // Remove non-escaping parameters from block variables
//            override this.VisitBlock(expr : BlockExpression) =
//                let blockVars = expr.Variables |> Seq.filter(fun v -> not(parameterExprs.Contains(v))) |> Seq.toArray
//                Expression.Block(blockVars, this.Visit(expr.Expressions)) :> _
//
//            // Remove any assignments to those parameters
//            override this.VisitBinary(expr : BinaryExpression) =
//                let pass () = expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> Expression
//                match expr.NodeType with
//                | ExpressionType.Assign when (expr.Left :? ParameterExpression) ->
//                    let left = expr.Left :?> ParameterExpression
//                    if parameterExprs.Contains(left) then
//                        match expr.Right with
//                        | :? ParameterExpression as right when not(parameterExprs.Contains(right)) ->
//                            pass ()
//                        | _ -> 
//                            Expression.Empty() :> _
//                    elif isTupleAccess expr.Right then
//                        let right = expr.Right :?> MemberExpression
//                        let item = getItemPosition right
//                        let obj = right.Expression :?> ParameterExpression
//                        let alias = getParameter obj
//                        match alias with
//                        | None -> pass()
//                        | Some alias ->
//                            match alias.Original with
//                            | None -> aliasMappings.Add((obj, item), left); pass ()
//                            | Some orig -> pass () 
//                    else
//                        pass ()
//                | _ -> pass ()
//
//            override this.VisitMember(expr : MemberExpression) =
//                if expr.Expression <> null && expr.Expression :? ParameterExpression && parameterExprs.Contains(expr.Expression :?> ParameterExpression) then
//                    let par = (getParameter (expr.Expression :?> ParameterExpression)).Value
//                    match par.Original with
//                    | None ->
//                        par.MemberMappings.[getItemPosition expr - 1]
//                    | Some orig when parameters.Contains(orig) -> // is alias
//                        aliasMappings.[orig.Expr, getItemPosition expr ] :> _
//                    | _ -> expr.Update(this.Visit(expr.Expression)) :> _
//                else
//                    expr.Update(this.Visit(expr.Expression)) :> _
//
//    module TupleElimination =
//        let apply(expr : Expression) =
//            let rsv = new ReshapeVisitor()
//            let expr = rsv.Visit(expr)
//            let eav = new EscapeAnalysisVisitor()
//            let expr = eav.Visit(expr)
//            let ps = eav.GetNotEscapingParameters()
//            let tev = new TupleEliminationVisitor(ps)
//            let expr = tev.Visit(expr)
//            expr