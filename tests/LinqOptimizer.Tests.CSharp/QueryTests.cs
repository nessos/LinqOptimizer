using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FsCheck.Fluent;
using NUnit.Framework;
using System.Linq.Expressions;
using Nessos.LinqOptimizer.CSharp;
using Nessos.LinqOptimizer.Base;

namespace Nessos.LinqOptimizer.Tests
{
    [TestFixture]
    public class QueryTests
    {

		bool RunsOnMono = Type.GetType("Mono.Runtime", false) != null;

        [Test]
        public void Select()
        {
            Func<IEnumerable<object>, bool> f = xs => {
                var x = xs.AsQueryExpr().Select(n => n.ToString()).Run();
                var y = xs.Select(n => n.ToString());
                return Enumerable.SequenceEqual(x, y);
            };

            Spec.ForAny<TestInput<object>>(xs =>
                    TestInput<object>.RunTestFunc<object>(f, xs))
                .QuickCheckThrowOnFailure();
        }

        [Test]
        public void TakeWhile()
        {
            Func<IEnumerable<int>, bool> f = xs =>
            {
                var x = xs.AsQueryExpr().TakeWhile(i => i < 10).Run();
                var y = xs.TakeWhile(i => i < 10);
                return Enumerable.SequenceEqual(x, y);
            };

            Spec.ForAny<TestInput<int>>(xs =>
                    TestInput<int>.RunTestFunc<int>(f, xs))
                .QuickCheckThrowOnFailure();
        }

        [Test]
        public void SkipWhile()
        {
            Func<IEnumerable<int>, bool> f = xs =>
            {
                var x = xs.AsQueryExpr().SkipWhile(i => i < 10).Run();
                var y = xs.SkipWhile(i => i < 10);
                return Enumerable.SequenceEqual(x, y);
            };

            Spec.ForAny<TestInput<int>>(xs =>
                    TestInput<int>.RunTestFunc<int>(f, xs))
                .QuickCheckThrowOnFailure();
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

                return (Double.IsNaN(x) && Double.IsNaN(y)) || x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SumLong()
        {
            Spec.ForAny<long[]>(xs =>
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
                        .SelectMany(n => xs.Where(_n => _n % 2 == 0).Select(_n => n * _n))
                        .Sum().Run();
                var y = xs.SelectMany(n => xs.Where(_n => _n % 2 == 0).Select(_n => n * _n))
                        .Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectManyCompehension()
        {
            Spec.ForAny<string[]>(xs =>
            {
                var x = (from num in xs.AsQueryExpr()
                          from _num in xs
                          select num + " " + _num).Run();
                var y = (from num in xs
                         from _num in xs
                         select num + " " + _num);

                return x.SequenceEqual(y);
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
        public void SelectManyComprehensionNestedTest()
        {
            Spec.ForAny<int>(max =>
            {
                if (max < 0) return true;

                var x = (from a in QueryExpr.Range(1, max)
                         from b in Enumerable.Range(1, max)
                         from c in Enumerable.Range(1, max)
                         where a + b == c
                         select Tuple.Create(a, b, c)).ToArray().Run();

                var y = (from a in Enumerable.Range(1, max)
                         from b in Enumerable.Range(1, max)
                         from c in Enumerable.Range(1, max)
                         where a + b == c
                         select Tuple.Create(a, b, c)).ToArray();

                return Enumerable.SequenceEqual(x, y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void SelectManyComprehensionNestedTestTypeEraser()
        {
            Spec.ForAny<int>(max =>
            {
                if (max < 0) return true;

                var x = (from a in QueryExpr.Range(1, max + 1)
                         from b in Enumerable.Range(a, max + 1 - a)
                         where a * a + b * b == b
                         select Tuple.Create(a, b)).ToArray().Run();

                var y = (from a in Enumerable.Range(1, max + 1)
                         from b in Enumerable.Range(a, max + 1 - a)
                         where a * a + b * b == b
                         select Tuple.Create(a, b)).ToArray();

                return Enumerable.SequenceEqual(x, y);
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

                return x.SequenceEqual(y);
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

                return x.SequenceEqual(y);
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

                return x.SequenceEqual(y);
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

                return x.SequenceEqual(y);
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

                return x.SequenceEqual(y);
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

                return x.SequenceEqual(y);
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

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void GroupBy()
        {
            Spec.ForAny<int[]>(xs =>
            {
#if MONO_BUILD
                var x = xs.AsQueryExpr().GroupBy(num => num.ToString()).Select(g => g.Sum()).Run();
#else
                var x = (from num in xs.AsQueryExpr()
                         group num by num.ToString() into g
                         select g.Sum())
                        .Run();
#endif

                var y = (from num in xs
                         group num by num.ToString() into g
                         select g.Sum());

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void OrderBy()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = (from num in xs.AsQueryExpr()
                         orderby num
                         select num * 2)
                        .Run();

                var y = from num in xs
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
                var x = (from num in xs.AsQueryExpr()
                         orderby num descending
                         select num * 2)
                        .Run();

                var y = from num in xs
                        orderby num descending
                        select num * 2;

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ThenBy()
        {
            Spec.ForAny<DateTime[]>(ds =>
            {
                var x = ds.AsQueryExpr()
                         .OrderBy(d => d.Year)
                         .ThenBy(d => d.Month)
                         .ThenBy(d => d.Day)
                         .ThenBy(d => d)
                         .Run();
                         
                var y = ds.OrderBy(d => d.Year)
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
                var x = (ds.AsQueryExpr()
                         .OrderByDescending(d => d.Year)
                         .ThenBy(d => d.Month)
                         .ThenByDescending(d => d.Day)
                         .Select(d => d.Year + ":" + d.Month + ":" + d.Day)).Run();

                var y = ds.OrderByDescending(d => d.Year)
                          .ThenBy(d => d.Month)
                          .ThenByDescending(d => d.Day)
                          .Select(d => d.Year + ":" + d.Month + ":" + d.Day);

                return x.SequenceEqual(y);
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

                return x.SequenceEqual(y);
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

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void NestedSelectTest()
        {
            Spec.ForAny<List<int>>(xs =>
            {
                var x = xs.AsQueryExpr()
                        .Select(m => Enumerable.Repeat(m, 10).Select(i => i * i).Sum())
                        .Run();

                var y = xs
                        .Select(m => Enumerable.Repeat(m, 10).Select(i => i * i).Sum());

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void NestedSelectManyTest()
        {
            Spec.ForAny<List<int>>(xs =>
            {
                var x = xs.AsQueryExpr()
                        .SelectMany(m => Enumerable.Repeat(m, Enumerable.Range(1,10).Sum()).Select(i => i * i))
                        .Run();

                var y = xs
                        .SelectMany(m => Enumerable.Repeat(m, Enumerable.Range(1, 10).Sum()).Select(i => i * i));

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }


        [Test]
        public void LinqLetTest()
        {
            Spec.ForAny<List<int>>(nums =>
            {
                var x = 
                    (from num in nums.AsQueryExpr()
                    let a = num * 2
                    let c = a + 1
                    let b = a * 2
                    let e = b - 5
                    let d = c * c
                    let m = 3
                    select a + b + c + d + e + m + num).Sum().Run();

                var y = 
                    (from num in nums
                     let a = num * 2
                     let c = a + 1
                     let b = a * 2
                     let e = b - 5
                     let d = c * c
                     let m = 3
                     select a + b + c + d + e + m + num).Sum();

                return x == y;
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void RangeTest()
        {
            var result = QueryExpr.Range(1, 10);

            Assert.AreEqual(Enumerable.Range(1, 10), result.Run());
        }

        [Test]
        public void RepeatTest()
        {
            var result = QueryExpr.Repeat(42, 10);

            Assert.AreEqual(Enumerable.Repeat(42, 10), result.Run());
        }

        [Test]
        public void RepeatValueTypeCastTest()
        {
            var t = (object)DateTime.Now;
            var result = QueryExpr.Repeat(t, 10);

            Assert.AreEqual(Enumerable.Repeat(t, 10), result.Run());
        }

        [Test]
        public void RangeWhereTest()
        {
            var result = QueryExpr.Range(1, 10).Where(f => f < 5);

            Assert.AreEqual(Enumerable.Range(1, 10).Where(f => f < 5), result.Run());
        }

        [Test]
        public void RepeatWhereTest()
        {
            var result = QueryExpr.Repeat(42, 10).Where(f => f < 5);

            Assert.AreEqual(Enumerable.Repeat(42, 10).Where(f => f < 5), result.Run());
        }

        [Test]
        public void RangeInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => QueryExpr.Range(0, -1).Run());
        }

        [Test]
        public void RepeatInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => QueryExpr.Repeat(0, -1).Run());
        }

        [Test]
        public void NestedRangeInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => QueryExpr.Range(1, 10).SelectMany(_ => Enumerable.Range(0, -1)).Run());
        }

        [Test]
        public void NestedRepeatInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => QueryExpr.Range(1, 10).SelectMany(_ => Enumerable.Repeat(0, -1)).Run());
        }

        private int UserDefinedAnonymousTypeTest(bool enable)
        {
            var anon = new { A = 1, B = "42" };
            try
            {
                var t = QueryExpr.Range(1, 42).Select(_ => anon.A).Sum().Run(enable);
                return t;
            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
                
        }

        [Test]
        public void UserDefinedAnonymousType()
        {
			if(RunsOnMono)
				Assert.AreEqual(42, UserDefinedAnonymousTypeTest(false));
			else
				Assert.Catch(typeof(MemberAccessException), () => UserDefinedAnonymousTypeTest(false));


			Assert.AreEqual(42, UserDefinedAnonymousTypeTest(true));
        } 


        [Test]
        public void PreCompileFunc()
        {
            Spec.ForAny<int>(i =>
            {
                if (i < 1) return true;

                var t = Extensions.Compile<int, IEnumerable<string>>(m =>
                        QueryExpr.Range(1, m).Select(n => n.ToString()));

                var x = t(i);

                var y = Enumerable.Range(1, i).Select(n => n.ToString());

                return Enumerable.SequenceEqual(x,y);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void PreCompileAction()
        {
            Spec.ForAny<int>(i =>
            {
                if (i < 1) return true;

                var xs = new List<int>();
                Expression<Func<int, IQueryExpr>> lam = m => QueryExpr.Range(1, m).ForEach(n => xs.Add(n));
                Action<int> t = Extensions.Compile<int>(lam);

                t(i);

                var ys = new List<int>();
                Enumerable.Range(1, i).ToList().ForEach(n => ys.Add(n));

                return Enumerable.SequenceEqual(xs, ys);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Zip()
        {
            Spec.ForAny<List<int>>(ms =>
            {
                var xs = QueryExpr.Zip(ms, ms, (a, b) => a * b).Run();
                var ys = Enumerable.Zip(ms, ms, (a, b) => a * b);

                return Enumerable.SequenceEqual(xs, ys);
            }).QuickCheckThrowOnFailure();
        }
    }

    [TestFixture]
    public class TupleTests
    {
        [Test]
        public void Detuple1()
        {
            Spec.ForAny<List<int>>(ms =>
            {
                var xs = ms.AsQueryExpr()
                         .Select(i => new Tuple<int, int>(i, i + 1))
                         .Select(t => new Tuple<int, int, int>(t.Item2, t.Item2, t.Item2 + 1))
                         .Select(m => string.Format("{0}{1}", m.Item1, m.Item3))
                         .Run();

                var ys = ms
                         .Select(i => new Tuple<int, int>(i, i + 1))
                         .Select(t => new Tuple<int, int, int>(t.Item2, t.Item2, t.Item2 + 1))
                         .Select(m => string.Format("{0}{1}", m.Item1, m.Item3));

                return Enumerable.SequenceEqual(xs, ys);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Detuple2()
        {
            Spec.ForAny<List<int>>(ms =>
            {
                var xs = ms.AsQueryExpr()
                         .Select(i => new Tuple<int, int>(i, i + 1))
                         .Take(ms.Count() / 2)
                         .Select(m => m.Item2)
                         .Run();

                var ys = ms
                         .Select(i => new Tuple<int, int>(i, i + 1))
                         .Take(ms.Count() / 2)
                         .Select(m => m.Item2);

                return Enumerable.SequenceEqual(xs, ys);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Detuple3()
        {
            Spec.ForAny<List<int>>(ms =>
            {
                var xs = ms.AsQueryExpr()
                         .Select(i => new Tuple<int, int>(i, i + 1))
                         .Select(m => m.ToString())
                         .Run();

                var ys = ms
                         .Select(i => new Tuple<int, int>(i, i + 1))
                         .Select(m => m.ToString());

                return Enumerable.SequenceEqual(xs, ys);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Detuple4()
        {
            Spec.ForAny<List<int>>(ms =>
            {
                var xs = ms.AsQueryExpr()
                         .Select(i => new Tuple<int, int>(i, i * 42))
                         .Where(m => m.Item1 % 2 == 0)
                         .Run();

                var ys = ms
                         .Select(i => new Tuple<int, int>(i, i * 42))
                         .Where(m => m.Item1 % 2 == 0);

                return Enumerable.SequenceEqual(xs, ys);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Detuple5()
        {
            Spec.ForAny<List<int>>(ms =>
            {
                var xs = ms.AsQueryExpr()
                         .Aggregate(Tuple.Create(0, 1), (t, _) => Tuple.Create(t.Item2, t.Item1 + t.Item2))
                         .Run();

                var ys = ms
                         .Aggregate(Tuple.Create(0, 1), (t, _) => Tuple.Create(t.Item2, t.Item1 + t.Item2));

                return Tuple.Equals(xs, ys);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Detuple6()
        {
            Spec.ForAny<List<int>>(ms =>
            {
                var xs = ms.AsQueryExpr()
                         .Select(x1 => Tuple.Create(x1 * x1, Tuple.Create(x1, -x1)))
                         .Where(t1 => t1.Item2.Item1 == -t1.Item2.Item2)
                         .Run();

                var ys = ms
                         .Select(x2 => Tuple.Create(x2 * x2, Tuple.Create(x2, -x2)))
                         .Where(t2 => t2.Item2.Item1 == -t2.Item2.Item2);

                return Enumerable.SequenceEqual(xs, ys);
            }).QuickCheckThrowOnFailure();
        }

        [Test]
        public void Detuple7()
        {
            Spec.ForAny<List<int>>(ms =>
            {
                var xs = ms.AsQueryExpr()
                         .Select(x1 => Tuple.Create(x1 * x1, Tuple.Create(x1, -x1)))
                         .Where(t1 => t1.Item2.Item1 == -t1.Item2.Item2)
                         .Count()
                         .Run();

                var ys = ms
                         .Select(x2 => Tuple.Create(x2 * x2, Tuple.Create(x2, -x2)))
                         .Where(t2 => t2.Item2.Item1 == -t2.Item2.Item2)
                         .Count();

                return xs == ys;
            }).QuickCheckThrowOnFailure();
        }
    }
}
