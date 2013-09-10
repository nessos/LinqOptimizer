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

            //var nums = Enumerable.Range(1, 100000000).Select(x => (double)x).ToArray();

            //Func<double> f = nums.AsQueryExpr().SelectMany(_num => nums.Select(num => num * _num)).Sum().Compile();

            //Measure(() => Console.WriteLine(f.Invoke()));

            //Measure(() => Console.WriteLine(nums.SelectMany(_num => nums.Select(num => num * _num)).Sum()));


            LinqTests tests = new LinqTests();
            tests.SelectManyComprehensionTest();
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
