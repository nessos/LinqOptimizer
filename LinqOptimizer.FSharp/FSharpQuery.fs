namespace LinqOptimizer.FSharp
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open LinqOptimizer.Base
    open LinqOptimizer.Core

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