namespace Nessos.LinqOptimizer.FSharp
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open Nessos.LinqOptimizer.Base
    open Nessos.LinqOptimizer.Core

    /// Basic operations on queries.
    type Query =

        ///<summary>Lifts a sequence to a query that can be optimized.</summary>
        ///<param name="source">The source sequence.</param>
        ///<returns>The query.</returns>
        static member ofSeq (source : seq<'T>) =
            QueryExpr<seq<'T>>(CoreHelpers.AsQueryExpr(source, typeof<'T>)) :> IQueryExpr<_>

        ///<summary>Compiles the query and returns a delegate to the compiled code.</summary>
        ///<param name="query">The query to compile.</param>
        ///<returns>A delegate to the compiled code.</returns>
        static member compile<'T>(query : IQueryExpr<'T>) = 
            CoreHelpers.Compile<'T>(query.Expr, Func<_,_>(FSharpExpressionOptimizer.Optimize)).Invoke

        ///<summary>Compiles the query and returns a delegate to the compiled code.<para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        ///<param name="query">The query to compile.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        ///<returns>A delegate to the compiled code.</returns>
        static member compile<'T>(query : IQueryExpr<'T>, enableNonPublicMemberAccess : bool) = 
            CoreHelpers.Compile<'T>(query.Expr, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess).Invoke
            
        ///<summary>Compiles the query and returns a delegate to the compiled code.</summary>
        ///<param name="query">The query to compile.</param>
        ///<returns>A delegate to the compiled code.</returns>
        static member compile(query : IQueryExpr)  =
            CoreHelpers.Compile(query.Expr, Func<_,_>(FSharpExpressionOptimizer.Optimize)).Invoke

        ///<summary>Compiles the query and returns a delegate to the compiled code.<para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        ///</summary>
        ///<param name="query">The query to compile.</param>
        ///<param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        ///<returns>A delegate to the compiled code.</returns>
        static member compile(query : IQueryExpr, enableNonPublicMemberAccess : bool)  =
            CoreHelpers.Compile(query.Expr, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess).Invoke

        ///<summary>Compiles and runs the query.</summary>
        ///<param name="query">The query to run.</param>
        ///<returns>The result of the query execution.</returns>
        static member run (source : IQueryExpr<'T>) : 'T =
            (Query.compile source)()

        ///<summary>Compiles and runs the query.<para/>
        ///<b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        ///</summary>
        ///<param name="query">The query to run.</param>
        ///<param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        ///<returns>The result of the query execution.</returns>
        static member run (source : IQueryExpr<'T>, enableNonPublicMemberAccess : bool) : 'T =
            (Query.compile(source, enableNonPublicMemberAccess))()

        ///<summary>Compiles and runs the query.</summary>
        ///<param name="query">The query to run.</param>
        static member run(source : IQueryExpr) : unit =
            (Query.compile source)()

        ///<summary>Compiles and runs the query.<para/>
        ///<b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        ///</summary>
        ///<param name="query">The query to run.</param>
        ///<param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        static member run(source : IQueryExpr, enableNonPublicMemberAccess : bool) : unit =
            (Query.compile(source, enableNonPublicMemberAccess))()

        ///<summary>Constructs a query that creates a new collection whose elements are the results of applying the given function to each of the elements of the collection.</summary>
        ///<param name="projection">A function to transform items from the input sequence.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query whose elements will be the result of applying the projection function to the elements of the input query.</returns>
        static member map<'T,'R> (mapping : Expression<Func<'T,'R>>) = 
            fun (query : IQueryExpr<seq<'T>>) -> 
                QueryExpr<seq<'R>>(Transform(mapping, query.Expr)) :> IQueryExpr<seq<'R>> 

        ///<summary>Constructs a query that creates a new collection whose elements are the results of applying the given function to each of the elements of the collection. The integer index passed to the function indicates the index (from 0) of element being transformed.</summary>
        ///<param name="mapping">A function to transform items from the input sequence that also supplies the current index.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query whose elements will be the result of applying the projection function to the elements of the input query.</returns>
        static member mapi<'T, 'R>(mapping : Expression<Func<'T, int, 'R>>) =
            fun (query : IQueryExpr<seq<'T>>) -> 
                QueryExpr<seq<'R>>(TransformIndexed(mapping, query.Expr)) :> IQueryExpr<seq<'R>> 

        ///<summary>Constructs a query that returns a new collection containing only the elements of the collection for which the given predicate returns true.</summary>
        ///<param name="predicate">A function to test whether each item in the input sequence should be included in the output.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query containing the result sequence.</returns>
        static member filter<'T>(predicate : Expression<Func<'T, bool>>) =
            fun (query : IQueryExpr<seq<'T>>) ->
                QueryExpr<seq<'T>>(Filter(predicate, query.Expr)) :> IQueryExpr<seq<'T>> 

        ///<summary>Constructs a query that returns a new collection containing only the elements of the collection for which the given predicate returns true.</summary>
        ///<param name="predicate">A function to test whether each item in the input sequence should be included in the output.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query containing the result sequence.</returns>
        static member where<'T>(predicate : Expression<Func<'T, bool>>) =
            Query.filter predicate

        ///<summary>Constructs a query that applies a function to each element of the collection, threading an accumulator argument through the computation.</summary>
        ///<param name="func">A function that updates the state with each element from the sequence.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<param name="state">The initial state.</param>
        ///<returns>A query that contains the final result of the computation.</returns>
        static member fold(func : Expression<Func<'State, 'T, 'State>>) =
            fun (state : 'State) (query : IQueryExpr<seq<'T>>) ->
                QueryExpr<'State>(Aggregate(Expression.Constant(state), func, query.Expr)) :> IQueryExpr<'State> 

        ///<summary>Constructs a query that returns the sum of the elements in the sequence.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member sum (source : IQueryExpr<seq<double>>) =
            QueryExpr<double>(Sum(source.Expr)) :> IQueryExpr<double> 

        ///<summary>Constructs a query that returns the sum of the elements in the sequence.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member sum (source : IQueryExpr<seq<int64>>) =
            QueryExpr<int64>(Sum(source.Expr)) :> IQueryExpr<int64> 

        ///<summary>Constructs a query that returns the sum of the elements in the sequence.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member sum (source : IQueryExpr<seq<int>>) =
            QueryExpr<int>(Sum(source.Expr)) :> IQueryExpr<int> 

        ///<summary>Constructs a query that returns the length of the sequence.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member length(queryExpr : IQueryExpr<seq<'T>>) =
            QueryExpr<int>(Count(queryExpr.Expr)) :> IQueryExpr<int> 

        ///<summary>Constructs a query that applies the given function to each element of the sequence and concatenates all the results.</summary>
        ///<param name="selector">A function to transform elements of the input sequence into the sequences that are concatenated.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member collect<'T, 'R>(selector : Expression<Func<'T, seq<'R>>>) =
            fun (query : IQueryExpr<seq<'T>>) -> 
                let paramExpr, bodyExpr = selector.Parameters.Single(), selector.Body
                QueryExpr<seq<'R>>(NestedQuery ((paramExpr, FSharpExpressionOptimizer.ToQueryExpr bodyExpr), query.Expr)) :> IQueryExpr<_>

        ///<summary>Constructs a query that returns the elements of the sequence up to a specified count.</summary>
        ///<param name="count">The number of items to take.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member take<'T>(count : int) =
            fun (query : IQueryExpr<seq<'T>>) ->
                QueryExpr<seq<'T>>(Take(Expression.Constant count, query.Expr)) :> IQueryExpr<seq<'T>> 

        ///<summary>Constructs a query that returns a sequence that skips N elements of the underlying sequence and then yields the remaining elements of the sequence.</summary>
        ///<param name="count">The number of items to skip.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member skip<'T>(count : int) =
            fun (query : IQueryExpr<seq<'T>>) ->
                QueryExpr<seq<'T>>(Skip(Expression.Constant count, query.Expr)) :> IQueryExpr<seq<'T>> 

        ///<summary>Constructs a query that applies the given function to each element of the collection.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<param name="action">A function to apply to each element of the sequence.</param>
        static member iter<'T>(action : Expression<Action<'T>>) =
            fun (query : IQueryExpr<seq<'T>>) ->
                QueryExprVoid(ForEach(action, query.Expr)) :> IQueryExpr

        ///<summary>Constructs a query that applies a key-generating function to each element of a sequence and yields a sequence of unique keys and a sequence of all elements that have each key.</summary>
        ///<param name="keySelector">A function that transforms an element of the sequence into a comparable key.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member groupBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (query : IQueryExpr<seq<'T>>) ->
                let groupBy = QueryExpr<seq<IGrouping<'Key,'T>>>(GroupBy(keySelector, query.Expr, typeof<IGrouping<'Key,'T>>)) :> IQueryExpr<_>
                Query.map (fun (grp : IGrouping<'Key, 'T>) -> (grp.Key, (grp :> seq<'T>))) groupBy

        ///<summary>Constructs a query that applies a key-generating function to each element of a sequence and yields a sequence ordered by keys.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<param name="keySelector">A function to transform items of the input sequence into comparable keys.</param>
        ///<returns>A query that contains sorted sequence.</returns>
        static member sortBy<'T, 'Key>(keySelector : Expression<Func<'T, 'Key>>) = 
            fun (query : IQueryExpr<seq<'T>>) ->
                new QueryExpr<seq<'T>>(QueryExpr.OrderBy([keySelector :> LambdaExpression, Order.Ascending], query.Expr)) :> IQueryExpr<_>

        ///<summary>Constructs a query that yields a sequence ordered by keys.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains sorted sequence.</returns>
        static member sort<'T>(query : IQueryExpr<seq<'T>>) =
            Query.sortBy (fun i -> i) query

        ///<summary>Constructs a query that returns an array containing the elements of the source query.</summary>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the array.</returns>        
        static member toArray(query : IQueryExpr<seq<'T>>) =
            new QueryExpr<'T []>(ToArray(query.Expr))

        ///<summary>Constructs a query that skips elements of the underlying sequence while the given predicate returns true, and then yields the remaining elements of the sequence.</summary>
        ///<param name="predicate">A function that evaluates an element of the sequence to a Boolean value.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member skipWhile(predicate : Expression<Func<'T,bool>>) =
            fun (query : IQueryExpr<seq<'T>>) ->
                new QueryExpr<seq<'T>>(SkipWhile(predicate, query.Expr)) :> IQueryExpr<_>

        ///<summary>Constructs a query that when iterated, yields elements of the underlying sequence while the given predicate returns true, and then returns no further elements.</summary>
        ///<param name="predicate">A function that evaluates to false when no more items should be returned.</param>
        ///<param name="query">The query whose elements are used as input.</param>
        ///<returns>A query that contains the result of the computation.</returns>
        static member takeWhile(predicate : Expression<Func<'T,bool>>) =
            fun (query : IQueryExpr<seq<'T>>) ->
                new QueryExpr<seq<'T>>(TakeWhile(predicate, query.Expr)) :> IQueryExpr<_>

        /// <summary>
        /// Constructs a query that generates a sequence of integral numbers within a specified range.
        /// </summary>
        /// <param name="start">The value of the first integer in the sequence.</param>
        /// <param name="count">The number of sequential integers to generate.</param>
        /// <returns>A query that contains a range of sequential integral numbers.</returns>
        static member range(start : int, count : int) : IQueryExpr<IEnumerable<int>> =
            if count < 0 || (int64 start + int64 count) - 1L > int64 Int32.MaxValue then 
                raise <| ArgumentOutOfRangeException("count")
            else
                new QueryExpr<seq<int>>(RangeGenerator(Expression.Constant start, Expression.Constant count)) :> _

        /// <summary>
        /// Constructs a query that generates a sequence that contains one repeated value.
        /// </summary>
        /// <param name="element">The value to be repeated.</param>
        /// <param name="count">The number of sequential integers to generate.</param>
        /// <returns>A query that contains a repeated value.</returns>
        static member repeat(element : 'T, count : int) : IQueryExpr<IEnumerable<'T>> =
            if count < 0 then
                raise <| ArgumentOutOfRangeException("count")
            else 
                new QueryExpr<seq<'T>>(RepeatGenerator(Expression.Constant(element, typeof<'T>), Expression.Constant count)) :> _

        /// <summary>
        /// Constructs a query that applies a specified function to the corresponding elements of two sequences, producing a sequence of the results.
        /// </summary>
        /// <param name="first">The first sequence to merge.</param>
        /// <param name="second">The first sequence to merge.</param>
        /// <param name="func">A function that specifies how to merge the elements from the two sequences.</param>
        /// <returns>A query that contains merged elements of two input sequences.</returns>
        static member zipWith(left : IEnumerable<'T>, right : IEnumerable<'U>, func : Expression<Func<'T,'U,'R>>) : IQueryExpr<seq<'R>>  =
            let left  = Expression.Constant left  :> Expression 
            let right = Expression.Constant right :> Expression
            new QueryExpr<seq<'R>>(ZipWith(left, right, func)) :> _


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
        static member compile<'T,'R>(template : Expression<Func<'T, IQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            (CoreHelpers.CompileTemplateVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T,'R>).Invoke

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
        static member compile<'T1,'T2,'R>(template : Expression<Func<'T1, 'T2, IQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 ->
                (CoreHelpers.CompileTemplateVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T1, 'T2, 'R>).Invoke(t1,t2)

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
        static member compile<'T1,'T2,'T3,'R>(template : Expression<Func<'T1, 'T2, 'T3, IQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 t3 ->
                (CoreHelpers.CompileTemplateVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T1, 'T2, 'T3, 'R>).Invoke(t1,t2,t3)

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
        static member compile<'T1,'T2,'T3,'T4,'R>(template : Expression<Func<'T1, 'T2, 'T3, 'T4, IQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 t3 t4 ->
                (CoreHelpers.CompileTemplateVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T1, 'T2, 'T3, 'T4, 'R>).Invoke(t1,t2,t3,t4)

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
        static member compile<'T1,'T2,'T3,'T4,'T5,'R>(template : Expression<Func<'T1, 'T2, 'T3, 'T4, 'T5, IQueryExpr<'R>>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 t3 t4 t5 ->
                (CoreHelpers.CompileTemplateVariadic<'R>(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Func<'T1, 'T2, 'T3, 'T4, 'T5, 'R>).Invoke(t1,t2,t3,t4,t5)


        //
        // Precompiled Actions
        //

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T">The type of the query parameter.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T>(template : Expression<Func<'T, IQueryExpr>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            (CoreHelpers.CompileActionTemplateVariadic(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Action<'T>).Invoke

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T1,'T2>(template : Expression<Func<'T1, 'T2, IQueryExpr>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 ->
                (CoreHelpers.CompileActionTemplateVariadic(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Action<'T1, 'T2>).Invoke(t1,t2)

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="T3">The type of the third query parameter.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T1,'T2,'T3>(template : Expression<Func<'T1, 'T2, 'T3, IQueryExpr>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 t3 ->
                (CoreHelpers.CompileActionTemplateVariadic(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Action<'T1, 'T2, 'T3>).Invoke(t1,t2,t3)

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="T3">The type of the third query parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth query parameter.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T1,'T2,'T3,'T4>(template : Expression<Func<'T1, 'T2, 'T3, 'T4, IQueryExpr>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 t3 t4 ->
                (CoreHelpers.CompileActionTemplateVariadic(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Action<'T1, 'T2, 'T3, 'T4>).Invoke(t1,t2,t3,t4)

        /// <summary>
        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="T3">The type of the third query parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth query parameter.</typeparam>
        /// <typeparam name="T5">The type of the fifth query parameter.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        static member compile<'T1,'T2,'T3,'T4,'T5>(template : Expression<Func<'T1, 'T2, 'T3, 'T4, 'T5, IQueryExpr>>, ?enableNonPublicMemberAccess : bool) =
            let enableNonPublicMemberAccess = defaultArg enableNonPublicMemberAccess false
            let param = template.Parameters.ToArray()
            let query = FSharpExpressionOptimizer.ToQueryExpr(template.Body)
            fun t1 t2 t3 t4 t5 ->
                (CoreHelpers.CompileActionTemplateVariadic(param, query, Func<_,_>(FSharpExpressionOptimizer.Optimize), enableNonPublicMemberAccess) :?> Action<'T1, 'T2, 'T3, 'T4, 'T5>).Invoke(t1,t2,t3,t4,t5)






//        /// <summary>
//        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
//        /// </summary>
//        /// <typeparam name="T">The type of the query parameter.</typeparam>
//        /// <param name="template">The parameterized query.</param>
//        /// <returns>A delegate to the optimized query.</returns>
//        static member compile<'T>(template : Expression<Func<'T, IQueryExpr>>) =
//            Query.compile<'T>(template, false)
//
//        /// <summary>
//        /// Precompiles a parameterized query to optimized code that can by invoked using a delegate.
//        /// </summary>
//        /// <typeparam name="T">The type of the query parameter.</typeparam>
//        /// <typeparam name="R">The type of the query.</typeparam>
//        /// <param name="template">The parameterized query.</param>
//        /// <returns>A delegate to the optimized query.</returns>
//        static member compile<'T,'R>(template : Expression<Func<'T, IQueryExpr<'R>>>) =
//            Query.compile<'T,'R>(template, false)