namespace Nessos.LinqOptimizer.FSharp
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open Nessos.LinqOptimizer.Base
    open Nessos.LinqOptimizer.Core

    /// Basic operations on parallel queries.
    type PQuery =

        ///<summary>Lifts a sequence to a parallel query that can be optimized.</summary>
        ///<param name="source">The source sequence.</param>
        ///<returns>The parallel query.</returns>
        static member ofSeq(source : seq<'T>) = 
             new ParallelQueryExpr<seq<'T>>(QueryExpr.Source(Expression.Constant(source), typeof<'T>, QueryExprType.Parallel)) :> IParallelQueryExpr<_>

        ///<summary>Compiles the parallel query and returns a delegate to the compiled code.</summary>
        ///<param name="query">The query to compile.</param>
        ///<returns>A delegate to the compiled code.</returns>
        static member compile<'T>(query : IParallelQueryExpr<'T>) =
            CoreHelpers.CompileToParallel<'T>(query.Expr, Func<_,_>(FSharpExpressionOptimizer.Optimize)).Invoke

        ///<summary>Compiles the parallel query and returns a delegate to the compiled code.<para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        ///</summary>
        ///<param name="query">The query to compile.</param>
        ///<param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        ///<returns>A delegate to the compiled code.</returns>
        static member compile<'T>(query : IParallelQueryExpr<'T>, enableNonPublicMemberAccess : bool) =
            CoreHelpers.CompileToParallel<'T>(query.Expr, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess).Invoke

        ///<summary>Compiles and runs the parallel query.</summary>
        ///<param name="query">The query to run.</param>
        ///<returns>The result of the query execution.</returns>
        static member run<'T>(query : IParallelQueryExpr<'T>) : 'T =
            (PQuery.compile query)()

        ///<summary>Compiles and runs the parallel query.<para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        ///</summary>
        ///<param name="query">The query to run.</param>
        ///<param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        ///<returns>The result of the query execution.</returns>
        static member run<'T>(query : IParallelQueryExpr<'T>, enableNonPublicMemberAccess : bool) : 'T =
            (PQuery.compile(query, enableNonPublicMemberAccess))()

        ///<summary>Constructs a parallel query that creates a new collection whose elements are the results of applying the given function to each of the elements of the collection.</summary>
        ///<param name="projection">A function to transform items from the input sequence.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A parallel query whose elements will be the result of applying the projection function to the elements of the input query.</returns>
        static member map<'T, 'R>(selector : Expression<Func<'T, 'R>>) =
            fun (query : IParallelQueryExpr<seq<'T>>) ->
                new ParallelQueryExpr<seq<'R>>(QueryExpr.Transform(selector, query.Expr)) :> IParallelQueryExpr<_>

        ///<summary>Constructs a parallel query that returns a new collection containing only the elements of the collection for which the given predicate returns true.</summary>
        ///<param name="predicate">A function to test whether each item in the input sequence should be included in the output.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A parallel query containing the result sequence.</returns>
        static member where<'T>(predicate : Expression<Func<'T, bool>>) =
            fun (query : IParallelQueryExpr<seq<'T>>) ->
                new ParallelQueryExpr<seq<'T>>(QueryExpr.Filter(predicate, query.Expr)) :> IParallelQueryExpr<_>

        ///<summary>Constructs a parallel query that returns a new collection containing only the elements of the collection for which the given predicate returns true.</summary>
        ///<param name="predicate">A function to test whether each item in the input sequence should be included in the output.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A parallel query containing the result sequence.</returns>
        static member filter<'T>(predicate : Expression<Func<'T, bool>>) =
            PQuery.where predicate

        ///<summary>Constructs a parallel query that returns the sum of the elements in the sequence.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A parallel query that contains the result of the computation.</returns>
        static member sum(query : IParallelQueryExpr<seq<int>>) =
            new ParallelQueryExpr<int>(QueryExpr.Sum(query.Expr)) :> IParallelQueryExpr<_>

        ///<summary>Constructs a parallel query that returns the sum of the elements in the sequence.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member sum(query : IParallelQueryExpr<seq<int64>>) =
            new ParallelQueryExpr<int64>(QueryExpr.Sum(query.Expr)) :> IParallelQueryExpr<_>

        ///<summary>Constructs a parallel query that returns the sum of the elements in the sequence.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member sum(query : IParallelQueryExpr<seq<float>>) =
            new ParallelQueryExpr<float>(QueryExpr.Sum(query.Expr)) :> IParallelQueryExpr<_>

        ///<summary>Constructs a parallel query that applies the given function to each element of the sequence and concatenates all the results.</summary>
        ///<param name="selector">A function to transform elements of the input sequence into the sequences that are concatenated.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member collect<'T,'R>(selector : Expression<Func<'T, IEnumerable<'R>>>) =
            fun (query : IParallelQueryExpr<seq<'T>>) ->
                let paramExpr, bodyExpr = selector.Parameters.Single(), selector.Body
                ParallelQueryExpr<seq<'R>>(NestedQuery ((paramExpr, FSharpExpressionOptimizer.ToQueryExpr bodyExpr), query.Expr)) :> IParallelQueryExpr<_>

        ///<summary>Constructs a parallel query that applies a key-generating function to each element of a sequence and yields a sequence of unique keys and a sequence of all elements that have each key.</summary>
        ///<param name="keySelector">A function that transforms an element of the sequence into a comparable key.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A parallel query that contains the result of the computation.</returns>
        static member groupBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (query : IParallelQueryExpr<seq<'T>>) ->
                let groupBy = ParallelQueryExpr<seq<IGrouping<'Key,'T>>>(GroupBy(keySelector, query.Expr, typeof<IGrouping<'Key,'T>>)) :> IParallelQueryExpr<_>
                PQuery.map (fun (grp : IGrouping<'Key, 'T>) -> (grp.Key, (grp :> seq<'T>))) groupBy

        ///<summary>Constructs a parallel query that applies a key-generating function to each element of a sequence and yields a sequence ordered by keys.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<param name="keySelector">A function to transform items of the input sequence into comparable keys.</param>
        ///<returns>A parallel query that contains sorted sequence.</returns>
        static member sortBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (query : IParallelQueryExpr<seq<'T>>) ->
                new ParallelQueryExpr<seq<'T>>(QueryExpr.OrderBy([keySelector :> LambdaExpression, Order.Ascending], query.Expr)) :> IParallelQueryExpr<_>

        ///<summary>Constructs a parallel query that yields a sequence ordered by keys.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A parallel query that contains sorted sequence.</returns> 
        static member sort<'T>(query : IParallelQueryExpr<seq<'T>>) =
            PQuery.sortBy (fun i -> i) query

        ///<summary>Constructs a parallel query that returns the length of the sequence.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A parallel query that contains the result of the computation.</returns>
        static member length(queryExpr : IParallelQueryExpr<seq<'T>>) =
            ParallelQueryExpr<int>(Count(queryExpr.Expr)) :> IParallelQueryExpr<int> 

        ///<summary>Constructs a parallel query that returns an array containing the elements of the source query.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A parallel query that contains the array.</returns>
        static member toArray(query : IParallelQueryExpr<seq<'T>>) =
            ParallelQueryExpr<'T []>(ToArray(query.Expr))


        //
        // Precompiled Funcs
        //

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T">The type of the query parameter.</typeparam>
        /// <typeparam name="R">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T,'R>(template : Expression<Func<'T, IParallelQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            (CoreHelpers.CompileTemplateToParallelVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T,'R>).Invoke

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="R">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T1,'T2,'R>(template : Expression<Func<'T1, 'T2, IParallelQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 ->
                (CoreHelpers.CompileTemplateToParallelVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T1, 'T2, 'R>).Invoke(t1,t2)

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="T3">The type of the third query parameter.</typeparam>
        /// <typeparam name="R">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T1,'T2,'T3,'R>(template : Expression<Func<'T1, 'T2, 'T3, IParallelQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 t3 ->
                (CoreHelpers.CompileTemplateToParallelVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T1, 'T2, 'T3, 'R>).Invoke(t1,t2,t3)

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="T3">The type of the third query parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth query parameter.</typeparam>
        /// <typeparam name="R">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T1,'T2,'T3,'T4,'R>(template : Expression<Func<'T1, 'T2, 'T3, 'T4, IParallelQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 t3 t4 ->
                (CoreHelpers.CompileTemplateToParallelVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T1, 'T2, 'T3, 'T4, 'R>).Invoke(t1,t2,t3,t4)

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="T3">The type of the third query parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth query parameter.</typeparam>
        /// <typeparam name="T5">The type of the fifth query parameter.</typeparam>
        /// <typeparam name="R">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T1,'T2,'T3,'T4,'T5,'R>(template : Expression<Func<'T1, 'T2, 'T3, 'T4, 'T5, IParallelQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 t3 t4 t5 ->
                (CoreHelpers.CompileTemplateToParallelVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T1, 'T2, 'T3, 'T4, 'T5, 'R>).Invoke(t1,t2,t3,t4,t5)
