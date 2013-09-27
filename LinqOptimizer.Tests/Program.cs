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
            Random r = new Random();
            var nums = Enumerable.Range(1, 100000000).Select(num => r.Next(1000)).ToArray();
            

            Func<int> f = nums.AsQueryExpr().GroupBy(num => num).Select(g => g.Count()).Sum().Compile();
            Measure(() => Console.WriteLine(f.Invoke()));

            Measure(() => Console.WriteLine(nums.AsParallel().GroupBy(num => num)
                                                .Select(g => g.Count())
                                                .Sum()));

            //var tests = new QueryExprTests();
            //tests.OrderByTest();
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
