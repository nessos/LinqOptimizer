using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.CSharp;
using FsCheck.Fluent;

namespace LinqOptimizer.Tests
{
    [TestFixture]
    class FsCheckParallelQueryExpr
    {
        [Test]
        public void Select()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsParallelQueryExpr().Select(n => n * 2).Sum().Run();
                var y = xs.AsParallel().Select(n => n * 2).Sum();
                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Where()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from n in xs.AsParallelQueryExpr()
                         where n % 2 == 0
                         select n)
                         .Sum()
                         .Run();
                var y = (from n in xs.AsParallel()
                         where n % 2 == 0
                         select n)
                         .Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Pipelined()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsParallelQueryExpr()
                        .Where(n => n % 2 == 0)
                        .Select(n => n * 2)
                        .Select(n => n * n)
                        .Sum()
                        .Run();

                var y = xs
                        .AsParallel()
                        .Where(n => n % 2 == 0)
                        .Select(n => n * 2)
                        .Select(n => n * n)
                        .Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Sum()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from n in xs.AsParallelQueryExpr()
                         select n * 2).Sum().Run();
                var y = (from n in xs.AsParallel()
                         select n * 2).Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectMany()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsParallelQueryExpr()
                        .SelectMany(n => xs.Select(_n => n * _n))
                        .Sum().Run();
                var y = xs.AsParallel()
                        .SelectMany(n => xs.Select(_n => n * _n))
                        .Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectManyNested()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsParallelQueryExpr()
                        .SelectMany(num => xs.SelectMany(_num => new[] { num * _num }))
                        .Sum().Run();
                var y = xs
                        .AsParallel()
                        .SelectMany(num => xs.SelectMany(_num => new[] { num * _num }))
                        .Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectManyPipeline()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsParallelQueryExpr()
                        .Where(num => num % 2 == 0)
                        .SelectMany(num => xs.Select(_num => num * _num))
                        .Select(i => i + 1)
                        .Sum()
                        .Run();

                var y = xs
                        .AsParallel()
                        .Where(num => num % 2 == 0)
                        .SelectMany(num => xs.Select(_num => num * _num))
                        .Select(i => i + 1)
                        .Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectManyCompehension()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from num in xs.AsParallelQueryExpr()
                         from _num in xs
                         select num * _num).Sum().Run();
                var y = (from num in xs.AsParallel()
                         from _num in xs
                         select num * _num).Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void GroupBy()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from num in xs.AsParallelQueryExpr()
                         group num by num into g
                         select g.Count()).Sum()
                        .Run();

                var y = (from num in xs.AsParallel()
                         group num by num into g
                         select g.Count()).Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void OrderBy()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from num in xs.AsParallelQueryExpr()
                         orderby num
                         select num)
                        .Run();

                var y = from num in xs.AsParallel()
                        orderby num
                        select num;

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }
    }
}
