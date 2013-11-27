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
            var nums = Enumerable.Range(1, 1000).Select(_ => random.Next(1, 1000)).Select(x => x).ToArray();
            //var pairs = nums.Select(x => new KeyValue<int, int>(x, x)).ToArray();

            //var g = nums.GroupBy(x => x).First();

            //var test = g.GetEnumerator();
            //var _test = g.GetEnumerator();
            //var b = test.GetType().IsClass;

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
