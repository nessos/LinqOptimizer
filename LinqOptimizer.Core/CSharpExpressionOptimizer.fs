namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    // C#/Linq call patterns
    // TODO: expr type checks
    module private CSharpExpressionOptimizerHelpers =
        let rec toQueryExpr (expr : Expression) : QueryExpr =
            match expr with 
            | MethodCall (_, MethodName "Select" _, [expr'; Lambda ([_], bodyExpr) as f']) -> 
                Transform (optimize f' :?> LambdaExpression, toQueryExpr expr')
    
            | MethodCall (_, MethodName "Select" _, [expr'; Lambda ([_; _], bodyExpr) as f']) -> 
                TransformIndexed (optimize f' :?> LambdaExpression, toQueryExpr expr')
    
            | MethodCall (_, MethodName "Where" _, [expr'; Lambda ([paramExpr], _) as f']) -> 
                Filter (optimize f' :?> LambdaExpression, toQueryExpr expr')
    
            | MethodCall (_, MethodName "Where" _, [expr'; Lambda ([paramExpr; indexExpr], _) as f']) -> 
                FilterIndexed (optimize f' :?> LambdaExpression, toQueryExpr expr')
            
            | MethodCall (_, MethodName "Aggregate" [|_;_|] , [expr'; seedExpr; Lambda ([_;_] as paramsExpr, bodyExpr) as f' ] ) ->
                Aggregate(seedExpr, optimize f' :?> LambdaExpression, toQueryExpr expr')    

            | MethodCall (_, MethodName "Take" _, [expr'; countExpr]) when countExpr.Type = typeof<int> -> 
                Take (optimize countExpr, toQueryExpr expr' )
    
            | MethodCall (_, MethodName "Skip" _, [expr'; countExpr]) when countExpr.Type = typeof<int> -> 
                Skip (countExpr, toQueryExpr expr')
    
            | MethodCall (_, (MethodName "SelectMany" [|_; _|] as m), [expr'; Lambda ([paramExpr], bodyExpr)]) -> 
                NestedQuery ((paramExpr, toQueryExpr (optimize bodyExpr)), toQueryExpr expr')
    
            | MethodCall (_, MethodName "GroupBy" _, [expr'; Lambda ([paramExpr], bodyExpr) as f']) -> 
                GroupBy (optimize f' :?> LambdaExpression, toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|paramExpr.Type; bodyExpr.Type|])
    
            | MethodCall (_, MethodName "OrderBy" _, [expr'; Lambda ([paramExpr], bodyExpr) as f']) -> 
                OrderBy ([optimize f' :?> LambdaExpression, Order.Ascending], toQueryExpr expr')

            | MethodCall (_, MethodName "OrderByDescending" _, [expr'; Lambda ([paramExpr], bodyExpr) as f']) -> 
                OrderBy ([optimize f' :?> LambdaExpression, Order.Descending], toQueryExpr expr')
    
            | MethodCall (_, MethodName "Count" _,  [expr']) -> 
                Count(toQueryExpr expr')
    
            | MethodCall (_, MethodName "Range" _, [startExpr; countExpr]) ->
                RangeGenerator(optimize startExpr, optimize countExpr)

            | MethodCall (_, MethodName "Sum" _,  [expr']) -> 
                Sum(toQueryExpr expr')

            | MethodCall (_, MethodName "ToList" _, [expr']) ->
                ToList(toQueryExpr expr')

            | MethodCall (_, MethodName "ToArray" _, [expr']) ->
                ToArray(toQueryExpr expr')

            | MethodCall (expr', MethodName "ForEach" _, [Lambda ([paramExpr], bodyExpr) as f']) ->
                ForEach(optimize f' :?> LambdaExpression, toQueryExpr expr')

            | NotNull expr -> 
                if expr.Type.IsArray then
                    Source (expr, expr.Type.GetElementType())
                else
                    Source (expr, expr.Type.GetGenericArguments().[0])
            | _ ->
                invalidArg "expr" "Cannot extract QueryExpr from null Expression"

        and private transformer (expr : Expression) : Expression option =
            match expr with
            | MethodCall (_, MethodName "Select" _,             [_; Lambda _ ]) 
            | MethodCall (_, MethodName "Select" _,             [_; Lambda _ ]) 
            | MethodCall (_, MethodName "Where" _,              [_; Lambda _ ])
            | MethodCall (_, MethodName "Where" _,              [_; Lambda _ ]) 
            | MethodCall (_, MethodName "Aggregate" [|_;_|] ,   [_; _; Lambda ([_;_], _) ])
            | MethodCall (_, MethodName "Take" _,               [_; _        ])
            | MethodCall (_, MethodName "Skip" _,               [_; _        ])
            | MethodCall (_, MethodName "SelectMany" [|_; _|],  [_; Lambda _ ])
            | MethodCall (_, MethodName "GroupBy" _,            [_; Lambda _ ])
            | MethodCall (_, MethodName "OrderBy" _,            [_; Lambda _ ])
            | MethodCall (_, MethodName "OrderByDescending" _,  [_; Lambda _ ])
            | MethodCall (_, MethodName "Count" _,              [_           ])
            | MethodCall (_, MethodName "Range" _,              [_; _        ]) 
            | MethodCall (_, MethodName "Sum" _,                [_           ])
            | MethodCall (_, MethodName "ToList" _,             [_           ])
            | MethodCall (_, MethodName "ToArray" _,            [_           ]) 
            | MethodCall (_, MethodName "ForEach" _,            [Lambda _    ]) ->
                let query = toQueryExpr expr
                (Compiler.compileToSequential >> Some) query
            | _ ->
                None

        and private opt = ExpressionTransformer.transform transformer
        
        and optimize (expr : Expression) : Expression = opt expr

    type CSharpExpressionOptimizer =
        static member Optimize(expr : Expression) : Expression =
            CSharpExpressionOptimizerHelpers.optimize(expr)
        static member ToQueryExpr(expr : Expression) : QueryExpr =
            CSharpExpressionOptimizerHelpers.toQueryExpr(expr)
