using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqOptimizer.CSharp;
using LinqOptimizer.Base;

namespace LinqOptimizer.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            (new GpuQueryTests()).Select();
            //var rnd = new Random();
            //var input = Enumerable.Range(1, 100000000).Select(x => (float)x).ToArray();
            
            //var f = input.AsQueryExpr().Select(x => x * 2.0).Compile();
            //Measure(() => f());
            ////Measure(() => input.Select(x => x * 2.0).ToArray());

            //Measure(() =>
            //{
            //    var s2 = 0;
            //    for (int i = 0; i < 10000; i++)
            //    {
            //        s2 += Enumerable.Range(0, i).AsQueryExpr().Count().Run();
            //    }
            //    Console.WriteLine(s2);
            //});
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
