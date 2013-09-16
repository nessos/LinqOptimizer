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

            //var nums = Enumerable.Range(1, 100000).ToArray();
            //var _nums = Enumerable.Range(1, 10000).ToArray();

            //Func<double> f = nums.AsQueryExpr().SelectMany(num => _nums.Select(_num => num * _num)).Select(num => (double) num).Sum().Compile();
            //Measure(() => Console.WriteLine(f.Invoke()));

            //Measure(() => Console.WriteLine(nums.SelectMany(num => _nums.Select(_num => num * _num)).Select(num => (double)num).Sum()));

            LinqTests tests = new LinqTests();
            tests.EnumerableSourceTest();
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
