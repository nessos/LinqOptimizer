using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqOptimizer.Core;
using LinqOptimizer.CSharp;


namespace LinqOptimizer.Tests
{
    public class Program
    {

        struct KeyValue<T1, T2> : IComparable<KeyValue<T1, T2>> where T1 : IComparable<T1>
                                                                where T2 : IComparable<T2>
        {
            private readonly T1 t1;
            private readonly T2 t2;
            public KeyValue(T1 t1, T2 t2)
            {
                this.t1 = t1;
                this.t2 = t2;
            }

            public T1 Ti1 { get { return this.t1; } }
            public T2 Ti2 { get { return this.t2; } }

            #region IComparable<KeyValue<T1,T2>> Members

            public int CompareTo(KeyValue<T1, T2> other)
            {
                if (this.t1.CompareTo(other.Ti1) == 0)
                    return this.t2.CompareTo(other.Ti2);
                else
                    return this.t2.CompareTo(other.Ti2);
            }

            #endregion
        }

        public static void Main(string[] args)
        {

            var tests = new QueryTests();
            tests.NestedSelectTest();

            var size = 10;
            var rnd = new Random(size);
            var foo= Enumerable.Range(1, size).AsQueryExpr()
                   .Select(x => 100 * rnd.NextDouble() - 50)
                   .GroupBy(x => (int)x % 10)
                   .Select(x => x.Count())
                   .ToArray()
                   .Run();

            Random random = new Random();
            //var nums = Enumerable.Range(1, 100000000).Select(_ => random.Next(1, 1000)).Select(x => x).ToArray();
            //var pairs = nums.Select(x => new KeyValue<int, int>(x, x)).ToArray();

            var ds = new[] { DateTime.Parse("20/4/1989 8:00:00 μμ"), DateTime.Parse("2/6/2024 11:00:00 πμ"),
            DateTime.Parse("28/5/2094 1:00:00 πμ"),DateTime.Parse("19/10/2091 10:00:00 μμ"),DateTime.Parse("10/1/1981 11:00:00 πμ"),
            DateTime.Parse("26/2/1953 7:00:00 μμ"),DateTime.Parse("25/5/2022 12:00:00 πμ"),DateTime.Parse(" 14/9/2087 2:00:00 μμ"),
            DateTime.Parse("24/12/1952 2:00:00 μμ"),DateTime.Parse("8/12/2007 12:00:00 πμ"),DateTime.Parse("20/2/1932 5:00:00 πμ"),
            DateTime.Parse("20/10/1988 6:00:00 πμ"),DateTime.Parse("19/4/1999 5:00:00 μμ"),DateTime.Parse("31/5/1954 2:00:00 μμ"),
            DateTime.Parse("5/10/2072 10:00:00 πμ"),DateTime.Parse("16/1/1981 10:00:00 μμ"),DateTime.Parse("29/8/1982 6:00:00 πμ")};
            var test = ds.AsParallelQueryExpr().OrderBy(d1 => d1.Year).ThenBy(d2 => d2.Month).ThenBy(d2 => d2.Day).Select(d3 => d3.ToString()).Run();
            var _test = ds.OrderBy(d => d.Year).ThenBy(d => d.Month).ThenBy(d2 => d2.Day).Select(d => d.ToString()).ToList();

            var b = test.SequenceEqual(_test);

            //Measure(() => nums.OrderBy(x => x).ThenBy(x => x).ToList());
            //Measure(() => nums.AsQueryExpr().OrderBy(x => x).ThenBy(x => x).ToList().Run());
            //Measure(() => nums.OrderBy(x => Tuple.Create(x, x)).ToList());
            //Measure(() => Array.Sort(pairs, nums));
            //Measure(() => Array.Sort(nums, nums));
        }

        static void Measure(Action action)
        {
            var watch = new Stopwatch();
            watch.Start();
            action();
            Console.WriteLine(watch.Elapsed);
        }
    }
}
