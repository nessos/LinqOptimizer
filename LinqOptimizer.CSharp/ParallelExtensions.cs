using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqOptimizer.Base;
using LinqOptimizer.Core;

namespace LinqOptimizer.CSharp
{
    public static class ParallelExtensions
    {
        public static IParallelQueryExpr<IEnumerable<T>> AsParallelQueryExpr<T>(this IEnumerable<T> source)
        {
            return new ParallelQueryExpr<IEnumerable<T>>(QueryExpr.NewSource(Expression.Constant(source), typeof(T)));
        }

        public static Func<T> Compile<T>(this IParallelQueryExpr<T> query)
        {
            return CoreExts.CompileToParallel<T>(query.Expr);
        }

        public static T Run<T>(this IParallelQueryExpr<T> query)
        {
            return query.Compile<T>().Invoke();
        }

        public static IParallelQueryExpr<IEnumerable<R>> Select<T, R>(this IParallelQueryExpr<IEnumerable<T>> query, Expression<Func<T, R>> selector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(selector);
            return new ParallelQueryExpr<IEnumerable<R>>(QueryExpr.NewTransform(f, query.Expr, typeof(R)));
        }

        public static IParallelQueryExpr<IEnumerable<T>> Where<T>(this IParallelQueryExpr<IEnumerable<T>> query, Expression<Func<T, bool>> predicate)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(predicate);
            return new ParallelQueryExpr<IEnumerable<T>>(QueryExpr.NewFilter(f, query.Expr, typeof(T)));
        }

        public static IParallelQueryExpr<double> Sum(this IParallelQueryExpr<IEnumerable<double>> query)
        {
            return new ParallelQueryExpr<double>(QueryExpr.NewSum(query.Expr, typeof(double)));
        }

        public static IParallelQueryExpr<int> Sum(this IParallelQueryExpr<IEnumerable<int>> query)
        {
            return new ParallelQueryExpr<int>(QueryExpr.NewSum(query.Expr, typeof(int)));
        }

        public static IParallelQueryExpr<IEnumerable<R>> SelectMany<T, Col, R>(this IParallelQueryExpr<IEnumerable<T>> query, Expression<Func<T, IEnumerable<Col>>> collectionSelector, Expression<Func<T, Col, R>> resultSelector)
        {
            return new ParallelQueryExpr<IEnumerable<R>>(CoreExts.SelectManyCSharp<T, Col, R>(query.Expr, collectionSelector, resultSelector));
        }

        public static IParallelQueryExpr<IEnumerable<R>> SelectMany<T, R>(this IParallelQueryExpr<IEnumerable<T>> query, Expression<Func<T, IEnumerable<R>>> selector)
        {
            return new ParallelQueryExpr<IEnumerable<R>>(CoreExts.SelectManyCSharp<T, R>(query.Expr, selector));
        }

        public static IParallelQueryExpr<IEnumerable<IGrouping<Key, T>>> GroupBy<T, Key>(this IParallelQueryExpr<IEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(keySelector);
            return new ParallelQueryExpr<IEnumerable<IGrouping<Key, T>>>(QueryExpr.NewGroupBy(f, query.Expr, typeof(IGrouping<Key, T>)));
        }

        public static IParallelQueryExpr<IEnumerable<T>> OrderBy<T, Key>(this IParallelQueryExpr<IEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(keySelector);
            return new ParallelQueryExpr<IEnumerable<T>>(QueryExpr.AddOrderBy(f, Order.Ascending, query.Expr, typeof(T)));
        }

        public static IParallelQueryExpr<IEnumerable<T>> OrderByDescending<T, Key>(this IParallelQueryExpr<IEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(keySelector);
            return new ParallelQueryExpr<IEnumerable<T>>(QueryExpr.AddOrderBy(f, Order.Descending, query.Expr, typeof(T)));
        }

        public static IParallelQueryExpr<IOrderedEnumerable<T>> ThenBy<T, Key>(this IParallelQueryExpr<IOrderedEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            throw new NotImplementedException();
            //return new ParallelQueryExpr<IOrderedEnumerable<T>>(QueryExpr.AddOrderBy(keySelector, Order.Ascending, query.Expr, typeof(T)));
        }

        public static IParallelQueryExpr<IOrderedEnumerable<T>> ThenByDescending<T, Key>(this IParallelQueryExpr<IOrderedEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            throw new NotImplementedException();
            //return new ParallelQueryExpr<IOrderedEnumerable<T>>(QueryExpr.AddOrderBy(keySelector, Order.Descending, query.Expr, typeof(T)));
        }
    }
}
