namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    module FSharpExpressionTransformer =

        // F# call patterns
        // TODO: expr type checks
        let rec toQueryExpr (expr : Expression) : QueryExpr =
            match expr with
            | MethodCall (_, MethodName "Map" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([_], bodyExpr) as f']) ; expr']) -> 
                Transform (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)
            
            | MethodCall (_, MethodName "Filter" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], _) as f']) ; expr']) -> 
                Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
            
            | MethodCall (_, MethodName "Where" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], _) as f']) ; expr']) -> 
                Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
            
            | MethodCall (_, MethodName "Take" _, [countExpr; expr' ]) when countExpr.Type = typeof<int> -> 
                let queryExpr = toQueryExpr expr'
                Take (countExpr, queryExpr, queryExpr.Type)
            
            | MethodCall (_, MethodName "Skip" _, [countExpr; expr' ]) when countExpr.Type = typeof<int> -> 
                let queryExpr = toQueryExpr expr'
                Skip (countExpr, queryExpr, queryExpr.Type)

            | MethodCall (_, (MethodName "Collect" [|_; _|] as m), [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], bodyExpr) as f']) ; expr']) -> 
                NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr', m.ReturnType.GetGenericArguments().[0])

            | MethodCall (_, MethodName "Sort" _, [expr']) -> 
                let query' = toQueryExpr expr'
                let v = var "x" query'.Type
                let f = Expression.Lambda(v,v)
                OrderBy (f, Order.Ascending, query', query'.Type)

            | MethodCall (_, MethodName "SortBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], bodyExpr) as f']) ; expr']) -> 
                OrderBy (f' :?> LambdaExpression, Order.Ascending, toQueryExpr expr', paramExpr.Type)

            | MethodCall (_, MethodName "Length" _, [expr']) -> 
                let query' = toQueryExpr expr'
                Count (query', query'.Type)

            | _ -> 
                if expr.Type.IsArray then
                    Source (expr, expr.Type.GetElementType())
                else
                    Source (expr, expr.Type.GetGenericArguments().[0])


//            | MethodCall (_, MethodName "ToArray" _, [expr']) -> 
//                ToArray(toQueryExpr expr')
//            | MethodCall (_, MethodName "GroupBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], bodyExpr) as f']) ; expr']) -> 
//                GroupBy (f' :?> LambdaExpression, toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|paramExpr.Type; bodyExpr.Type|])
