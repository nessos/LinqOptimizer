using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqOptimizer.Base;
using LinqOptimizer.Core;
using LinqOptimizer.Gpu;

namespace LinqOptimizer.Gpu.CSharp
{
    /// <summary>
    /// Provides a set of static methods for querying objects that implement IGpuQueryExpr.
    /// </summary>
    public static class GpuQueryExpr
    {
        /// <summary>
        /// Enables a gpu query.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source array.</typeparam>
        /// <param name="source">An array to convert to an IGpuQueryExpr.</param>
        /// <returns>A query that returns the elements of the source array.</returns>
        public static IGpuQueryExpr<IGpuArray<TSource>> AsGpuQueryExpr<TSource>(this IGpuArray<TSource> source) 
        {
            return new GpuQueryExpr<IGpuArray<TSource>>(QueryExpr.NewSource(Expression.Constant(source), typeof(TSource), QueryExprType.Gpu));
        }

        #region Combinators
        /// <summary>
        /// Creates a new query that projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the query.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by selector.</typeparam>
        /// <param name="query">A query whose values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>A query whose elements will be the result of invoking the transform function on each element of source.</returns>
        public static IGpuQueryExpr<IGpuArray<TResult>> Select<TSource, TResult>(this IGpuQueryExpr<IGpuArray<TSource>> query, Expression<Func<TSource, TResult>> selector) 
        {
            return new GpuQueryExpr<IGpuArray<TResult>>(QueryExpr.NewTransform(selector, query.Expr));
        }

        /// <summary>
        /// Creates a new query that filters a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">An query whose values to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A query that contains elements from the input query that satisfy the condition.</returns>
        public static IGpuQueryExpr<IGpuArray<TSource>> Where<TSource>(this IGpuQueryExpr<IGpuArray<TSource>> query, Expression<Func<TSource, bool>> predicate) 
        {
            return new GpuQueryExpr<IGpuArray<TSource>>(QueryExpr.NewFilter(predicate, query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of int values.
        /// </summary>
        /// <param name="query">A query whose sequence of int values to calculate the sum of.</param>
        /// <returns>A query that returns the sum of the values in the gpu array.</returns>
        public static IGpuQueryExpr<int> Sum(this IGpuQueryExpr<IGpuArray<int>> query) 
        {
            return new GpuQueryExpr<int>(QueryExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of float values.
        /// </summary>
        /// <param name="query">A query whose sequence of int values to calculate the sum of.</param>
        /// <returns>A query that returns the sum of the values in the gpu array.</returns>
        public static IGpuQueryExpr<float> Sum(this IGpuQueryExpr<IGpuArray<float>> query)
        {
            return new GpuQueryExpr<float>(QueryExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of double values.
        /// </summary>
        /// <param name="query">A query whose sequence of int values to calculate the sum of.</param>
        /// <returns>A query that returns the sum of the values in the gpu array.</returns>
        public static IGpuQueryExpr<double> Sum(this IGpuQueryExpr<IGpuArray<double>> query)
        {
            return new GpuQueryExpr<double>(QueryExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that returns the number of elements in a gpu array.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">A query whose elements will be count.</param>
        /// <returns>A query that returns the number of elements in the gpu array.</returns>
        public static IGpuQueryExpr<int> Count<TSource>(this IGpuQueryExpr<IGpuArray<TSource>> query) 
        {
            return new GpuQueryExpr<int>(QueryExpr.NewCount(query.Expr));
        }

        /// <summary>
        /// A query that returns an array from an gpu array.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">The query to create an array from.</param>
        /// <returns>A query that contains elements from the gpu array in an array form.</returns>
        public static IGpuQueryExpr<TSource[]> ToArray<TSource>(this IGpuQueryExpr<IGpuArray<TSource>> query) 
        {
            return new GpuQueryExpr<TSource[]>(QueryExpr.NewToArray(query.Expr));
        }


        /// <summary>
        /// Creates a query that applies a specified function to the corresponding elements of two gpu arrays, producing a sequence of the results.
        /// </summary>
        /// <param name="first">The first gpu array to merge.</param>
        /// <param name="second">The first gpu array to merge.</param>
        /// <param name="resultSelector">A function that specifies how to merge the elements from the two gpu arrays.</param>
        /// <returns>A query that contains merged elements of two gpu arrays.</returns>
        public static IGpuQueryExpr<IGpuArray<TResult>> Zip<TFirst, TSecond, TResult>(IGpuArray<TFirst> first, IGpuArray<TSecond> second, Expression<Func<TFirst, TSecond, TResult>> resultSelector)
        {
            return new GpuQueryExpr<IGpuArray<TResult>>(QueryExpr.NewZipWith(Expression.Constant(first), Expression.Constant(second), resultSelector));
        }

        #endregion
    }
}
