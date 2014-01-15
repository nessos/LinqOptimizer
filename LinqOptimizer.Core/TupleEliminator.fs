namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    type TupleEliminator () =
        inherit ExpressionVisitor() with

            let tupleTypes = [ typedefof<Tuple>
                               typedefof<Tuple<_,_>>
                               typedefof<Tuple<_,_,_>>
                               typedefof<Tuple<_,_,_,_>>
                               typedefof<Tuple<_,_,_,_,_>>
                               typedefof<Tuple<_,_,_,_,_,_>>
                               typedefof<Tuple<_,_,_,_,_,_,_>>
                               typedefof<Tuple<_,_,_,_,_,_,_,_>>
                             ]

            // $a <-> tupledArg.Item$i
            let mappings = new Dictionary<ParameterExpression, Expression>()

            let isSpecialTupleArgExpression (expr : Expression) = 
                match expr with
                | :? MemberExpression as expr -> 
                    match expr.Expression with
                    | :? ParameterExpression as param ->
                        let ty = param.Type.GetGenericTypeDefinition()
                        param.Name = "tupledArg" && tupleTypes.Contains(ty) 
                    | _ -> false
                | _ -> false

            override this.VisitMethodCall(expr : MethodCallExpression) =
                //System.Diagnostics.Debugger.Break()
                
                // Looking for pattern (arg => body).Invoke(arg)
                // and substitute with body

                match expr.Object.NodeType with
                | ExpressionType.Lambda when Seq.length expr.Arguments = 1 ->
                    let lambda = expr.Object :?> LambdaExpression
                    let arg = expr.Arguments.Single()
                    let param = lambda.Parameters.Single()
                    let funcTy = typedefof<Func<_,_>>.MakeGenericType([| arg.Type; lambda.ReturnType |])
                    let invoke = funcTy.GetMethod("Invoke")
                    let isInvoke = expr.Method = invoke
                    if isInvoke && isSpecialTupleArgExpression arg then
                        mappings.Add(param, arg)
                        this.Visit(lambda.Body)
                    else 
                        expr.Update(this.Visit expr.Object, this.Visit expr.Arguments) :> _
                | _ ->  
                    expr.Update(this.Visit expr.Object, this.Visit expr.Arguments) :> _

            override this.VisitParameter(expr : ParameterExpression) =
                match mappings.TryGetValue(expr) with
                | true, expr' -> expr'
                | false, _    -> expr :> _

//            override this.VisitBinary(expr : BinaryExpression) =
//                match expr.NodeType with
//                | ExpressionType.Assign