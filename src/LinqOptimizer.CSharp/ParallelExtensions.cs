using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Nessos.LinqOptimizer.Base;
using Nessos.LinqOptimizer.Core;
using QExpr = Nessos.LinqOptimizer.Core.QueryExpr;

namespace Nessos.LinqOptimizer.CSharp
{
    /// <summary>
    /// Provides a set of static methods for querying objects that implement IParallelQExpr.
    /// </summary>
    public static class ParallelExtensions
    {
        /// <summary>
        /// Enables the optimization of a query in a parallel fashion.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An IEnumerable to convert to an IQExpr.</param>
        /// <returns>A parallel query that returns the elements of the source sequence.</returns>
        public static IParallelQueryExpr<IEnumerable<TSource>> AsParallelQueryExpr<TSource>(this IEnumerable<TSource> source)
        {
            return new ParallelQueryExpr<IEnumerable<TSource>>(QExpr.NewSource(Expression.Constant(source), typeof(TSource), QueryExprType.Parallel));
        }

        /// <summary>
        /// Compiles a parallel query to optimized code that can by invoked using a Func.
        /// </summary>
        /// <param name="query">The query to compile</param>
        /// <returns>A Func containing optimized code.</returns>
        public static Func<TQuery> Compile<TQuery>(this IParallelQueryExpr<TQuery> query)
        {
            return CoreHelpers.CompileToParallel<TQuery>(query.Expr, CSharpExpressionOptimizer.Optimize);
        }

        /// <summary>
        /// Compiles a parallel query to optimized code that can by invoked using a Func.<para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <param name="query">The query to compile</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A Func containing optimized code.</returns>
        public static Func<TQuery> Compile<TQuery>(this IParallelQueryExpr<TQuery> query, bool enableNonPublicMemberAccess)
        {
            return CoreHelpers.CompileToParallel<TQuery>(query.Expr, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        }

        /// <summary>
        /// Compiles a parallel query to optimized code, runs the code and returns the result.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <returns>The result of the query.</returns>
        public static TQuery Run<TQuery>(this IParallelQueryExpr<TQuery> query)
        {
            return query.Compile<TQuery>().Invoke();
        }

        /// <summary>
        /// Compiles a parallel query to optimized code, runs the code and returns the result.<para/>
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>The result of the query.</returns>
        public static TQuery Run<TQuery>(this IParallelQueryExpr<TQuery> query, bool enableNonPublicMemberAccess)
        {
            return query.Compile<TQuery>(enableNonPublicMemberAccess).Invoke();
        }

        /// <summary>
        /// Creates a new query that projects in parallel each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the query.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by selector.</typeparam>
        /// <param name="query">A query whose values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A query whose elements will be the result of invoking the transform function on each element of source, in parallel.</returns>
        public static IParallelQueryExpr<IEnumerable<TResult>> Select<TSource, TResult>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TResult>> selector)
        {
            return new ParallelQueryExpr<IEnumerable<TResult>>(QExpr.NewTransform(selector, query.Expr));
        }

        /// <summary>
        /// Creates a new query that filters in parallel a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">An query whose values to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A parallel query that contains elements from the input query that satisfy the condition.</returns>
        public static IParallelQueryExpr<IEnumerable<TSource>> Where<TSource>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, bool>> predicate)
        {
            return new ParallelQueryExpr<IEnumerable<TSource>>(QExpr.NewFilter(predicate, query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of Double values in parallel.
        /// </summary>
        /// <param name="query">A query whose sequence of Double values to calculate the sum of.</param>
        /// <returns>A parallel query that returns the sum of the values in the sequence.</returns>
        public static IParallelQueryExpr<double> Sum(this IParallelQueryExpr<IEnumerable<double>> query)
        {
            return new ParallelQueryExpr<double>(QExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of Long values in parallel.
        /// </summary>
        /// <param name="query">A query whose sequence of Long values to calculate the sum of.</param>
        /// <returns>A parallel query that returns the sum of the values in the sequence.</returns>
        public static IParallelQueryExpr<long> Sum(this IParallelQueryExpr<IEnumerable<long>> query)
        {
            return new ParallelQueryExpr<long>(QExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of int values in parallel.
        /// </summary>
        /// <param name="query">A query whose sequence of int values to calculate the sum of.</param>
        /// <returns>A parallel query that returns the sum of the values in the sequence.</returns>
        public static IParallelQueryExpr<int> Sum(this IParallelQueryExpr<IEnumerable<int>> query)
        {
            return new ParallelQueryExpr<int>(QExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new parallel query that projects each element of a sequence to an IEnumerable and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="query">A query whose values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A parallel query whose elements are the result of invoking the one-to-many transform function on each element of the input sequence.</returns>
        public static IParallelQueryExpr<IEnumerable<TResult>> SelectMany<TSource, TResult>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, IEnumerable<TResult>>> selector)
        {
            var paramExpr = selector.Parameters.Single();
            var bodyExpr =  selector.Body;
            var nested = Tuple.Create(paramExpr, CSharpExpressionOptimizer.ToQueryExpr(bodyExpr));
            return new ParallelQueryExpr<IEnumerable<TResult>>(QExpr.NewNestedQuery(nested, query.Expr));
        }

        /// <summary>
        /// Creates a parallel query that projects each element of a sequence to an IEnumerable, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TCol">The type of the intermediate elements collected by collectionSelector.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="query">A query whose values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>A parallel query whose elements are the result of invoking the one-to-many transform function on each element of the input sequence and the result selector function on each element therein.</returns>
        public static IParallelQueryExpr<IEnumerable<TResult>> SelectMany<TSource, TCol, TResult>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, IEnumerable<TCol>>> collectionSelector, Expression<Func<TSource, TCol, TResult>> resultSelector)
        {
            var paramExpr = collectionSelector.Parameters.Single();
            var bodyExpr =  collectionSelector.Body;
            var nested = Tuple.Create(paramExpr, CSharpExpressionOptimizer.ToQueryExpr(bodyExpr));
            return new ParallelQueryExpr<IEnumerable<TResult>>(QExpr.NewNestedQueryTransform(nested, resultSelector, query.Expr));
        }

        /// <summary>
        /// A parallel query that groups the elements of a sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>A parallel query where each IGrouping element contains a sequence of objects and a key.</returns>
        public static IParallelQueryExpr<IEnumerable<IGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new ParallelQueryExpr<IEnumerable<IGrouping<TKey, TSource>>>(QExpr.NewGroupBy(keySelector, query.Expr, typeof(IGrouping<TKey, TSource>)));
        }

        /// <summary>
        /// Creates a query that sorts in parallel the elements of a sequence in ascending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A parallel query whose elements are sorted according to a key.</returns>
        public static IParallelQueryExpr<IOrderedEnumerable<TSource>> OrderBy<TSource, TKey>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new ParallelQueryExpr<IOrderedEnumerable<TSource>>(QExpr.AddOrderBy(keySelector, Order.Ascending, query.Expr));
        }

        /// <summary>
        /// Creates a query that sorts in parallel the elements of a sequence in descending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A parallel query whose elements are sorted in descending according to a key.</returns>
        public static IParallelQueryExpr<IOrderedEnumerable<TSource>> OrderByDescending<TSource, TKey>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new ParallelQueryExpr<IOrderedEnumerable<TSource>>(QExpr.AddOrderBy(keySelector, Order.Descending, query.Expr));
        }

        /// <summary>
        /// Creates a new parallel query that returns the number of elements in a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">A query whose elements will be count.</param>
        /// <returns>A parallel query that returns the number of elements in the input sequence.</returns>
        public static IParallelQueryExpr<int> Count<TSource>(this IParallelQueryExpr<IEnumerable<TSource>> query)
        {
            return new ParallelQueryExpr<int>(QExpr.NewCount(query.Expr));
        }

        /// <summary>
        /// A parallel query that returns a List from an sequence of values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to create a List from.</param>
        /// <returns>A parallel query that contains elements from the input sequence in a List form.</returns>
        public static IParallelQueryExpr<List<TSource>> ToList<TSource>(this IParallelQueryExpr<IEnumerable<TSource>> query)
        {
            return new ParallelQueryExpr<List<TSource>>(QExpr.NewToList(query.Expr));
        }

        /// <summary>
        /// A parallel query that returns an array from an sequence of values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to create an array from.</param>
        /// <returns>A parallel query that contains elements from the input sequence in an array form.</returns>
        public static IParallelQueryExpr<TSource[]> ToArray<TSource>(this IParallelQueryExpr<IEnumerable<TSource>> query)
        {
            return new ParallelQueryExpr<TSource[]>(QExpr.NewToArray(query.Expr));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements of a sequence in parallel and in ascending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A parallel query whose elements are sorted according to a key.</returns>
        public static IParallelQueryExpr<IOrderedEnumerable<TSource>> ThenBy<TSource, TKey>(this IParallelQueryExpr<IOrderedEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new ParallelQueryExpr<IOrderedEnumerable<TSource>>(QExpr.AddOrderBy(keySelector, Order.Ascending, query.Expr));
        }

        /// <summary>
        /// Performs a subsequent ordering of the elements of a sequence in parallel and in descending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A parallel query whose elements are sorted according to a key.</returns>
        public static IParallelQueryExpr<IOrderedEnumerable<TSource>> ThenByDescending<TSource, TKey>(this IParallelQueryExpr<IOrderedEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new ParallelQueryExpr<IOrderedEnumerable<TSource>>(QExpr.AddOrderBy(keySelector, Order.Descending, query.Expr));
        }



        #region Compiled Funcs

        /// <summary>
        /// Precompiles a parameterized query to optimized parallel code that can by invoked using a Func.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T">The type of the query parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        public static Func<T, TResult> Compile<T, TResult>(this Expression<Func<T, IParallelQueryExpr<TResult>>> template, bool enableNonPublicMemberAccess = false)
        {
            var param = template.Parameters.ToArray();
            var query = CSharpExpressionOptimizer.ToQueryExpr(template.Body);
            return (Func<T, TResult>)CoreHelpers.CompileTemplateToParallelVariadic<TResult>(param, query, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        }

        /// <summary>
        /// Precompiles a parameterized query to optimized parallel code that can by invoked using a Func.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        public static Func<T1, T2, TResult> Compile<T1, T2, TResult>(this Expression<Func<T1, T2, IParallelQueryExpr<TResult>>> template, bool enableNonPublicMemberAccess = false)
        {
            var param = template.Parameters.ToArray();
            var query = CSharpExpressionOptimizer.ToQueryExpr(template.Body);
            return (Func<T1, T2, TResult>)CoreHelpers.CompileTemplateToParallelVariadic<TResult>(param, query, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        }

        /// <summary>
        /// Precompiles a parameterized query to optimized parallel code that can by invoked using a Func.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="T3">The type of the third query parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        public static Func<T1, T2, T3, TResult> Compile<T1, T2, T3, TResult>(this Expression<Func<T1, T2, T3, IParallelQueryExpr<TResult>>> template, bool enableNonPublicMemberAccess = false)
        {
            var param = template.Parameters.ToArray();
            var query = CSharpExpressionOptimizer.ToQueryExpr(template.Body);
            return (Func<T1, T2, T3, TResult>)CoreHelpers.CompileTemplateToParallelVariadic<TResult>(param, query, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        }

        /// <summary>
        /// Precompiles a parameterized query to optimized parallel code that can by invoked using a Func.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="T3">The type of the third query parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth query parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        public static Func<T1, T2, T3, T4, TResult> Compile<T1, T2, T3, T4, TResult>(this Expression<Func<T1, T2, T3, T4, IParallelQueryExpr<TResult>>> template, bool enableNonPublicMemberAccess = false)
        {
            var param = template.Parameters.ToArray();
            var query = CSharpExpressionOptimizer.ToQueryExpr(template.Body);
            return (Func<T1, T2, T3, T4, TResult>)CoreHelpers.CompileTemplateToParallelVariadic<TResult>(param, query, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        }

        /// <summary>
        /// Precompiles a parameterized query to optimized parallel code that can by invoked using a Func.
        /// <b>Warning</b> : Enabling non-public member access might lead to performance degradation.
        /// </summary>
        /// <typeparam name="T1">The type of the first query parameter.</typeparam>
        /// <typeparam name="T2">The type of the second query parameter.</typeparam>
        /// <typeparam name="T3">The type of the third query parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth query parameter.</typeparam>
        /// <typeparam name="T5">The type of the fifth query parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query.</typeparam>
        /// <param name="template">The parameterized query.</param>
        /// <param name="enableNonPublicMemberAccess">Enable or not non public member access from the compiled code.</param>
        /// <returns>A delegate to the optimized query.</returns>
        public static Func<T1, T2, T3, T4, T5, TResult> Compile<T1, T2, T3, T4, T5, TResult>(this Expression<Func<T1, T2, T3, T4, IParallelQueryExpr<TResult>>> template, bool enableNonPublicMemberAccess = false)
        {
            var param = template.Parameters.ToArray();
            var query = CSharpExpressionOptimizer.ToQueryExpr(template.Body);
            return (Func<T1, T2, T3, T4, T5, TResult>)CoreHelpers.CompileTemplateToParallelVariadic<TResult>(param, query, CSharpExpressionOptimizer.Optimize, enableNonPublicMemberAccess);
        }

        #endregion

    }
}
