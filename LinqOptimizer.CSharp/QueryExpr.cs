using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Nessos.LinqOptimizer.Base;
using QExpr = Nessos.LinqOptimizer.Core.QueryExpr;

namespace Nessos.LinqOptimizer.CSharp
{
    /// <summary>
    /// Provides a set of static methods for creating queries.
    /// </summary>
    public static class QueryExpr
    {
        /// <summary>
        /// Creates a new query that generates a sequence of integral numbers within a specified range.
        /// </summary>
        /// <param name="start">The value of the first integer in the sequence.</param>
        /// <param name="count">The number of sequential integers to generate.</param>
        /// <returns>A query that contains a range of sequential integral numbers.</returns>
        public static IQueryExpr<IEnumerable<int>> Range(int start, int count)
        {
            if(count < 0 || ((long)start + (long) count) - 1L > Int64.MaxValue)
                throw new ArgumentOutOfRangeException("count");
            else
                return new QueryExpr<IEnumerable<int>>(QExpr.NewRangeGenerator(Expression.Constant(start), Expression.Constant(count)));
        }

        /// <summary>
        /// Creates a new query that generates a sequence that contains one repeated value.
        /// </summary>
        /// <param name="element">The value to be repeated.</param>
        /// <param name="count">The number of sequential integers to generate.</param>
        /// <returns>A query that contains a repeated value.</returns>
        public static IQueryExpr<IEnumerable<TResult>> Repeat<TResult>(TResult element, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            else
                return new QueryExpr<IEnumerable<TResult>>(QExpr.NewRepeatGenerator(Expression.Constant(element, typeof(TResult)), Expression.Constant(count)));
        }

        /// <summary>
        /// Creates a query that applies a specified function to the corresponding elements of two sequences, producing a sequence of the results.
        /// </summary>
        /// <param name="first">The first sequence to merge.</param>
        /// <param name="second">The first sequence to merge.</param>
        /// <param name="resultSelector">A function that specifies how to merge the elements from the two sequences.</param>
        /// <returns>A query that contains merged elements of two input sequences.</returns>
        public static IQueryExpr<IEnumerable<TResult>> Zip<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Expression<Func<TFirst, TSecond, TResult>> resultSelector)
        {
            return new QueryExpr<IEnumerable<TResult>>(QExpr.NewZipWith(Expression.Constant(first), Expression.Constant(second), resultSelector));
        }
    }
}
