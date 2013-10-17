using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Base
{
    public interface IParallelQueryExpr
    {
        QueryExpr Expr { get; }
    }

    public interface IParallelQueryExpr<out T> : IParallelQueryExpr { }

    public class ParallelQueryExprVoid : IParallelQueryExpr
    {
        private QueryExpr _expr;
        public QueryExpr Expr { get { return _expr; } }

        public ParallelQueryExprVoid(QueryExpr query)
        {
            _expr = query;
        }
    }

    public class ParallelQueryExpr<T> : IParallelQueryExpr<T>
    {
        private QueryExpr _expr;
        public QueryExpr Expr { get { return _expr; } }

        public ParallelQueryExpr(QueryExpr query)
        {
            _expr = query;
        }
    }
}
