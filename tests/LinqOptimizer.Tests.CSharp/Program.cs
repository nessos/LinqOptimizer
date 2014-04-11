using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nessos.LinqOptimizer.CSharp;
using Nessos.LinqOptimizer.Base;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.Numerics;

namespace Nessos.LinqOptimizer.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var t = Tuple.Create(10, 10);
            var func = Extensions.Compile<Tuple<int,int>, IEnumerable<int>>(
                        tup =>
                            from x in QueryExpr.Range(1, tup.Item1)
                            from y in Enumerable.Range(1, tup.Item2)
                            select x * y
                        );

            var xs = func(t).ToArray();

            //var input = Enumerable.Range(1, 20000000).Select(x => (double)x).ToArray();

            //var query = input.AsQueryExpr()./*Where(x => x % 2 == 0).Select(x => x * x)*/Sum().Compile();
            //var parQuery = input.AsParallelQueryExpr()./*Where(x => x % 2 == 0).Select(x => x * x)*/Sum().Compile();

            ////var partitioner = Partitioner.Create(0, 100);
            ////var tuples = partitioner.GetDynamicPartitions().ToArray();

            //Measure(() =>
            //{
            //    double sum = 0;
            //    for (int i = 0; i < input.Length; i++)
            //    {
            //        var x = input[i];
            //        //if (x % 2 == 0)
            //        sum += x * x;
            //    }
            //});

            //Measure(() => query());
            //Measure(() => parQuery());
        }

        static void Measure(Action action)
        {
            Measure(action, 1);
        }

        static void Measure(Action action, int iterations)
        {
            var watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                action();
            }
            Console.WriteLine(new TimeSpan(watch.Elapsed.Ticks / iterations));
        }
    }
}
