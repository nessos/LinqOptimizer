using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqOptimizer.Base;
using LinqOptimizer.Core;

namespace LinqOptimizer.CSharp
{
    /// <summary>
    /// Provides a set of static methods for querying objects that implement IQueryExpr.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Enables the optimization of a query.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An IEnumerable to convert to an IQueryExpr.</param>
        /// <returns>A query that returns the elements of the source sequence.</returns>
        public static IQueryExpr<IEnumerable<TSource>> AsQueryExpr<TSource>(this IEnumerable<TSource> source)
        { 
            return new QueryExpr<IEnumerable<TSource>>(CoreHelpers.AsQueryExpr(source, typeof(TSource)));
        }

        #region Compile methods
        /// <summary>
        /// Compiles a query to optimized code that can by invoked using a Func.
        /// </summary>
        /// <param name="query">The query to compile</param>
        /// <returns>A Func containing optimized code.</returns>
        public static Func<TQuery> Compile<TQuery>(this IQueryExpr<TQuery> query)
        {
            return CoreHelpers.Compile<TQuery>(query.Expr, CSharpExpressionOptimizer.Optimize);
        }

        /// <summary>
        /// Compiles a query to optimized code that can by invoked using a Func. <para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <param name="query">The query to compile</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A Func containing optimized code.</returns>
        public static Func<TQuery> Compile<TQuery>(this IQueryExpr<TQuery> query, bool enableNonPublicMemberAccess)
        {
            return CoreHelpers.Compile<TQuery>(query.Expr, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        }

        /// <summary>
        /// Compiles a query to optimized code that can by invoked using a Func.
        /// </summary>
        /// <param name="query">The query to compile</param>
        /// <returns>A Func containing optimized code.</returns>
        public static Action Compile(this IQueryExpr query)
        {
            return CoreHelpers.Compile(query.Expr, CSharpExpressionOptimizer.Optimize);
        }

        /// <summary>
        /// Compiles a query to optimized code that can by invoked using a Func.<para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <param name="query">The query to compile</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A Func containing optimized code.</returns>
        public static Action Compile(this IQueryExpr query, bool enableNonPublicMemberAccess)
        {
            return CoreHelpers.Compile(query.Expr, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        } 
        #endregion

        #region Run methods
        /// <summary>
        /// Compiles a query to optimized code, runs the code and returns the result.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <returns>The result of the query.</returns>
        public static TQuery Run<TQuery>(this IQueryExpr<TQuery> query)
        {
            return query.Compile<TQuery>().Invoke();
        }

        /// <summary>
        /// Compiles a query to optimized code, runs the code and returns the result. <para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>The result of the query.</returns>
        public static TQuery Run<TQuery>(this IQueryExpr<TQuery> query, bool enableNonPublicMemberAccess)
        {
            return query.Compile<TQuery>(enableNonPublicMemberAccess).Invoke();
        }

        /// <summary>
        /// Compiles a query to optimized code, runs the code and returns the result.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <returns>The result of the query.</returns>
        public static void Run(this IQueryExpr query)
        {
            query.Compile().Invoke();
        } 

        /// <summary>
        /// Compiles a query to optimized code, runs the code and returns the result.<para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>The result of the query.</returns>
        public static void Run(this IQueryExpr query, bool enableNonPublicMemberAccess)
        {
            query.Compile(enableNonPublicMemberAccess).Invoke();
        }
        #endregion

        #region Compiled templates
        /// <summary>
        /// Precompiles a paramiterized query to optimized code that can by invoked using a Func.
        /// </summary>
        /// <typeparam name="TSource">The type of the query parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query.</typeparam>
        /// <param name="template">The paramiterized query.</param>
        /// <returns>A delegate to the optimized query.</returns>
        public static Func<TSource, TResult> Compile<TSource, TResult>(this Expression<Func<TSource, IQueryExpr<TResult>>> template)
        {
            return Extensions.Compile<TSource, TResult>(template, false);
        }

        /// <summary>
        /// Precompiles a paramiterized query to optimized code that can by invoked using a Func.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="TSource">The type of the query parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query.</typeparam>
        /// <param name="template">The paramiterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        public static Func<TSource, TResult> Compile<TSource, TResult>(this Expression<Func<TSource, IQueryExpr<TResult>>> template, bool enableNonPublicMemberAccess)
        {
            var param = template.Parameters.Single();
            var query = CSharpExpressionOptimizer.ToQueryExpr(template.Body);
            return CoreHelpers.CompileTemplate<TSource, TResult>(param, query, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        }

        /// <summary>
        /// Precompiles a paramiterized query to optimized code that can by invoked using a Func.
        /// </summary>
        /// <typeparam name="TSource">The type of the query parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query.</typeparam>
        /// <param name="template">The paramiterized query.</param>
        /// <returns>A delegate to the optimized query.</returns>
        public static Action<TSource> Compile<TSource>(this Expression<Func<TSource, IQueryExpr>> template)
        {
            return Extensions.Compile(template, false);
        }

        /// <summary>
        /// Precompiles a paramiterized query to optimized code that can by invoked using a Func.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="TSource">The type of the query parameter.</typeparam>
        /// <param name="template">The paramiterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        public static Action<TSource> Compile<TSource>(this Expression<Func<TSource, IQueryExpr>> template, bool enableNonPublicMemberAccess)
        {
            var param = template.Parameters.Single();
            var query = CSharpExpressionOptimizer.ToQueryExpr(template.Body);
            return CoreHelpers.CompileTemplate<TSource>(param, query, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        } 
        #endregion

        #region Combinators
        /// <summary>
        /// Creates a new query that projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the query.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by selector.</typeparam>
        /// <param name="query">A query whose values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A query whose elements will be the result of invoking the transform function on each element of source.</returns>
        public static IQueryExpr<IEnumerable<TResult>> Select<TSource, TResult>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TResult>> selector)
        {
            return new QueryExpr<IEnumerable<TResult>>(QueryExpr.NewTransform(selector, query.Expr));
        }

        /// <summary>
        /// Creates a new query that projects each element of a sequence into a new form by incorporating the element's index.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by selector.</typeparam>
        /// <param name="query">A query whose values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each source element; the second parameter of the function represents the index of the source element.</param>
        /// <returns>A query whose elements will be the result of invoking the transform function on each element of source.</returns>
        public static IQueryExpr<IEnumerable<TResult>> Select<TSource, TResult>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, int, TResult>> selector)
        {
            return new QueryExpr<IEnumerable<TResult>>(QueryExpr.NewTransformIndexed(selector, query.Expr));
        }

        /// <summary>
        /// Creates a new query that filters a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">An query whose values to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A query that contains elements from the input query that satisfy the condition.</returns>
        public static IQueryExpr<IEnumerable<TSource>> Where<TSource>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, bool>> predicate)
        {
            return new QueryExpr<IEnumerable<TSource>>(QueryExpr.NewFilter(predicate, query.Expr));
        }

        /// <summary>
        /// Creates a new query that filters a sequence of values based on a predicate. Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of query.</typeparam>
        /// <param name="query">An query whose values to filter.</param>
        /// <param name="predicate">A function to test each source element for a condition; the second parameter of the function represents the index of the source element.</param>
        /// <returns>A query that contains elements from the input query that satisfy the condition.</returns>
        public static IQueryExpr<IEnumerable<TSource>> Where<TSource>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, int, bool>> predicate)
        {
            return new QueryExpr<IEnumerable<TSource>>(QueryExpr.NewFilterIndexed(predicate, query.Expr));
        }

        /// <summary>
        /// Creates a new query that applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TAcc">The type of the accumulator value.</typeparam>
        /// <param name="query">An query whose values are used to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <returns>A query that returns the final accumulator value.</returns>
        public static IQueryExpr<TAcc> Aggregate<TSource, TAcc>(this IQueryExpr<IEnumerable<TSource>> query, TAcc seed, Expression<Func<TAcc, TSource, TAcc>> func)
        {
            return new QueryExpr<TAcc>(QueryExpr.NewAggregate(Expression.Constant(seed), func, query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of Double values.
        /// </summary>
        /// <param name="query">A query whose sequence of Double values to calculate the sum of.</param>
        /// <returns>A query that returns the sum of the values in the sequence.</returns>
        public static IQueryExpr<double> Sum(this IQueryExpr<IEnumerable<double>> query)
        {
            return new QueryExpr<double>(QueryExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of int values.
        /// </summary>
        /// <param name="query">A query whose sequence of int values to calculate the sum of.</param>
        /// <returns>A query that returns the sum of the values in the sequence.</returns>
        public static IQueryExpr<int> Sum(this IQueryExpr<IEnumerable<int>> query)
        {
            return new QueryExpr<int>(QueryExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that returns the number of elements in a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">A query whose elements will be count.</param>
        /// <returns>A query that returns the number of elements in the input sequence.</returns>
        public static IQueryExpr<int> Count<TSource>(this IQueryExpr<IEnumerable<TSource>> query)
        {
            return new QueryExpr<int>(QueryExpr.NewCount(query.Expr));
        }

        /// <summary>
        /// Creates a new query that projects each element of a sequence to an IEnumerable and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="query">A query whose values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A query whose elements are the result of invoking the one-to-many transform function on each element of the input sequence.</returns>
        public static IQueryExpr<IEnumerable<TResult>> SelectMany<TSource, TResult>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, IEnumerable<TResult>>> selector)
        {
            var paramExpr = selector.Parameters.Single();
            var bodyExpr = selector.Body;
            var nested = Tuple.Create(paramExpr, CSharpExpressionOptimizer.ToQueryExpr(bodyExpr));
            return new QueryExpr<IEnumerable<TResult>>(QueryExpr.NewNestedQuery(nested, query.Expr));
        }

        /// <summary>
        /// Creates a query that projects each element of a sequence to an IEnumerable, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TCol">The type of the intermediate elements collected by collectionSelector.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="query">A query whose values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>A query whose elements are the result of invoking the one-to-many transform function on each element of the input sequence and the result selector function on each element therein.</returns>
        public static IQueryExpr<IEnumerable<TResult>> SelectMany<TSource, TCol, TResult>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, IEnumerable<TCol>>> collectionSelector, Expression<Func<TSource, TCol, TResult>> resultSelector)
        {
            var paramExpr = collectionSelector.Parameters.Single();
            var bodyExpr = collectionSelector.Body;
            var nested = Tuple.Create(paramExpr, CSharpExpressionOptimizer.ToQueryExpr(bodyExpr));
            return new QueryExpr<IEnumerable<TResult>>(QueryExpr.NewNestedQueryTransform(nested, resultSelector, query.Expr));
        }

        /// <summary>
        /// Creates a query that returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to return elements from.</param>
        /// <param name="count">The number of elements to return.</param>
        /// <returns>A query that returns a sequence containing the specified number of elements from the start of the input sequence.</returns>
        public static IQueryExpr<IEnumerable<TSource>> Take<TSource>(this IQueryExpr<IEnumerable<TSource>> query, int count)
        {
            return new QueryExpr<IEnumerable<TSource>>(QueryExpr.NewTake(Expression.Constant(count), query.Expr));
        }

        /// <summary>
        /// Creates a query that returns elements from a sequence as long as a specified condition is true, and then skips the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A query that contains the elements from the input sequence that occur before the element at which the test no longer passes.</returns>
        public static IQueryExpr<IEnumerable<TSource>> TakeWhile<TSource>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, bool>> predicate)
        {
            return new QueryExpr<IEnumerable<TSource>>(QueryExpr.NewTakeWhile(predicate, query.Expr));
        }

        /// <summary>
        /// Creates a query that bypasses elements in a sequence as long as a specified condition is true and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A query that contains the elements from the input sequence starting at the first element in the linear series that does not pass the test specified by predicate.</returns>
        public static IQueryExpr<IEnumerable<TSource>> SkipWhile<TSource>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, bool>> predicate)
        {
            return new QueryExpr<IEnumerable<TSource>>(QueryExpr.NewSkipWhile(predicate, query.Expr));
        }

        /// <summary>
        /// A query that bypasses a specified number of elements in a sequence and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">A query to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A query that returns a sequence containing the elements that occur after the specified index in the input sequence.</returns>
        public static IQueryExpr<IEnumerable<TSource>> Skip<TSource>(this IQueryExpr<IEnumerable<TSource>> query, int count)
        {
            return new QueryExpr<IEnumerable<TSource>>(QueryExpr.NewSkip(Expression.Constant(count), query.Expr));
        }

        /// <summary>
        /// A query that performs the specified action on each element of the source query.
        /// </summary>
        /// <param name="query">An query to return elements from.</param>
        /// <param name="action">The Action delegate to perform on each element of the query.</param>
        /// <returns>A query that performs the action on each element.</returns>
        public static IQueryExpr ForEach<TSource>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Action<TSource>> action)
        {
            return new QueryExprVoid(QueryExpr.NewForEach(action, query.Expr));
        }

        /// <summary>
        /// A query that groups the elements of a sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>A query where each IGrouping element contains a sequence of objects and a key.</returns>
        public static IQueryExpr<IEnumerable<IGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new QueryExpr<IEnumerable<IGrouping<TKey, TSource>>>(QueryExpr.NewGroupBy(keySelector, query.Expr, typeof(IGrouping<TKey, TSource>)));
        }

        /// <summary>
        /// Creates a query that sorts the elements of a sequence in ascending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A query whose elements are sorted according to a key.</returns>
        public static IQueryExpr<IOrderedEnumerable<TSource>> OrderBy<TSource, TKey>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new QueryExpr<IOrderedEnumerable<TSource>>(QueryExpr.AddOrderBy(keySelector, Order.Ascending, query.Expr));
        }

        /// <summary>
        /// Creates a query that sorts the elements of a sequence in descending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A query whose elements are sorted in descending according to a key.</returns>
        public static IQueryExpr<IOrderedEnumerable<TSource>> OrderByDescending<TSource, TKey>(this IQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new QueryExpr<IOrderedEnumerable<TSource>>(QueryExpr.AddOrderBy(keySelector, Order.Descending, query.Expr));
        }

        /// <summary>
        /// A query that returns a List from an sequence of values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to create a List from.</param>
        /// <returns>A query that contains elements from the input sequence in a List form.</returns>
        public static IQueryExpr<List<TSource>> ToList<TSource>(this IQueryExpr<IEnumerable<TSource>> query)
        {
            return new QueryExpr<List<TSource>>(QueryExpr.NewToList(query.Expr));
        }

        /// <summary>
        /// A query that returns an array from an sequence of values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to create an array from.</param>
        /// <returns>A query that contains elements from the input sequence in an array form.</returns>
        public static IQueryExpr<TSource[]> ToArray<TSource>(this IQueryExpr<IEnumerable<TSource>> query)
        {
            return new QueryExpr<TSource[]>(QueryExpr.NewToArray(query.Expr));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements of a sequence in ascending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A query whose elements are sorted according to a key.</returns>
        public static IQueryExpr<IOrderedEnumerable<TSource>> ThenBy<TSource, TKey>(this IQueryExpr<IOrderedEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new QueryExpr<IOrderedEnumerable<TSource>>(QueryExpr.AddOrderBy(keySelector, Order.Ascending, query.Expr));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements of a sequence in descending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A query whose elements are sorted according to a key.</returns>
        public static IQueryExpr<IOrderedEnumerable<TSource>> ThenByDescending<TSource, TKey>(this IQueryExpr<IOrderedEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new QueryExpr<IOrderedEnumerable<TSource>>(QueryExpr.AddOrderBy(keySelector, Order.Descending, query.Expr));
        }

        /// <summary>        
        /// A query that generates a sequence by mimicking a for loop.
        /// </summary>        
        /// <typeparam name="TState">State type.</typeparam>        
        /// <typeparam name="TResult">Result sequence element type.</typeparam>        
        /// <param name="initialState">Initial state of the generator loop.</param>        
        /// <param name="condition">Loop condition.</param>        
        /// <param name="iterate">State update function to run after every iteration of the generator loop.</param>        
        /// <param name="resultSelector">Result selector to compute resulting sequence elements.</param>        
        /// <returns>A query whose elements are obtained by running the generator loop, yielding computed elements.</returns>
        public static IQueryExpr<IEnumerable<TResult>> Generate<TState, TResult>(TState initialState, Expression<Func<TState, bool>> condition, Expression<Func<TState, TState>> iterate, Expression<Func<TState, TResult>> resultSelector)
        {
            return new QueryExpr<IEnumerable<TResult>>(QueryExpr.NewGenerate(Expression.Constant(initialState), condition, iterate, resultSelector));
        }
        #endregion
    }
}
