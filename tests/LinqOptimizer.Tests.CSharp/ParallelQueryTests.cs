using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nessos.LinqOptimizer.CSharp;
using FsCheck.Fluent;

namespace Nessos.LinqOptimizer.Tests
{
    [TestFixture]
    class ParallelQueryTests
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
        public void SumInt()
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
        public void SumDouble()
        {
            Spec.ForAny<double[]>(xs =>
            {
                var x = (from n in xs.AsParallelQueryExpr()
                         select n * 2).Sum().Run();
                var y = (from n in xs.AsParallel()
                         select n * 2).Sum();

                return (Double.IsNaN(x) && Double.IsNaN(y)) || Math.Ceiling(x) == Math.Ceiling(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SumLong()
        {
            Spec.ForAny<long[]>(xs =>
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
            Spec.ForAny<string[]>(xs =>
            {
                var x = (from num in xs.AsParallelQueryExpr()
                         from _num in xs
                         select num + " " + _num).Run();
                var y = (from num in xs.AsParallel().AsOrdered()
                         from _num in xs
                         select num + " " + _num);

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void GroupBy()
        {
            Spec.ForAny<int[]>(xs =>
            {
#if MONO_BUILD
                var x = xs.AsParallelQueryExpr().GroupBy(num => num.ToString()).Select(g => g.Count()).Sum().Run();
#else
                var x = (from num in xs.AsParallelQueryExpr()
                         group num by num.ToString() into g
                         select g.Count()).Sum()
                        .Run();
#endif

                var y = (from num in xs.AsParallel()
                         group num by num.ToString() into g
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
                         select num * 2)
                        .Run();

                var y = from num in xs.AsParallel()
                        orderby num
                        select num * 2;

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void OrderByDescending()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from num in xs.AsParallelQueryExpr()
                         orderby num descending 
                         select num * 2)
                        .Run();

                var y = from num in xs.AsParallel()
                        orderby num descending
                        select num * 2;

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }


        [Test]
        public void Count()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsParallelQueryExpr()
                        .Select(i => i)
                        .Count()
                        .Run();

                var y = xs.AsParallel()
                        .Select(i => i)
                        .Count();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ToList()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsParallelQueryExpr()
                        .ToList()
                        .Run();

                var y = xs.AsParallel()
                        .ToList();

                return x.OrderBy(i => i).SequenceEqual(y.OrderBy(i => i));
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ToArray()
        {
            Spec.ForAny<List<int>>(xs =>
            {
                var x = xs.AsParallelQueryExpr()
                        .ToArray()
                        .Run();

                var y = xs.AsParallel()
                        .ToArray();

                return x.OrderBy(i => i).SequenceEqual(y.OrderBy(i => i));
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void LinqLetTest()
        {
            Spec.ForAny<List<int>>(nums =>
            {
                var x =
                    (from num in nums.AsParallelQueryExpr()
                     let r1 = num * 2
                     let r2 = r1 * 2
                     select r2 * num * 2).Sum().Run();

                var y =
                    (from num in nums.AsParallel()
                     let r1 = num * 2
                     let r2 = r1 * 2
                     select r2 * num * 2).Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ThenBy()
        {
            Spec.ForAny<DateTime[]>(ds =>
            {
                var x = ds.AsParallelQueryExpr()
                         .OrderBy(d => d.Year)
                         .ThenBy(d => d.Month)
                         .ThenBy(d => d.Day)
                         .ThenBy(d => d)
                         .Run();

                var y = ds.AsParallel().AsOrdered()
                          .OrderBy(d => d.Year)
                          .ThenBy(d => d.Month)
                          .ThenBy(d => d.Day)
                          .ThenBy(d => d);

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ThenByDescending()
        {
            Spec.ForAny<DateTime[]>(ds =>
            {
                var x = (ds.AsParallelQueryExpr()
                         .OrderByDescending(d => d.Year)
                         .ThenBy(d => d.Month)
                         .ThenByDescending(d => d.Day)
                         .Select(d => d.Year + ":" + d.Month + ":" + d.Day)).Run();

                var y = ds.AsParallel()
                          .OrderByDescending(d => d.Year)
                          .ThenBy(d => d.Month)
                          .ThenByDescending(d => d.Day)
                          .Select(d => d.Year + ":" + d.Month + ":" + d.Day);

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void PreCompileFunc()
        {
            Spec.ForAny<int>(i =>
            {
                if (i < 1) return true;

                var t = ParallelExtensions.Compile<int, int>(m =>
                            Enumerable.Range(1, m).AsParallelQueryExpr().Sum());

                var x = t(i);

                var y = Enumerable.Range(1, i).AsParallel().Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

    }
}
