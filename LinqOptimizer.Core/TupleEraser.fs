﻿namespace LinqOptimizer.Core
    
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
            let mappings = new Dictionary<ParameterExpression, Expression>()

            let tm = new Dictionary<ParameterExpression * string, ParameterExpression>() 
            member __.TupleMappings with get () = tm

            override this.VisitMethodCall(expr : MethodCallExpression) =
                //System.Diagnostics.Debugger.Break()
                
                // Looking for pattern (arg => body).Invoke(arg)
                // and substitute with body
                
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
                        |SpecialTupleArgExpression(tupledArg, mi) when isInvoke -> 
                            mappings.Add(param, arg)
                            this.TupleMappings.Add((tupledArg, mi.Name), param)
                            this.Visit(lambda.Body)
                        | _ -> pass()
                    | _ ->  
                        pass()
                else
                    pass()

//            override this.VisitParameter(expr : ParameterExpression) =
//                match mappings.TryGetValue(expr) with
//                | true, expr' -> expr'
//                | false, _    -> expr :> _

//            override this.VisitBinary(expr : BinaryExpression) =
//                match expr.NodeType with
//                | ExpressionType.Assign ->
//                    match expr.Left with
//                    | :? ParameterExpression as left when left.Name = specialTupleName ->
//                        
//                    | _ -> expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> _
//                | _ -> 
//                    expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> _

            override this.VisitBlock(expr : BlockExpression) =
                //System.Diagnostics.Debugger.Break()

//                let toReadOnly c = ReadOnlyCollection(Seq.toArray c)      
//
//                let before = Seq.takeWhile (fun e -> not(isTupledArgAssignment e)) expr.Expressions
//                             |> toReadOnly
//
//                let beforeVisited = this.Visit(before)
//
//                let assignExpr = expr.Expressions |> Seq.find isTupledArgAssignment
//
//                
//                Unchecked.defaultof<_>

                let exprs = this.Visit(expr.Expressions)

                let parameters = Seq.toArray mappings.Keys
                let vars = Seq.append expr.Variables parameters
                mappings.Clear()
                
                Expression.Block(vars, exprs ) :> _

    // Convert $tupledArg = new ($x, $y) to:
    // map[tupledArg.Item1] = $x
    // map[tupledArg.Item2] = $y
    // ...
    // and remove $tupledArg parameters
    type private TupleAssignmentEraser (mappings : Dictionary<ParameterExpression * string, ParameterExpression>) =
        inherit ExpressionVisitor() with
            
            let tupleArgParameters = mappings.Keys |> Seq.map fst |> Seq.toArray

            override this.VisitBinary(expr : BinaryExpression) =
                match expr with
                | TupledArgAssignment(tupledArg, args) ->
                    let exprs = args |> Seq.mapi (fun i arg -> Expression.Assign(mappings.[tupledArg, sprintf "Item%d" (i+1)], arg) :> Expression)
                    Expression.Block(exprs) :> _
                | _ -> expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> Expression

            override this.VisitBlock(expr : BlockExpression) =
                let vars = expr.Variables |> Seq.filter (fun p -> not <| tupleArgParameters.Contains(p))
                Expression.Block(vars, this.Visit expr.Expressions) :> _

    module TupleEraser =
        let visit (expr : Expression) =
            let tle = TupleLambdaEraser()
            let expr = tle.Visit(expr)
            let tae = TupleAssignmentEraser(tle.TupleMappings)
            let expr = tae.Visit(expr)
            expr