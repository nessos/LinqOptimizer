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
    class Program
    {
        struct Pair<T1, T2> : IComparable<Pair<T1, T2>> where T1 : IComparable<T1> 
                                                        where T2 : IComparable<T2>
        {
            private readonly T1 first;
            private readonly T2 second;

            public Pair(T1 first, T2 second)
            {
                this.first = first;
                this.second = second;
            }

            public T1 First { get { return this.first; } }
            public T2 Second { get { return this.second; } }

            #region IComparable<Pair<T1,T2>> Members

            public int CompareTo(Pair<T1, T2> other)
            {
                var first = this.first.CompareTo(other.first);
                if (first == 0)
                    return this.second.CompareTo(other.second);
                else
                    return first;
            }

            #endregion
        }

        public static void Main(string[] args)
        {
            Random random = new Random();
            var nums = Enumerable.Range(1, 100).Select(_ => random.Next(1, 100)).Select(x => new DateTime(x)).ToArray();
            var keys = nums.ToArray();

            //Measure(() => nums.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList());
            //Measure(() => nums.AsQueryExpr().OrderBy(x => new Pair<int, int>(x.Year, x.Month)).Run());
            //Measure(() => nums.AsQueryExpr().OrderBy(x => x.Year).Run());
            
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
