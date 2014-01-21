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
        public static IGpuQueryExpr<TSource[]> AsGpuQueryExpr<TSource>(this TSource[] source)
        {
            return new GpuQueryExpr<TSource[]>(QueryExpr.NewSource(Expression.Constant(source), typeof(TSource), QueryExprType.Gpu));
        }

        

        #region Run methods
        /// <summary>
        /// Compiles a gpu query to gpu kernel code, runs the kernel and returns the result.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <returns>The result of the query.</returns>
        public static TQuery Run<TQuery>(this IGpuQueryExpr<TQuery> query)
        {
            return (TQuery)GpuHelpers.Run(query.Expr);
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
        public static IGpuQueryExpr<TResult[]> Select<TSource, TResult>(this IGpuQueryExpr<TSource[]> query, Expression<Func<TSource, TResult>> selector)
        {
            return new GpuQueryExpr<TResult[]>(QueryExpr.NewTransform(selector, query.Expr));
        }

        #endregion
    }
}
