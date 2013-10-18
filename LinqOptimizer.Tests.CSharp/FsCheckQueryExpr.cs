using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FsCheck.Fluent;
using NUnit.Framework;
using LinqOptimizer.CSharp;

namespace LinqOptimizer.Tests
{
    [TestFixture]
    public class QuickCheckQueryExpr
    {
        [Test]
        public void Select()
        {
            Spec.ForAny<int[]>(xs => {
                var x = xs.AsQueryExpr().Select(n => n * 2).Run();
                var y = xs.Select(n => n * 2);
                return Enumerable.SequenceEqual(x, y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectIndexed()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsQueryExpr().Select((n, index) => index).Run();
                var y = xs.Select((n, index) => index);
                return Enumerable.SequenceEqual(x, y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Where()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from n in xs.AsQueryExpr()
                         where n % 2 == 0
                         select n).Run();
                var y = from n in xs
                        where n % 2 == 0
                        select n;

                return Enumerable.SequenceEqual(x, y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void WhereIndexed()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsQueryExpr().Where((n, index) => index % 2 == 0).Run();
                var y = xs.Where((n, index) => index % 2 == 0);

                return Enumerable.SequenceEqual(x, y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Pipelined()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsQueryExpr()
                        .Where(n => n % 2 == 0)
                        .Select(n => n * 2)
                        .Select(n => n.ToString())
                        .Select(n => n + "!")
                        .Run();

                var y = xs
                        .Where(n => n % 2 == 0)
                        .Select(n => n * 2)
                        .Select(n => n.ToString())
                        .Select(n => n + "!");

                return Enumerable.SequenceEqual(x, y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SumInt()
        {
            Spec.ForAny<int []>(xs =>
            {
                var x = (from n in xs.AsQueryExpr()
                         select n * 2).Sum().Run();
                var y = (from n in xs
                         select n * 2).Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SumDouble()
        {
            Spec.ForAny<double[]>(xs =>
            {
                var x = (from n in xs.AsQueryExpr()
                         select n * 2).Sum().Run();
                var y = (from n in xs
                         select n * 2).Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Aggregate()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from n in xs.AsQueryExpr()
                          select n * 2).Aggregate(0, (acc, value) => acc + value).Run();
                var y = (from n in xs
                          select n * 2).Aggregate(0, (acc, value) => acc + value);

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectMany()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsQueryExpr()
                        .SelectMany(n => xs.Select(_n => n * _n))
                        .Sum().Run();
                var y = xs.SelectMany(n => xs.Select(_n => n * _n))
                        .Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectManyCompehension()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from num in xs.AsQueryExpr()
                          from _num in xs
                          select num * _num).Sum().Run();
                var y = (from num in xs
                         from _num in xs
                         select num * _num).Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectManyNested()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsQueryExpr()
                        .SelectMany(num => xs.SelectMany(_num => new[] { num * _num }))
                        .Sum().Run();
                var y = xs
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
                var x = xs.AsQueryExpr()
                        .Where(num => num % 2 == 0)
                        .SelectMany(num => xs.Select(_num => num * _num))
                        .Select(i => i + 1)
                        .Sum()
                        .Run();

                var y = xs
                        .Where(num => num % 2 == 0)
                        .SelectMany(num => xs.Select(_num => num * _num))
                        .Select(i => i + 1)
                        .Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void EnumerableSource()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var ys = xs.Select(i => i);

                var x = (from n in ys.AsQueryExpr()
                        select n * 2)
                        .Run();

                var y = (from n in ys
                         select n * 2);

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ListSource()
        {
            Spec.ForAny<List<int>>(xs =>
            {
                var x = (from n in xs.AsQueryExpr()
                         select n * 2)
                        .Run();

                var y = (from n in xs
                         select n * 2);

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void TakeN()
        {
            Spec.ForAny<int[], int>((xs, n) =>
            {
                var x = xs.AsQueryExpr()
                        .Take(n)
                        .Run();

                var y = xs
                        .Take(n);

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SkipN()
        {
            Spec.ForAny<int[], int>((xs, n) =>
            {
                var x = xs.AsQueryExpr()
                        .Skip(n)
                        .Run();

                var y = xs
                        .Skip(n);

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void TakeSkipN()
        {
            Spec.ForAny<int[],int>((xs, n) =>
            {
                var x = xs.AsQueryExpr()
                        .Take(n)
                        .Skip(n)
                        .Run();

                var y = xs
                        .Take(n)
                        .Skip(n);

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void NestedTakeAndSkipN()
        {
            Spec.ForAny<int[], int>((xs, n) =>
            {
                var x = xs.AsQueryExpr()
                        .Take(n)
                        .SelectMany(_ => xs.Skip(n).Take(n))
                        .Run();

                var y = xs
                        .Take(n)
                        .SelectMany(_ => xs.Skip(n).Take(n));

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ForEach()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = new List<int>();
                xs.AsQueryExpr().ForEach(num => x.Add(num)).Run();

                var y = new List<int>();
                xs.ToList().ForEach(num => y.Add(num));

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void GroupBy()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from num in xs.AsQueryExpr()
                         group num by num into g
                         select g.Count()).Sum()
                        .Run();

                var y = (from num in xs
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
                var x = (from num in xs.AsQueryExpr()
                         orderby num
                         select num)
                        .Run();

                var y = from num in xs
                        orderby num
                        select num;

                return x == y;
            }).QuickCheckThrowOnFailure();

            Spec.ForAny<int[]>(xs =>
            {
                var x = (from num in xs.AsQueryExpr()
                         orderby num descending
                         select num)
                        .Run();

                var y = from num in xs
                        orderby num descending
                        select num;

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Count()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsQueryExpr()
                        .Select(i => i)
                        .Count()
                        .Run();

                var y = xs
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
                var x = xs.AsQueryExpr()
                        .ToList()
                        .Run();

                var y = xs
                        .ToList();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ToArray()
        {
            Spec.ForAny<List<int>>(xs =>
            {
                var x = xs.AsQueryExpr()
                        .ToArray()
                        .Run();

                var y = xs
                        .ToArray();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void RangeTest()
        {
            var result = Enumerable.Range(1, 10).AsQueryExpr();

            Assert.AreEqual(Enumerable.Range(1, 10), result.Run());
        }

        [Test]
        public void RepeatTest()
        {
            var result = Enumerable.Repeat(42, 10).AsQueryExpr();

            Assert.AreEqual(Enumerable.Repeat(42, 10), result.Run());
        }

        [Test]
        public void RepeatValueTypeCastTest()
        {
            var t = (object)DateTime.Now;
            var result = Enumerable.Repeat(t, 10).AsQueryExpr();

            Assert.AreEqual(Enumerable.Repeat(t, 10), result.Run());
        }

        [Test]
        public void RangeWhereTest()
        {
            var result = Enumerable.Range(1, 10).AsQueryExpr().Where(f => f < 5);

            Assert.AreEqual(Enumerable.Range(1, 10).Where(f => f < 5), result.Run());
        }

        [Test]
        public void RepeatWhereTest()
        {
            var result = Enumerable.Repeat(42, 10).AsQueryExpr().Where(f => f < 5);

            Assert.AreEqual(Enumerable.Repeat(42, 10).Where(f => f < 5), result.Run());
        }

        [Test]
        public void RangeInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => Enumerable.Range(0, -1).AsQueryExpr().Run());
        }

        [Test]
        public void RepeatInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => Enumerable.Repeat(0, -1).AsQueryExpr().Run());
        }

        [Test]
        public void NestedRangeInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => Enumerable.Range(1, 10).AsQueryExpr().SelectMany(_ => Enumerable.Range(0, -1)).Run());
        }

        [Test]
        public void NestedRepeatInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => Enumerable.Range(1, 10).AsQueryExpr().SelectMany(_ => Enumerable.Repeat(0, -1)).Run());
        }

    }
}
