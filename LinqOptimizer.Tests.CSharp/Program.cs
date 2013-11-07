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



        public static void Main(string[] args)
        {
            Random random = new Random();
            var nums = Enumerable.Range(1, 100000000).Select(_ => random.Next(1, 100000000)).Select(x => x).ToArray();
            //var keys = nums.ToArray();

            Measure(() => nums.AsParallel().OrderBy(x => x).ToList());

            //Measure(() => Array.Sort(nums));
            //Measure(() => nums.AsQueryExpr().OrderBy(x => x).Run());
            //Measure(() => nums.AsParallelQueryExpr().OrderBy(x => x).Run());
            
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
