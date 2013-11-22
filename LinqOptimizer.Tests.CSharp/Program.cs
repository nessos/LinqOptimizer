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
    public class Program
    {



        public static void Main(string[] args)
        {
            //Random random = new Random();
            //var nums = Enumerable.Range(1, 1000).Select(_ => random.Next(1, 1000)).Select(x => x).ToArray();

            var nums = Enumerable.Range(1, 10);
            var x = (from num in nums.AsQueryExpr()
                     let r1 = num * 2
                     let r2 = r1 * 2
                     let r3 = 42
                     select r2 * num * 2 * r3).Run();

            var y = nums.AsQueryExpr()
                        .Where(n => n % 2 == 0)
                        .Select(n => n * 2)
                        .Select(n => n.ToString())
                        .Select(n => n + "!")
                        .Run();

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
