using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Tests
{
    class Program
    {
        
        public static void Main(string[] args)
        {
            var nums = Enumerable.Range(1, 100000000).Select(x => (double)x).ToArray();
            Func<double> f = nums.AsQueryExpr().Select(num => num * 2).Sum().Compile();
            Measure(() => Console.WriteLine(f.Invoke()));

            Measure(() => Console.WriteLine(nums.Select(num => num * 2).Sum()));
            
            //LinqTests tests = new LinqTests();
            //tests.PipelineTest();
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
