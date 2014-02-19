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
    public static class Extensions
    {
        /// <summary>
        /// Enables a gpu query.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source array.</typeparam>
        /// <param name="source">An array to convert to an IGpuQueryExpr.</param>
        /// <returns>A query that returns the elements of the source array.</returns>
        public static IGpuQueryExpr<GpuArray<TSource>> AsGpuQueryExpr<TSource>(this GpuArray<TSource> source) where TSource : struct 
        {
            return new GpuQueryExpr<GpuArray<TSource>>(QueryExpr.NewSource(Expression.Constant(source), typeof(TSource), QueryExprType.Gpu));
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
        public static IGpuQueryExpr<GpuArray<TResult>> Select<TSource, TResult>(this IGpuQueryExpr<GpuArray<TSource>> query, Expression<Func<TSource, TResult>> selector) where TSource : struct 
                                                                                                                                                                          where TResult : struct
        {
            return new GpuQueryExpr<GpuArray<TResult>>(QueryExpr.NewTransform(selector, query.Expr));
        }

        /// <summary>
        /// Creates a new query that filters a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="query">An query whose values to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A query that contains elements from the input query that satisfy the condition.</returns>
        public static IGpuQueryExpr<GpuArray<TSource>> Where<TSource>(this IGpuQueryExpr<GpuArray<TSource>> query, Expression<Func<TSource, bool>> predicate) where TSource : struct
        {
            return new GpuQueryExpr<GpuArray<TSource>>(QueryExpr.NewFilter(predicate, query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of int values.
        /// </summary>
        /// <param name="query">A query whose sequence of int values to calculate the sum of.</param>
        /// <returns>A query that returns the sum of the values in the sequence.</returns>
        public static IGpuQueryExpr<int> Sum(this IGpuQueryExpr<GpuArray<int>> query) 
        {
            return new GpuQueryExpr<int>(QueryExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of float values.
        /// </summary>
        /// <param name="query">A query whose sequence of int values to calculate the sum of.</param>
        /// <returns>A query that returns the sum of the values in the sequence.</returns>
        public static IGpuQueryExpr<float> Sum(this IGpuQueryExpr<GpuArray<float>> query)
        {
            return new GpuQueryExpr<float>(QueryExpr.NewSum(query.Expr));
        }

        /// <summary>
        /// Creates a new query that computes the sum of a sequence of double values.
        /// </summary>
        /// <param name="query">A query whose sequence of int values to calculate the sum of.</param>
        /// <returns>A query that returns the sum of the values in the sequence.</returns>
        public static IGpuQueryExpr<double> Sum(this IGpuQueryExpr<GpuArray<double>> query)
        {
            return new GpuQueryExpr<double>(QueryExpr.NewSum(query.Expr));
        }

        #endregion
    }
}
