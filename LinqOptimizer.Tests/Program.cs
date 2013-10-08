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
            //var input = Enumerable.Range(1, 250000000).ToArray();

            //var query = input.AsQueryExpr().Select(x => (double)x).Select(x => x + 1).Select(x => x + 1).Select(x => x + 1).Sum().Compile();
            //Measure(() => Console.WriteLine(query.Invoke()));

            //Measure(() => Console.WriteLine(input.Select(x => (double)x).Select(x => x + 1).Select(x => x + 1).Select(x => x + 1).Sum()));
            //Measure(() => Console.WriteLine(input.AsParallel().Select(x => (double)x).Select(x => x + 1).Select(x => x + 1).Select(x => x + 1).Sum()));
            //Measure(() => Console.WriteLine(input.AsParallel().Aggregate(() => 0.0, (acc, x) => ((((double)x + 1) + 1) + 1) + acc, (left, right) => left + right, x => x)));

            //var parallelQuery = input.AsParallel().AsQueryExpr().Select(x => (double)x).Select(x => x + 1).Select(x => x + 1).Select(x => x + 1).Sum().Compile();
            //Measure(() => Console.WriteLine(parallelQuery.Invoke()));

            var tests = new ParallelQueryExprTests();
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
