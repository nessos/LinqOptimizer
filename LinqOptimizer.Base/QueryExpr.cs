using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Base
{
    public interface IQueryExpr
    {
        QueryExpr Expr { get; }
    }

    public interface IQueryExpr<out T> : IQueryExpr { }

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
