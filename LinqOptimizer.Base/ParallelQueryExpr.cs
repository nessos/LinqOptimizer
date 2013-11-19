using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Base
{
    /// <summary>
    /// This interface represents an optimized parallel query.
    /// </summary>
    public interface IParallelQueryExpr
    {
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
        public QueryExpr Expr { get { return _expr; } }

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
        public QueryExpr Expr { get { return _expr; } }

        public ParallelQueryExpr(QueryExpr query)
        {
            _expr = query;
        }
    }
}
