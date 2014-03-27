using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nessos.LinqOptimizer.Core;

namespace Nessos.LinqOptimizer.Base
{
    /// <summary>
    ///  This interface represents an optimized query.
    /// </summary>
    public interface IQueryExpr
    {
        /// <summary>
        /// The expression representing the query.
        /// </summary>
        QueryExpr Expr { get; }
    }

    /// <summary>
    /// This interface represents an optimized query.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    public interface IQueryExpr<out TQuery> : IQueryExpr { }

    /// <summary>
    /// The concrete implementation of an optimized query.
    /// </summary>
    public class QueryExprVoid : IQueryExpr
    {
        private QueryExpr _expr;
        /// <summary>
        /// The expression representing the query.
        /// </summary>
        public QueryExpr Expr { get { return _expr; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExprVoid"/> class.
        /// </summary>
        /// <param name="query">The expression.</param>
        public QueryExprVoid(QueryExpr query)
        {
            _expr = query;
        }
    }

    /// <summary>
    /// The concrete implementation of an optimized query.
    /// </summary>
    /// <typeparam name="T">The type of the query.</typeparam>
    public class QueryExpr<T> : IQueryExpr<T>
    {
        private QueryExpr _expr;
        /// <summary>
        /// The expression representing the query.
        /// </summary>
        public QueryExpr Expr { get { return _expr; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExpr{T}"/> class.
        /// </summary>
        /// <param name="query">The expression.</param>
        public QueryExpr(QueryExpr query)
        {
            _expr = query;
        }
    }
}
