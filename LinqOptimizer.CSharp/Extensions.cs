using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqOptimizer.Core;

namespace LinqOptimizer.CSharp
{
    public interface IQueryExpr<out T>
    {
        QueryExpr Expression { get; }
    }

    public class QueryExpression<T> : IQueryExpr<T>
    {
        private QueryExpr _expr;
        public QueryExpr Expression { get { return _expr;  } }

        public QueryExpression(QueryExpr query)
        {
            _expr = query;
        }
    }

    public static class Extensions
    {
        public static IQueryExpr<IEnumerable<T>> AsQueryExpression<T>(this IEnumerable<T> source)
        {
            return new QueryExpression<IEnumerable<T>>(CoreExts.AsQueryExpr(source, typeof(T)));
        }

        public static Func<T> Compile<T>(this IQueryExpr<T> query) 
        {
            return CoreExts.Compile<T>(query.Expression);
        }

        public static T Run<T>(this IQueryExpr<T> query)
        {
            return query.Compile<T>().Invoke();
        }

        public static IQueryExpr<IEnumerable<R>> Select<T,R>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, R>> selector)
        {
            return new QueryExpression<IEnumerable<R>>(QueryExpr.NewTransform(selector, query.Expression, typeof(R)));
        }
    }
}
