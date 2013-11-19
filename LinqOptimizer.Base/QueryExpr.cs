using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Base
{
    /// <summary>
    ///  This interface represents an optimized query.
    /// </summary>
    public interface IQueryExpr
    {
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
        public QueryExpr Expr { get { return _expr; } }

        public QueryExprVoid(QueryExpr query)
        {
            _expr = query;
        }
    }

    /// <summary>
    /// The concrete implementation of an optimized query.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueryExpr<T> : IQueryExpr<T>
    {
        private QueryExpr _expr;
        public QueryExpr Expr { get { return _expr; } }

        public QueryExpr(QueryExpr query)
        {
            _expr = query;
        }
    }
}
