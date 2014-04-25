namespace Nessos.LinqOptimizer.Core
    
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
        let private sourceOfExpr (expr : Expression) sourceType : QueryExpr =
                if expr.Type.IsArray then
                    Source (expr, expr.Type.GetElementType(), sourceType)
                elif expr.Type.IsGenericType && expr.Type.GetGenericTypeDefinition() = typedefof<IEnumerable<_>> then
                    Source(expr, expr.Type.GetGenericArguments().[0], sourceType)
                elif expr.Type.IsGenericType then
                    Source (expr, expr.Type.GetInterface("IEnumerable`1").GetGenericArguments().[0], sourceType)
                else
                    failwithf "Not supported source %A" expr
        
        let rec toQueryExpr (expr : Expression) : QueryExpr =
            match expr with 
            | MethodCall (_, MethodName "Select" _, [expr'; LambdaOrQuote ([_], bodyExpr, f')]) -> 
                Transform (f', toQueryExpr expr')
    
            | MethodCall (_, MethodName "Select" _, [expr'; LambdaOrQuote ([_; _], bodyExpr, f')]) -> 
                TransformIndexed (f', toQueryExpr expr')
    
            | MethodCall (_, MethodName "Where" _, [expr'; LambdaOrQuote ([paramExpr], _, f')]) -> 
                Filter (f', toQueryExpr expr')
    
            | MethodCall (_, MethodName "Where" _, [expr'; LambdaOrQuote ([paramExpr; indexExpr], _,f')]) -> 
                FilterIndexed (f' , toQueryExpr expr')
            
            | MethodCall (_, MethodName "Aggregate" _, [expr'; seedExpr; LambdaOrQuote ([_;_] as paramsExpr, bodyExpr, f') ] ) ->
                Aggregate(seedExpr, f' , toQueryExpr expr')    

            | MethodCall (_, MethodName "Generate" _, [startExpr; Lambda (_,_) as pred; LambdaOrQuote(_,_,state); LambdaOrQuote(_,_,result)]) ->
                Generate(startExpr, pred :?> LambdaExpression, state, result)

            | MethodCall (_, MethodName "Take" _, [expr'; countExpr]) when countExpr.Type = typeof<int> -> 
                Take (countExpr, toQueryExpr expr' )

            | MethodCall (_, MethodName "TakeWhile" _, [expr'; LambdaOrQuote ([paramExpr], _,f')]) -> 
                TakeWhile(f', toQueryExpr expr' )

            | MethodCall (_, MethodName "SkipWhile" _, [expr'; LambdaOrQuote ([paramExpr], _,f')]) -> 
                SkipWhile(f', toQueryExpr expr' )

            | MethodCall (_, MethodName "Skip" _, [expr'; countExpr]) when countExpr.Type = typeof<int> -> 
                Skip (countExpr, toQueryExpr expr')
    
            | MethodCall (_, (MethodName "SelectMany" _), [expr'; LambdaOrQuote ([paramExpr], bodyExpr, _)]) -> 
                NestedQuery ((paramExpr, toQueryExpr (bodyExpr)), toQueryExpr expr')

            | MethodCall (_, (MethodName "SelectMany" _), [expr'; LambdaOrQuote ([paramExpr], bodyExpr, _); LambdaOrQuote (_,_,lam)]) -> 
                NestedQueryTransform((paramExpr, toQueryExpr (bodyExpr)), lam, toQueryExpr expr')
    
            | MethodCall (_, MethodName "GroupBy" _, [expr'; LambdaOrQuote ([paramExpr], bodyExpr,f')]) -> 
                GroupBy (f', toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|bodyExpr.Type; paramExpr.Type|])
    
            | MethodCall (_, MethodName "OrderBy" _, [expr'; LambdaOrQuote ([paramExpr], bodyExpr, f')]) -> 
                OrderBy ([f', Order.Ascending], toQueryExpr expr')

            | MethodCall (_, MethodName "OrderByDescending" _, [expr'; LambdaOrQuote ([paramExpr], bodyExpr, f')]) -> 
                OrderBy ([f', Order.Descending], toQueryExpr expr')
    
            | MethodCall (_, MethodName "Count" _,  [expr']) -> 
                Count(toQueryExpr expr')
    
            | MethodCall (_, MethodName "Range" _, [startExpr; countExpr]) ->
                RangeGenerator(startExpr, countExpr)

            | MethodCall (_, MethodName "Sum" _,  [expr']) -> 
                Sum(toQueryExpr expr')

            | MethodCall (_, MethodName "ToList" _, [expr']) ->
                ToList(toQueryExpr expr')

            | MethodCall (_, MethodName "ToArray" _, [expr']) ->
                ToArray(toQueryExpr expr')

            | MethodCall (expr', MethodName "ForEach" _, [       LambdaOrQuote ([paramExpr], bodyExpr, f')]) 
            | MethodCall (_,     MethodName "ForEach" _, [expr'; LambdaOrQuote ([paramExpr], bodyExpr, f')]) ->
                ForEach(f', toQueryExpr expr')

            | MethodCall (_, MethodName "AsQueryExpr" _, [expr']) ->
                sourceOfExpr expr' QueryExprType.Sequential

            | MethodCall (_, MethodName "AsParallelQueryExpr" _, [expr']) ->
                sourceOfExpr expr' QueryExprType.Parallel

            | NotNull expr' -> 
                sourceOfExpr expr' QueryExprType.Sequential

            | _ ->
                invalidArg "expr" "Cannot extract QueryExpr from null Expression"

        and private transformer (expr : Expression) : Expression option =
            match expr with
            | MethodCall (_, MethodName "AsQueryExpr" _,        [_              ])
            | MethodCall (_, MethodName "Select" _,             [_; LambdaOrQuote _ ]) 
            | MethodCall (_, MethodName "Select" _,             [_; LambdaOrQuote _ ]) 
            | MethodCall (_, MethodName "Where" _,              [_; LambdaOrQuote _ ])
            | MethodCall (_, MethodName "Where" _,              [_; LambdaOrQuote _ ]) 
            | MethodCall (_, MethodName "Aggregate" _,          [_; _; LambdaOrQuote ([_;_], _, _) ])
            | MethodCall (_, MethodName "Take" _,               [_; _        ])
            | MethodCall (_, MethodName "TakeWhile" _,          [_; LambdaOrQuote _ ])
            | MethodCall (_, MethodName "SkipWhile" _,          [_; LambdaOrQuote _ ])
            | MethodCall (_, MethodName "Skip" _,               [_; _        ])
            | MethodCall (_, MethodName "SelectMany" _,         [_; LambdaOrQuote _ ])
            | MethodCall (_, MethodName "SelectMany" _,         [_; LambdaOrQuote _; LambdaOrQuote _])
            | MethodCall (_, MethodName "GroupBy" _,            [_; LambdaOrQuote _ ])
            | MethodCall (_, MethodName "OrderBy" _,            [_; LambdaOrQuote _ ])
            | MethodCall (_, MethodName "OrderByDescending" _,  [_; LambdaOrQuote _ ])
            | MethodCall (_, MethodName "Count" _,              [_           ])
            | MethodCall (_, MethodName "Range" _,              [_; _        ]) 
            | MethodCall (_, MethodName "Sum" _,                [_           ])
            | MethodCall (_, MethodName "ToList" _,             [_           ])
            | MethodCall (_, MethodName "ToArray" _,            [_           ]) 
            | MethodCall (_, MethodName "ForEach" _,            [_; LambdaOrQuote _    ]) 
            | MethodCall (_, MethodName "ForEach" _,            [   LambdaOrQuote _    ]) ->
                let query = toQueryExpr expr
                let expr = (Compiler.compileToSequential query optimize)
                Some expr
            | _ ->
                None

        and private opt = ExpressionTransformer.transform transformer
        
        and optimize (expr : Expression) : Expression = opt expr

    type CSharpExpressionOptimizer =
        static member Optimize(expr : Expression) : Expression =
            CSharpExpressionOptimizerHelpers.optimize(expr)
        static member ToQueryExpr(expr : Expression) : QueryExpr =
            CSharpExpressionOptimizerHelpers.toQueryExpr(expr)
