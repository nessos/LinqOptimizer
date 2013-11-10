namespace LinqOptimizer.FSharp
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open LinqOptimizer.Base
    open LinqOptimizer.Core

    type Query =
        static member ofSeq (source : seq<'T>) =
            QueryExpr<seq<'T>>(CoreExts.AsQueryExpr(source, typeof<'T>)) :> IQueryExpr<_>

        static member compile<'T>(query : IQueryExpr<'T>) = 
            CoreExts.Compile<'T>(query.Expr).Invoke

        static member compile(query : IQueryExpr)  =
            CoreExts.Compile(query.Expr).Invoke

        static member run (source : IQueryExpr<'T>) : 'T =
            (Query.compile source)()

        static member run(source : IQueryExpr) : unit =
            (Query.compile source)()

        static member map<'T,'R> (projection : Expression<Func<'T,'R>>) = 
            fun (query : IQueryExpr<seq<'T>>) -> 
                let f = FSharpExpressionOptimizer.Optimize(projection) :?> LambdaExpression
                QueryExpr<seq<'R>>(Transform(f, query.Expr, typeof<'R>)) :> IQueryExpr<seq<'R>> 

        static member mapi<'T, 'R>(selector : Expression<Func<'T, int, 'R>>) =
            fun (query : IQueryExpr<seq<'T>>) -> 
                let f = FSharpExpressionOptimizer.Optimize(selector) :?> LambdaExpression
                QueryExpr<seq<'R>>(TransformIndexed(f, query.Expr, typeof<'R>)) :> IQueryExpr<seq<'R>> 

        static member filter<'T>(predicate : Expression<Func<'T, bool>>) =
            fun (query : IQueryExpr<seq<'T>>) ->
                let f = FSharpExpressionOptimizer.Optimize(predicate) :?> LambdaExpression
                QueryExpr<seq<'T>>(Filter(f, query.Expr, typeof<'T>)) :> IQueryExpr<seq<'T>> 

        static member where<'T>(predicate : Expression<Func<'T, bool>>) =
            Query.filter predicate

        static member fold(func : Expression<Func<'Acc, 'T, 'Acc>>) =
            fun (seed : 'Acc) (query : IQueryExpr<seq<'T>>) ->
                let f = FSharpExpressionOptimizer.Optimize(func) :?> LambdaExpression
                QueryExpr<'Acc>(Aggregate((seed :> obj, typeof<'Acc>), f, query.Expr)) :> IQueryExpr<'Acc> 

        static member sum (source : IQueryExpr<seq<double>>) =
            QueryExpr<double>(Sum(source.Expr, typeof<double>)) :> IQueryExpr<double> 

        static member sum (source : IQueryExpr<seq<int>>) =
            QueryExpr<int>(Sum(source.Expr, typeof<int>)) :> IQueryExpr<int> 

        static member length(queryExpr : IQueryExpr<seq<'T>>) =
            QueryExpr<int>(Count(queryExpr.Expr, typeof<'T>)) :> IQueryExpr<int> 

        static member collect<'T, 'R>(selector : Expression<Func<'T, seq<'R>>>) =
            fun (queryExpr : IQueryExpr<seq<'T>>) -> 
                let paramExpr, bodyExpr = selector.Parameters.Single(), selector.Body
                QueryExpr<seq<'R>>(NestedQuery ((paramExpr, FSharpExpressionOptimizer.ToQueryExpr bodyExpr), queryExpr.Expr, typeof<'R>)) :> IQueryExpr<_>

        static member take<'T>(n : int) =
            fun (query : IQueryExpr<seq<'T>>) ->
                QueryExpr<seq<'T>>(Take(Expression.Constant n, query.Expr, typeof<'T>)) :> IQueryExpr<seq<'T>> 

        static member skip<'T>(n : int) =
            fun (query : IQueryExpr<seq<'T>>) ->
                QueryExpr<seq<'T>>(Skip(Expression.Constant n, query.Expr, typeof<'T>)) :> IQueryExpr<seq<'T>> 

        static member iter<'T>(action : Expression<Action<'T>>) =
            fun (query : IQueryExpr<seq<'T>>) ->
                let f = FSharpExpressionOptimizer.Optimize(action) :?> LambdaExpression
                QueryExprVoid(ForEach(f, query.Expr)) :> IQueryExpr

        static member groupBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (query : IQueryExpr<seq<'T>>) ->
                let f = FSharpExpressionOptimizer.Optimize(keySelector) :?> LambdaExpression
                let groupBy = QueryExpr<seq<IGrouping<'Key,'T>>>(GroupBy(f, query.Expr, typeof<IGrouping<'Key,'T>>)) :> IQueryExpr<_>
                Query.map (fun (grp : IGrouping<'Key, 'T>) -> (grp.Key, (grp :> seq<'T>))) groupBy

        static member sortBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (queryExpr : IQueryExpr<seq<'T>>) ->
                let f =  FSharpExpressionOptimizer.Optimize(keySelector) :?> LambdaExpression
                new QueryExpr<seq<'T>>(QueryExpr.OrderBy([f, Order.Ascending], queryExpr.Expr, typeof<'T>)) :> IQueryExpr<_>
                
        static member sort<'T>(query : IQueryExpr<seq<'T>>) =
            Query.sortBy (fun i -> i) query

        static member toArray(query : IQueryExpr<seq<'T>>) =
            new QueryExpr<'T []>(ToArray(query.Expr))

    type ParallelQuery =
        static member ofSeq(source : seq<'T>) = 
             new ParallelQueryExpr<seq<'T>>(QueryExpr.Source(Expression.Constant(source), typeof<'T>)) :> IParallelQueryExpr<_>

        static member compile<'T>(query : IParallelQueryExpr<'T>) =
            CoreExts.CompileToParallel<'T>(query.Expr).Invoke

        static member run<'T>(query : IParallelQueryExpr<'T>) : 'T =
            (ParallelQuery.compile query)()

        static member map<'T, 'R>(selector : Expression<Func<'T, 'R>>) =
            fun (query : IParallelQueryExpr<seq<'T>>) ->
                let f = FSharpExpressionOptimizer.Optimize(selector) :?> LambdaExpression
                new ParallelQueryExpr<seq<'R>>(QueryExpr.Transform(f, query.Expr, typeof<'R>)) :> IParallelQueryExpr<_>

        static member where<'T>(predicate : Expression<Func<'T, bool>>) =
            fun (query : IParallelQueryExpr<seq<'T>>) ->
                let f = FSharpExpressionOptimizer.Optimize(predicate) :?> LambdaExpression
                new ParallelQueryExpr<seq<'T>>(QueryExpr.Filter(f, query.Expr, typeof<'T>)) :> IParallelQueryExpr<_>

        static member filter<'T>(predicate : Expression<Func<'T, bool>>) =
            ParallelQuery.where predicate


