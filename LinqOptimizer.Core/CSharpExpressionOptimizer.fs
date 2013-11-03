namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    module CSharpExpressionOptimizer =

        // create Expression that calls a F#'s DU ctor
        let private mkCall =
            let queryExprTy = typeof<QueryExpr>
            fun (name : string) (args : seq<Expression>) ->
                let mi = queryExprTy.GetMethod(sprintf "New%s" name)
                Some(Expression.Call(mi, args) :> Expression)

        let private evalToQueryExpr (expr : Expression) : QueryExpr =
            let del = Expression.Lambda(expr).Compile() :?> Func<QueryExpr>
            del.Invoke()

        // C#/Linq call patterns
        // TODO: expr type checks
        let private toQueryExprTransformer (expr : Expression) : Expression option =
            match expr with
            | MethodCall (_, MethodName "Select" _, [expr'; Lambda ([_], bodyExpr) as f']) -> 
                //Transform (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)
                mkCall "Transform" [constant(f' :?> LambdaExpression); constant(evalToQueryExpr expr') ; constant bodyExpr.Type]
                
            | MethodCall (_, MethodName "Select" _, [expr'; Lambda ([_; _], bodyExpr) as f']) -> 
                //TransformIndexed (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)
                mkCall "TransformIndexed" [constant(f' :?> LambdaExpression); constant(evalToQueryExpr expr') ; constant bodyExpr.Type]
    
            | MethodCall (_, MethodName "Where" _, [expr'; Lambda ([paramExpr], _) as f']) -> 
                //Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
                mkCall "Filter" [constant(f' :?> LambdaExpression); constant(evalToQueryExpr expr') ; constant paramExpr.Type]
    
            | MethodCall (_, MethodName "Where" _, [expr'; Lambda ([paramExpr; indexExpr], _) as f']) -> 
                //FilterIndexed (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
                mkCall "FilterIndexed" [constant(f' :?> LambdaExpression); constant(evalToQueryExpr expr') ; constant paramExpr.Type]
    
            | MethodCall (_, MethodName "Take" _, [expr'; countExpr]) when countExpr.Type = typeof<int> -> 
                let queryExpr = evalToQueryExpr expr'
                //Take (countExpr, queryExpr, queryExpr.Type)
                mkCall "Take" [constant(countExpr); constant(queryExpr) ; constant queryExpr.Type]
    
            | MethodCall (_, MethodName "Skip" _, [expr'; countExpr]) when countExpr.Type = typeof<int> -> 
                let queryExpr = evalToQueryExpr expr'
                //Skip (countExpr, queryExpr, queryExpr.Type)
                mkCall "Skip" [constant(countExpr); constant(queryExpr) ; constant queryExpr.Type]

            | MethodCall (_, (MethodName "SelectMany" [|_; _|] as m), [expr'; Lambda ([paramExpr], bodyExpr)]) -> 
                //NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr', m.ReturnType.GetGenericArguments().[0])
                mkCall "NestedQuery" [constant(paramExpr, evalToQueryExpr bodyExpr);  constant(evalToQueryExpr expr') ; constant(m.ReturnType.GetGenericArguments().[0])]
    
            | MethodCall (_, MethodName "GroupBy" _, [expr'; Lambda ([paramExpr], bodyExpr) as f']) -> 
                //GroupBy (f' :?> LambdaExpression, toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|paramExpr.Type; bodyExpr.Type|])
                mkCall "GroupBy" [constant(f' :?> LambdaExpression); constant(evalToQueryExpr expr') ; constant(typedefof<IGrouping<_, _>>.MakeGenericType [|paramExpr.Type; bodyExpr.Type|])]

            | MethodCall (_, MethodName "OrderBy" _, [expr'; Lambda ([paramExpr], bodyExpr) as f']) -> 
                //OrderBy (f' :?> LambdaExpression, Order.Ascending, toQueryExpr expr', paramExpr.Type)
                mkCall "OrderBy" [constant(f' :?> LambdaExpression); constant(Order.Ascending); constant(evalToQueryExpr expr') ; constant(paramExpr.Type)]
    
            | MethodCall (_, MethodName "Count" _,  [expr']) -> 
                let query' = evalToQueryExpr expr'
                //Count (query', query'.Type)
                mkCall "Count" [constant(query'); constant(query'.Type)]
    
            | MethodCall (_, MethodName "Range" _, [startExpr; countExpr]) ->
                //RangeGenerator(startExpr, countExpr)
                mkCall "RangeGenerator" [constant(startExpr); constant(countExpr)]
            
            | _ when expr.Type.IsArray -> 
                //Source (expr, expr.Type.GetElementType())
                mkCall "Source" [ constant expr; constant(expr.Type.GetElementType()) ]
            
            | _ when typedefof<IEnumerable<_>>.IsAssignableFrom(expr.Type)  ->
                //Source (expr, expr.Type.GetGenericArguments().[0])
                mkCall "Source" [ constant expr; constant(expr.Type.GetGenericArguments().[0])]
            
            | _ ->
                None

        let optimize (expr : Expression) : Expression =
            ExpressionTransformer.transform toQueryExprTransformer expr

        let optimizeAsQueryExpr (expr : Expression) : QueryExpr =
            optimize expr |> evalToQueryExpr
