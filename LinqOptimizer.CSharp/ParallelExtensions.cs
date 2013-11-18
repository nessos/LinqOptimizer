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
    /// Provides a set of static methods for querying objects that implement IParallelQueryExpr.
    /// </summary>
    public static class ParallelExtensions
    {
        /// <summary>
        /// Enables the optimization of a query in a parallel fashion.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">An IEnumerable to convert to an IQueryExpr.</param>
        /// <returns>A parallel query that returns the elements of the source sequence.</returns>
        public static IParallelQueryExpr<IEnumerable<TSource>> AsParallelQueryExpr<TSource>(this IEnumerable<TSource> source)
        {
            return new ParallelQueryExpr<IEnumerable<TSource>>(QueryExpr.NewSource(Expression.Constant(source), typeof(TSource)));
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
        /// Compiles a parallel query to optimized code, runs the code and returns the result.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <returns>The result of the query.</returns>
        public static TQuery Run<TQuery>(this IParallelQueryExpr<TQuery> query)
        {
            return query.Compile<TQuery>().Invoke();
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
            return new ParallelQueryExpr<IEnumerable<TResult>>(QueryExpr.NewTransform(selector, query.Expr));
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
            return new ParallelQueryExpr<IEnumerable<TSource>>(QueryExpr.NewFilter(predicate, query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of Double values in parallel.
        /// </summary>
        /// <param name="query">A query whose sequence of Double values to calculate the sum of.</param>
        /// <returns>A parallel query that returns the sum of the values in the sequence.</returns>
        public static IParallelQueryExpr<double> Sum(this IParallelQueryExpr<IEnumerable<double>> query)
        {
            return new ParallelQueryExpr<double>(QueryExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of int values in parallel.
        /// </summary>
        /// <param name="query">A query whose sequence of int values to calculate the sum of.</param>
        /// <returns>A parallel query that returns the sum of the values in the sequence.</returns>
        public static IParallelQueryExpr<int> Sum(this IParallelQueryExpr<IEnumerable<int>> query)
        {
            return new ParallelQueryExpr<int>(QueryExpr.NewSum(query.Expr));
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
            return new ParallelQueryExpr<IEnumerable<TResult>>(QueryExpr.NewNestedQuery(nested, query.Expr));
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
            return new ParallelQueryExpr<IEnumerable<TResult>>(QueryExpr.NewNestedQueryTransform(nested, resultSelector, query.Expr));
        }

        /// <summary>
        /// A parallel query that groups the elements of a sequence according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>A parallel query where each IGrouping<TKey, TElement> element contains a sequence of objects and a key.</returns>
        public static IParallelQueryExpr<IEnumerable<IGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new ParallelQueryExpr<IEnumerable<IGrouping<TKey, TSource>>>(QueryExpr.NewGroupBy(keySelector, query.Expr, typeof(IGrouping<TKey, TSource>)));
        }

        /// <summary>
        /// Creates a query that sorts in parallel the elements of a sequence in ascending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A parallel query whose elements are sorted according to a key.</returns>
        public static IParallelQueryExpr<IEnumerable<TSource>> OrderBy<TSource, TKey>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new ParallelQueryExpr<IEnumerable<TSource>>(QueryExpr.AddOrderBy(keySelector, Order.Ascending, query.Expr));
        }

        /// <summary>
        /// Creates a query that sorts in parallel the elements of a sequence in descending order according to a key.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="query">A query whose values to order.</param>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A parallel query whose elements are sorted in descending according to a key.</returns>
        public static IParallelQueryExpr<IEnumerable<TSource>> OrderByDescending<TSource, TKey>(this IParallelQueryExpr<IEnumerable<TSource>> query, Expression<Func<TSource, TKey>> keySelector)
        {
            return new ParallelQueryExpr<IEnumerable<TSource>>(QueryExpr.AddOrderBy(keySelector, Order.Descending, query.Expr));
        }

        /// <summary>
        /// Creates a new parallel query that returns the number of elements in a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">A query whose elements will be count.</param>
        /// <returns>A parallel query that returns the number of elements in the input sequence.</returns>
        public static IParallelQueryExpr<int> Count<TSource>(this IParallelQueryExpr<IEnumerable<TSource>> query)
        {
            return new ParallelQueryExpr<int>(QueryExpr.NewCount(query.Expr));
        }

        /// <summary>
        /// A parallel query that returns a List from an sequence of values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to create a List from.</param>
        /// <returns>A parallel query that contains elements from the input sequence in a List form.</returns>
        public static IParallelQueryExpr<List<TSource>> ToList<TSource>(this IParallelQueryExpr<IEnumerable<TSource>> query)
        {
            return new ParallelQueryExpr<List<TSource>>(QueryExpr.NewToList(query.Expr));
        }

        /// <summary>
        /// A parallel query that returns an array from an sequence of values.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to create an array from.</param>
        /// <returns>A parallel query that contains elements from the input sequence in an array form.</returns>
        public static IParallelQueryExpr<TSource[]> ToArray<TSource>(this IParallelQueryExpr<IEnumerable<TSource>> query)
        {
            return new ParallelQueryExpr<TSource[]>(QueryExpr.NewToArray(query.Expr));
        }

        //public static IParallelQueryExpr<IOrderedEnumerable<T>> ThenBy<T, Key>(this IParallelQueryExpr<IOrderedEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        //{
        //    throw new NotImplementedException();
        //    //return new ParallelQueryExpr<IOrderedEnumerable<T>>(QueryExpr.AddOrderBy(keySelector, Order.Ascending, query.Expr, typeof(T)));
        //}

        //public static IParallelQueryExpr<IOrderedEnumerable<T>> ThenByDescending<T, Key>(this IParallelQueryExpr<IOrderedEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        //{
        //    throw new NotImplementedException();
        //    //return new ParallelQueryExpr<IOrderedEnumerable<T>>(QueryExpr.AddOrderBy(keySelector, Order.Descending, query.Expr, typeof(T)));
        //}
    }
}
