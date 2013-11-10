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

    public static class Extensions
    {
        public static IQueryExpr<IEnumerable<T>> AsQueryExpr<T>(this IEnumerable<T> source)
        {
            return new QueryExpr<IEnumerable<T>>(CoreHelpers.AsQueryExpr(source, typeof(T)));
        }

        public static Func<T> Compile<T>(this IQueryExpr<T> query) 
        {
            return CoreHelpers.Compile<T>(query.Expr);
        }

        public static Action Compile(this IQueryExpr queryExpr)
        {
            return CoreHelpers.Compile(queryExpr.Expr);
        }

        public static T Run<T>(this IQueryExpr<T> query)
        {
            return query.Compile<T>().Invoke();
        }

        public static void Run(this IQueryExpr query)
        {
            query.Compile().Invoke();
        }

        public static IQueryExpr<IEnumerable<R>> Select<T, R>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, R>> selector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(selector);
            return new QueryExpr<IEnumerable<R>>(QueryExpr.NewTransform(f, query.Expr, typeof(R)));
        }

        public static IQueryExpr<IEnumerable<R>> Select<T, R>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, int, R>> selector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(selector);
            return new QueryExpr<IEnumerable<R>>(QueryExpr.NewTransformIndexed(f, query.Expr, typeof(R)));
        }

        public static IQueryExpr<IEnumerable<T>> Where<T>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, bool>> predicate)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(predicate);
            return new QueryExpr<IEnumerable<T>>(QueryExpr.NewFilter(f, query.Expr, typeof(T)));
        }
        
        public static IQueryExpr<IEnumerable<T>> Where<T>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, int, bool>> predicate)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(predicate);
            return new QueryExpr<IEnumerable<T>>(QueryExpr.NewFilterIndexed(f, query.Expr, typeof(T)));
        }

        public static IQueryExpr<Acc> Aggregate<T,Acc>(this IQueryExpr<IEnumerable<T>> query, Acc seed, Expression<Func<Acc, T, Acc>> func)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(func);
            return new QueryExpr<Acc>(QueryExpr.NewAggregate(Tuple.Create((object) seed, typeof(Acc)), f, query.Expr));
        }

        public static IQueryExpr<double> Sum(this IQueryExpr<IEnumerable<double>> query)
        {
            return new QueryExpr<double>(QueryExpr.NewSum(query.Expr, typeof(double)));
        }

        public static IQueryExpr<int> Sum(this IQueryExpr<IEnumerable<int>> query)
        {
            return new QueryExpr<int>(QueryExpr.NewSum(query.Expr, typeof(int)));
        }

        public static IQueryExpr<int> Count<T>(this IQueryExpr<IEnumerable<T>> query)
        {
            return new QueryExpr<int>(QueryExpr.NewCount(query.Expr, typeof(T)));
        }

        public static IQueryExpr<IEnumerable<R>> SelectMany<T,Col,R>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, IEnumerable<Col>>> collectionSelector, Expression<Func<T, Col, R>> resultSelector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(collectionSelector);
            var paramExpr = f.Parameters.Single();
            var bodyExpr = f.Body;
            var nested = Tuple.Create(paramExpr, CSharpExpressionOptimizer.ToQueryExpr(bodyExpr));
            var result = (LambdaExpression)CSharpExpressionOptimizer.Optimize(resultSelector);
            return new QueryExpr<IEnumerable<R>>(QueryExpr.NewNestedQueryTransform(nested, result, query.Expr, typeof(R)));
        }

        public static IQueryExpr<IEnumerable<R>> SelectMany<T,R>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, IEnumerable<R>>> selector )
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(selector);
            var paramExpr = f.Parameters.Single();
            var bodyExpr = f.Body;
            var nested = Tuple.Create(paramExpr, CSharpExpressionOptimizer.ToQueryExpr(bodyExpr));
            return new QueryExpr<IEnumerable<R>>(QueryExpr.NewNestedQuery(nested, query.Expr, typeof(R)));
        }

        public static IQueryExpr<IEnumerable<T>> Take<T>(this IQueryExpr<IEnumerable<T>> query, int n)
        {
            return new QueryExpr<IEnumerable<T>>(QueryExpr.NewTake(Expression.Constant(n), query.Expr, typeof(T)));
        }

        public static IQueryExpr<IEnumerable<T>> Skip<T>(this IQueryExpr<IEnumerable<T>> query, int n)
        {
            return new QueryExpr<IEnumerable<T>>(QueryExpr.NewSkip(Expression.Constant(n), query.Expr, typeof(T)));
        }

        public static IQueryExpr ForEach<T>(this IQueryExpr<IEnumerable<T>> query, Expression<Action<T>> action)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(action);
            return new QueryExprVoid(QueryExpr.NewForEach(f, query.Expr));
        }

        public static IQueryExpr<IEnumerable<IGrouping<Key, T>>> GroupBy<T, Key>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(keySelector);
            return new QueryExpr<IEnumerable<IGrouping<Key,T>>>(QueryExpr.NewGroupBy(f, query.Expr, typeof(IGrouping<Key,T>)));
        }

        public static IQueryExpr<IEnumerable<T>> OrderBy<T, Key>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(keySelector);
            return new QueryExpr<IEnumerable<T>>(QueryExpr.AddOrderBy(f, Order.Ascending, query.Expr, typeof(T)));
        }

        public static IQueryExpr<IEnumerable<T>> OrderByDescending<T, Key>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            var f = (LambdaExpression)CSharpExpressionOptimizer.Optimize(keySelector);

            return new QueryExpr<IEnumerable<T>>(QueryExpr.AddOrderBy(f, Order.Descending, query.Expr, typeof(T)));
        }

        public static IQueryExpr<IOrderedEnumerable<T>> ThenBy<T, Key>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            throw new NotImplementedException();
            //return new QueryExpr<IOrderedEnumerable<T>>(QueryExpr.AddOrderBy(keySelector, Order.Ascending, query.Expr, typeof(T)));
        }
        public static IQueryExpr<IOrderedEnumerable<T>> ThenByDescending<T, Key>(this IQueryExpr<IEnumerable<T>> query, Expression<Func<T, Key>> keySelector)
        {
            throw new NotImplementedException();
            //return new QueryExpr<IOrderedEnumerable<T>>(QueryExpr.AddOrderBy(keySelector, Order.Descending, query.Expr, typeof(T)));
        }

        public static IQueryExpr<List<T>> ToList<T>(this IQueryExpr<IEnumerable<T>> query)
        {
            return new QueryExpr<List<T>>(QueryExpr.NewToList(query.Expr));
        }

        public static IQueryExpr<T[]> ToArray<T>(this IQueryExpr<IEnumerable<T>> query)
        {
            return new QueryExpr<T[]>(QueryExpr.NewToArray(query.Expr));
        }
    }
}
