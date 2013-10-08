namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection

    // F# friendly class
    //[<CompiledName("FSharpQuery")>]
    type Query =
        static member ofSeq (source : IEnumerable<'T>) =
            ExtensionMethods.AsQueryExpr(source)

        static member compile<'T>(queryExpr : QueryExpr<'T>) = 
            ExtensionMethods.Compile(queryExpr).Invoke

        static member compile(queryExpr : QueryExprVoid)  =
            ExtensionMethods.Compile(queryExpr).Invoke

        static member run (source : QueryExpr<'T>) =
            ExtensionMethods.Run(source)

        static member run(queryExpr : QueryExprVoid) : unit =
            ExtensionMethods.Run(queryExpr)

        static member map<'T,'R> (projection : Expression<Func<'T,'R>>) = 
            fun (source : QueryExpr<IEnumerable<'T>>) -> 
                ExtensionMethods.Select(source, projection)

        static member map<'T, 'R>(selector : Expression<Func<'T, int, 'R>>) =
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.Select(queryExpr, selector)
                
        static member filter<'T>(predicate : Expression<Func<'T, bool>>) =
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.Where(queryExpr, predicate)

        static member where<'T>(predicate : Expression<Func<'T, bool>>) =
            Query.filter predicate

        static member where<'T>(predicate : Expression<Func<'T, int, bool>>) =
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.Where(queryExpr, predicate)

        static member filter<'T>(predicate : Expression<Func<'T, int, bool>>) =
            Query.where predicate

        static member fold(func : Expression<Func<'Acc, 'T, 'Acc>>) =
            fun (seed : 'Acc) (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.Aggregate(queryExpr, seed, func)

        static member sum (source : QueryExpr<IEnumerable<double>>) =
            ExtensionMethods.Sum(source)

        static member sum (source : QueryExpr<IEnumerable<int>>) =
            ExtensionMethods.Sum(source)

        static member length(queryExpr : QueryExpr<IEnumerable<'T>>) =
            ExtensionMethods.Count(queryExpr)

        static member collect<'T, 'Col, 'R>(collectionSelector : Expression<Func<'T, IEnumerable<'Col>>>, 
                                               resultSelector : Expression<Func<'T, 'Col, 'R>>) =
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.SelectMany(queryExpr, collectionSelector, resultSelector)

        static member collect<'T, 'R>(selector : Expression<Func<'T, IEnumerable<'R>>>) =
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.SelectMany(queryExpr, selector)

        static member take<'T>(n : int) =
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.Take(queryExpr, n)

        static member skip<'T>(n : int) =
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.Skip(queryExpr, n)

        static member iter<'T>(action : Expression<Action<'T>>) =
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.ForEach(queryExpr, action)

        static member groupBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.GroupBy(queryExpr, keySelector)

        static member sortBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.OrderBy(queryExpr, keySelector)

        static member sortByDescending<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (queryExpr : QueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.OrderByDescending(queryExpr, keySelector)

        static member toArray(queryExpr : QueryExpr<IEnumerable<'T>>) =
            ExtensionMethods.ToArray(queryExpr)

//        static member range (start : int) (count : int) =
//            QueryExpr.Range(start, count)
//
//        static member repeat (elem : 'T) (count : int) =
//            QueryExpr.Repeat(elem, count)

        static member zipWith (func : Expression<Func<'T,'U,'R>>) =
            fun (first : seq<'T>) (second : seq<'U>) ->
                QueryExpr.Zip(first, second, func)
        

    type ParallelQuery =
        static member ofSeq(source : IEnumerable<'T>) = 
            ParallelQuery.ofQuery(source.AsParallel())

        static member ofQuery(parallelQuery : ParallelQuery<'T>) = 
            ExtensionMethods.AsQueryExpr(parallelQuery)

        static member compile<'T>(queryExpr : ParallelQueryExpr<'T>) =
            ExtensionMethods.Compile(queryExpr).Invoke

        static member run<'T>(queryExpr : ParallelQueryExpr<'T>) : 'T =
            ExtensionMethods.Run(queryExpr)

        static member map<'T, 'R>(selector : Expression<Func<'T, 'R>>) =
            fun (queryExpr : ParallelQueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.Select(queryExpr, selector)

        static member where<'T>(predicate : Expression<Func<'T, bool>>) =
            fun (queryExpr : ParallelQueryExpr<IEnumerable<'T>>) ->
                ExtensionMethods.Where(queryExpr, predicate)

        static member filter<'T>(predicate : Expression<Func<'T, bool>>) =
            ParallelQuery.where predicate

