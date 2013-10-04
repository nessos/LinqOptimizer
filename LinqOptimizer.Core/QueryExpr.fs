namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection

    // Typed Wrapper for QueryExpr 
    type QueryExpr<'T>(queryExpr : QueryExpr) =
        member self.QueryExpr = queryExpr 
    and ParallelQueryExpr<'T>(queryExpr : QueryExpr) =
        member self.QueryExpr = queryExpr 
    // C# friendly QueryExpr<unit>
    and QueryExprVoid(queryExpr : QueryExpr) =
        member self.QueryExpr = queryExpr 
    and ParallelQueryExprVoid(queryExpr : QueryExpr) =
        member self.QueryExpr = queryExpr 
    // Main Query representation
    and Order = Ascending | Descending
    and QueryExpr = 
        | Source of Expression * Type
        | Transform of LambdaExpression * QueryExpr * Type
        | TransformIndexed of LambdaExpression * QueryExpr * Type
        | Filter of LambdaExpression * QueryExpr * Type
        | FilterIndexed of LambdaExpression * QueryExpr * Type
        | NestedQuery of (ParameterExpression * QueryExpr) * QueryExpr * Type
        | NestedQueryTransform of (ParameterExpression * QueryExpr) * LambdaExpression * QueryExpr * Type
        | Aggregate of (obj *  Type) * LambdaExpression * QueryExpr
        | Sum of QueryExpr * Type 
        | Count of QueryExpr * Type
        | Take of Expression * QueryExpr * Type
        | Skip of Expression * QueryExpr * Type
        | ForEach of LambdaExpression * QueryExpr 
        | GroupBy of LambdaExpression * QueryExpr * Type
        | OrderBy of LambdaExpression * Order * QueryExpr * Type
        | ToList of QueryExpr
        | ToArray of QueryExpr
        | RangeGenerator of int * int
        | RepeatGenerator of obj * Type * int
        | ZipWith of (Expression * Type) * (Expression * Type) * LambdaExpression
        with

        member self.Type = 
            match self with
            | Source (_, t) -> t
            | Transform (_, _, t) -> t
            | TransformIndexed (_, _, t) -> t
            | Filter (_, _, t) -> t
            | FilterIndexed (_, _, t) -> t
            | NestedQuery (_, _, t) -> t 
            | NestedQueryTransform (_, _, _, t) -> t
            | Aggregate ((_, t), _, _) -> t
            | Sum (_, t) -> t
            | Count (_, t) -> t
            | Take (_, _, t) -> t
            | Skip (_, _, t) -> t
            | ForEach (_, _) -> typeof<Void>
            | GroupBy (_, _, t) -> t
            | OrderBy (_, _, _, t) -> t
            | ToList q -> q.Type
            | ToArray q -> q.Type
            | RangeGenerator _ -> typeof<int>
            | RepeatGenerator (_,t,_) -> t
            | ZipWith (_,_,f) -> f.ReturnType
     
        static member Range(start : int, count : int) : QueryExpr<IEnumerable<int>> =
            if count < 0 || (int64 start + int64 count) - 1L > int64 Int32.MaxValue then 
                raise <| ArgumentOutOfRangeException("count")
            else
                new QueryExpr<IEnumerable<int>>(RangeGenerator(start, count))

        static member Repeat(element : 'T, count : int) : QueryExpr<IEnumerable<'T>> =
            if count < 0 then
                raise <| ArgumentOutOfRangeException("count")
            else 
                new QueryExpr<_>(RepeatGenerator(element, typeof<'T>, count))

        static member Zip(left : IEnumerable<'T>, right : IEnumerable<'U>, 
                          func : Expression<Func<'T,'U,'R>>) : QueryExpr<IEnumerable<'R>> =
            let left  = Expression.Constant left  :> Expression , typeof<'T>
            let right = Expression.Constant right :> Expression , typeof<'U>
            new QueryExpr<_>(ZipWith(left, right, func))

            


        
        