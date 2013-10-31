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
            var nums = Enumerable.Range(1, 10000000).Select(_ => random.Next(1, 1000000)).ToArray();
            var keys = nums.ToArray();


            
            //Console.WriteLine("CSharpParallelSort");
            //Measure(() => CSharpParallelSort.QuicksortParallel(keys, nums));
            Console.WriteLine("FSharpParallelSort");
            Measure(() => ParallelSort.QuicksortParallel(keys, nums));

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
