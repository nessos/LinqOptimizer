using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nessos.LinqOptimizer.Core;

namespace Nessos.LinqOptimizer.Base
{
    /// <summary>
    /// This interface represents an optimized parallel query.
    /// </summary>
    public interface IParallelQueryExpr
    {
        /// <summary>
        /// The expression representing the query.
        /// </summary>
        QueryExpr Expr { get; }
    }

    /// <summary>
    /// This interface represents an optimized parallel query.
    /// </summary>
    /// <typeparam name="T">The type of the query.</typeparam>
    public interface IParallelQueryExpr<out T> : IParallelQueryExpr { }

    /// <summary>
    /// The concrete implementation of an optimized query.
    /// </summary>
    public class ParallelQueryExprVoid : IParallelQueryExpr
    {
        private QueryExpr _expr;
        /// <summary>
        /// The expression representing the query.
        /// </summary>
        public QueryExpr Expr { get { return _expr; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelQueryExprVoid"/> class.
        /// </summary>
        /// <param name="query">The expression.</param>
        public ParallelQueryExprVoid(QueryExpr query)
        {
            _expr = query;
        }
    }

    /// <summary>
    /// The concrete implementation of an optimized query.
    /// </summary>
    public class ParallelQueryExpr<T> : IParallelQueryExpr<T>
    {
        private QueryExpr _expr;
        /// <summary>
        /// The expression representing the query.
        /// </summary>
        public QueryExpr Expr { get { return _expr; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelQueryExpr{T}"/> class.
        /// </summary>
        /// <param name="query">The expression.</param>
        public ParallelQueryExpr(QueryExpr query)
        {
            _expr = query;
        }
    }
}
