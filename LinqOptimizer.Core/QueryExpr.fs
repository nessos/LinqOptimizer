namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection

//    // Typed Wrapper for QueryExpr 
//    type QueryExpr<'T>(queryExpr : QueryExpr) =
//        member self.QueryExpr = queryExpr 
//    type ParallelQueryExpr<'T>(queryExpr : QueryExpr) =
//        member self.QueryExpr = queryExpr 
    // C# friendly QueryExpr<unit>
//    and QueryExprVoid(queryExpr : QueryExpr) =
//        member self.QueryExpr = queryExpr 
//    and ParallelQueryExprVoid(queryExpr : QueryExpr) =
//        member self.QueryExpr = queryExpr 
    // Main Query representation
    type Order = Ascending | Descending
    /// The type representing an query expression.
    and QueryExpr = 
        | Source of Expression * Type
        | Generate of Expression * LambdaExpression * LambdaExpression * LambdaExpression
        | Transform of LambdaExpression * QueryExpr 
        | TransformIndexed of LambdaExpression * QueryExpr 
        | Filter of LambdaExpression * QueryExpr 
        | FilterIndexed of LambdaExpression * QueryExpr 
        | NestedQuery of (ParameterExpression * QueryExpr) * QueryExpr 
        | NestedQueryTransform of (ParameterExpression * QueryExpr) * LambdaExpression * QueryExpr 
        | Aggregate of Expression * LambdaExpression * QueryExpr
        | Sum of QueryExpr 
        | Count of QueryExpr 
        | Take of Expression * QueryExpr
        | TakeWhile of LambdaExpression * QueryExpr
        | Skip of Expression * QueryExpr 
        | ForEach of LambdaExpression * QueryExpr 
        | GroupBy of LambdaExpression * QueryExpr * Type
        | OrderBy of (LambdaExpression * Order) list * QueryExpr 
        | ToList of QueryExpr
        | ToArray of QueryExpr
        | RangeGenerator of Expression * Expression
        | RepeatGenerator of Expression * Expression
//        | ZipWith of (Expression * Type) * (Expression * Type) * LambdaExpression
        with

        member self.Type = 
            match self with
            | Source (_, t) -> t
            | Generate(_,_,_, selector) -> selector.Body.Type
            | Transform (lambda, _) -> lambda.Body.Type
            | TransformIndexed (lambda, _) -> lambda.Body.Type
            | Filter (_, q) -> q.Type
            | FilterIndexed (_, q) -> q.Type
            | NestedQuery ((_, q), _) -> q.Type
            | NestedQueryTransform ((_, _), resultSelector, _) -> resultSelector.Body.Type
            | Aggregate (seed, _, _) -> seed.Type
            | Sum (q) -> q.Type
            | Count (q) -> q.Type
            | Take (_, q) -> q.Type
            | TakeWhile(_,q) -> q.Type
            | Skip (_, q) -> q.Type
            | ForEach (_, _) -> typeof<Void>
            | GroupBy (_, _, t) -> t
            | OrderBy (_, q) -> q.Type
            | ToList q -> q.Type
            | ToArray q -> q.Type
            | RangeGenerator _ -> typeof<int>
            | RepeatGenerator (objExpr,_) -> objExpr.Type
//            | ZipWith (_,_,f) -> f.ReturnType

        static member AddOrderBy(keySelector : LambdaExpression, order : Order, queryExpr : QueryExpr) = 
            match queryExpr with
            | OrderBy (keySelectorOrderPairs, queryExpr') ->
                OrderBy ((keySelector, order) :: keySelectorOrderPairs, queryExpr')
            | _ -> OrderBy ([keySelector, order], queryExpr)
            
//        static member Range(start : int, count : int) : QueryExpr<IEnumerable<int>> =
//            if count < 0 || (int64 start + int64 count) - 1L > int64 Int32.MaxValue then 
//                raise <| ArgumentOutOfRangeException("count")
//            else
//                new QueryExpr<IEnumerable<int>>(RangeGenerator(start, count))
//
//        static member Repeat(element : 'T, count : int) : QueryExpr<IEnumerable<'T>> =
//            if count < 0 then
//                raise <| ArgumentOutOfRangeException("count")
//            else 
//                new QueryExpr<_>(RepeatGenerator(element, typeof<'T>, count))

//        static member Zip(left : IEnumerable<'T>, right : IEnumerable<'U>, 
//                          func : Expression<Func<'T,'U,'R>>) : QueryExpr<IEnumerable<'R>> =
//            let left  = Expression.Constant left  :> Expression , typeof<'T>
//            let right = Expression.Constant right :> Expression , typeof<'U>
//            new QueryExpr<_>(ZipWith(left, right, func))

            


        
        