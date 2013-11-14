using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Base
{
    /// <summary>
    ///  This class represents an optimized query.
    /// </summary>
    public interface IQueryExpr
    {
        QueryExpr Expr { get; }
    }

    /// <summary>
    /// This class represents an optimized query.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    public interface IQueryExpr<out TQuery> : IQueryExpr { }

    public class QueryExprVoid : IQueryExpr
    {
        private QueryExpr _expr;
        public QueryExpr Expr { get { return _expr; } }

        public QueryExprVoid(QueryExpr query)
        {
            _expr = query;
        }
    }

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
