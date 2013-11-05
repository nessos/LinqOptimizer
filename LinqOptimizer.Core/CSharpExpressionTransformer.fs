namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    module CSharpExpressionTransformer =

        // C#/Linq call patterns
        // TODO: expr type checks
        let rec toQueryExpr (expr : Expression) : QueryExpr =
            match expr with
            | MethodCall (_, MethodName "Select" _, [expr'; Lambda ([_], bodyExpr) as f']) -> 
                Transform (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)
    
            | MethodCall (_, MethodName "Select" _, [expr'; Lambda ([_; _], bodyExpr) as f']) -> 
                TransformIndexed (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)
    
            | MethodCall (_, MethodName "Where" _, [expr'; Lambda ([paramExpr], _) as f']) -> 
                Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
    
            | MethodCall (_, MethodName "Where" _, [expr'; Lambda ([paramExpr; indexExpr], _) as f']) -> 
                FilterIndexed (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
    
            | MethodCall (_, MethodName "Take" _, [expr'; countExpr]) when countExpr.Type = typeof<int> -> 
                let queryExpr = toQueryExpr expr'
                Take (countExpr, queryExpr, queryExpr.Type)
    
            | MethodCall (_, MethodName "Skip" _, [expr'; countExpr]) when countExpr.Type = typeof<int> -> 
                let queryExpr = toQueryExpr expr'
                Skip (countExpr, queryExpr, queryExpr.Type)
    
            | MethodCall (_, (MethodName "SelectMany" [|_; _|] as m), [expr'; Lambda ([paramExpr], bodyExpr)]) -> 
                NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr', m.ReturnType.GetGenericArguments().[0])
    
            | MethodCall (_, MethodName "GroupBy" _, [expr'; Lambda ([paramExpr], bodyExpr) as f']) -> 
                GroupBy (f' :?> LambdaExpression, toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|paramExpr.Type; bodyExpr.Type|])
    
            | MethodCall (_, MethodName "OrderBy" _, [expr'; Lambda ([paramExpr], bodyExpr) as f']) -> 
                OrderBy ([f' :?> LambdaExpression,  Order.Ascending], toQueryExpr expr', paramExpr.Type)
    
            | MethodCall (_, MethodName "Count" _,  [expr']) -> 
                let query' = toQueryExpr expr'
                Count (query', query'.Type)
    
            | MethodCall (_, MethodName "Range" _, [startExpr; countExpr]) ->
                RangeGenerator(startExpr, countExpr)
            
            | _ -> 
                if expr.Type.IsArray then
                    Source (expr, expr.Type.GetElementType())
                else
                    Source (expr, expr.Type.GetGenericArguments().[0])


