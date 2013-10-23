namespace LinqOptimizer.FSharp
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open LinqOptimizer.Base
    open LinqOptimizer.Core
    open LinqOptimizer.CSharp

    type Query =
        static member ofSeq (source : IEnumerable<'T>) =
            Extensions.AsQueryExpr(source)

        static member compile<'T>(queryExpr : IQueryExpr<'T>) = 
            Extensions.Compile(queryExpr).Invoke

        static member compile(queryExpr : IQueryExpr)  =
            Extensions.Compile(queryExpr).Invoke

        static member run (source : IQueryExpr<'T>) =
            Extensions.Run(source)

        static member run(queryExpr : IQueryExpr) : unit =
            Extensions.Run(queryExpr)

        static member map<'T,'R> (projection : Expression<Func<'T,'R>>) = 
            fun (source : IQueryExpr<IEnumerable<'T>>) -> 
                Extensions.Select(source, projection)

        static member mapi<'T, 'R>(selector : Expression<Func<'T, int, 'R>>) =
            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
                Extensions.Select(queryExpr, selector)

        static member filter<'T>(predicate : Expression<Func<'T, bool>>) =
            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
                Extensions.Where(queryExpr, predicate)

        static member where<'T>(predicate : Expression<Func<'T, bool>>) =
            Query.filter predicate

//        static member where<'T>(predicate : Expression<Func<'T, int, bool>>) =
//            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
//                Extensions.Where(queryExpr, predicate)
//
//        static member filter<'T>(predicate : Expression<Func<'T, int, bool>>) =
//            Query.where predicate
            
        static member fold(func : Expression<Func<'Acc, 'T, 'Acc>>) =
            fun (seed : 'Acc) (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
                Extensions.Aggregate(queryExpr, seed, func)

        static member sum (source : IQueryExpr<IEnumerable<double>>) =
            Extensions.Sum(source)

        static member sum (source : IQueryExpr<IEnumerable<int>>) =
            Extensions.Sum(source)

        static member length(queryExpr : IQueryExpr<IEnumerable<'T>>) =
            Extensions.Count(queryExpr)

//        static member collect<'T, 'Col, 'R>(collectionSelector : Expression<Func<'T, IEnumerable<'Col>>>, 
//                                               resultSelector : Expression<Func<'T, 'Col, 'R>>) =
//            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
//                Extensions.SelectMany(queryExpr, collectionSelector, resultSelector)

        static member collect<'T, 'R>(selector : Expression<Func<'T, IEnumerable<'R>>>) =
            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
                QueryExpr<IEnumerable<'R>>(CoreExts.SelectManyFSharp<'T, 'R>(queryExpr.Expr, selector)) :> IQueryExpr<IEnumerable<'R>>

        static member take<'T>(n : int) =
            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
                Extensions.Take(queryExpr, n)

        static member skip<'T>(n : int) =
            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
                Extensions.Skip(queryExpr, n)

        static member iter<'T>(action : Expression<Action<'T>>) =
            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
                Extensions.ForEach(queryExpr, action)

        static member groupBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
                Extensions.Select(
                    Extensions.GroupBy(queryExpr, keySelector),
                    (fun (grp : IGrouping<'Key, 'T>) -> (grp.Key, (grp :> IEnumerable<'T>))))
                
        static member sort<'T>(queryExpr : IQueryExpr<IEnumerable<'T>>) =
            Extensions.OrderBy(queryExpr, fun i -> i)

        static member sortBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
                Extensions.OrderBy(queryExpr, keySelector)
                
//        static member sortByDescending<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
//            fun (queryExpr : IQueryExpr<IEnumerable<'T>>) ->
//                Extensions.OrderByDescending(queryExpr, keySelector)
                
        static member toArray(queryExpr : IQueryExpr<IEnumerable<'T>>) =
            Extensions.ToArray(queryExpr)

//        static member range (start : int) (count : int) =
//            QueryExpr.Range(start, count)
//
//        static member repeat (elem : 'T) (count : int) =
//            QueryExpr.Repeat(elem, count)

//        static member zipWith (func : Expression<Func<'T,'U,'R>>) =
//            fun (first : seq<'T>) (second : seq<'U>) ->
//                QueryExpr.Zip(first, second, func)
        

    type ParallelQuery =
        static member ofSeq(source : IEnumerable<'T>) = 
            ParallelQuery.ofQuery(source.AsParallel())

        static member ofQuery(parallelQuery : ParallelQuery<'T>) = 
            ParallelExtensions.AsParallelQueryExpr(parallelQuery)

        static member compile<'T>(queryExpr : ParallelQueryExpr<'T>) =
            ParallelExtensions.Compile(queryExpr).Invoke

        static member run<'T>(queryExpr : ParallelQueryExpr<'T>) : 'T =
            ParallelExtensions.Run(queryExpr)

        static member map<'T, 'R>(selector : Expression<Func<'T, 'R>>) =
            fun (queryExpr : ParallelQueryExpr<IEnumerable<'T>>) ->
                ParallelExtensions.Select(queryExpr, selector)

        static member where<'T>(predicate : Expression<Func<'T, bool>>) =
            fun (queryExpr : ParallelQueryExpr<IEnumerable<'T>>) ->
                ParallelExtensions.Where(queryExpr, predicate)

        static member filter<'T>(predicate : Expression<Func<'T, bool>>) =
            ParallelQuery.where predicate

