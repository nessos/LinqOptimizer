
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Base
{
    /// <summary>
    /// This interface represents an gpu query.
    /// </summary>
    public interface IGpuQueryExpr
    {
        /// <summary>
        /// The expression representing the query.
        /// </summary>
        QueryExpr Expr { get; }
    }

    /// <summary>
    /// This interface represents an gpu query.
    /// </summary>
    /// <typeparam name="T">The type of the query.</typeparam>
    public interface IGpuQueryExpr<out T> : IGpuQueryExpr { }


    /// <summary>
    /// The concrete implementation of an gpu query.
    /// </summary>
    public class GpuQueryExpr<T> : IGpuQueryExpr<T>
    {
        private QueryExpr _expr;
        /// <summary>
        /// The expression representing the query.
        /// </summary>
        public QueryExpr Expr { get { return _expr; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="GpuQueryExpr{T}"/> class.
        /// </summary>
        /// <param name="query">The expression.</param>
        public GpuQueryExpr(QueryExpr query)
        {
            _expr = query;
        }
    }
}
