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
using LinqOptimizer.Base;

namespace LinqOptimizer.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var e1 = Extensions.CompileTemplate<int, List<int>>(
            //        t => Enumerable.Range(1, 10).AsQueryExpr().Select(x => x * t).ToList());
            
            //e1(10).ForEach(Console.WriteLine);

            //var e2 = Extensions.CompileTemplate<IEnumerable<int>, int>(ls => ls.AsQueryExpr().Count());

            //Console.WriteLine(e2(Enumerable.Range(1, 10)));
            //Console.WriteLine(e2(Enumerable.Range(1, 20)));

            var nums = Enumerable.Range(1, 1000).ToArray();

            Measure(() => Extensions.CompileTemplate<int [], int>(ls => ls.AsQueryExpr().Sum()));

            //var e3 = Extensions.CompileTemplate<int[], int>(ls => ls.AsQueryExpr().Count());

            //Measure(() =>
            //{
            //    var s1 = 0;
            //    for (int i = 0; i < 100000; i++)
            //    {
            //        s1 += e3(nums);
            //    }
            //    Console.WriteLine(s1);
            //});


            //Measure(() =>
            //{
            //    var s2 = 0;
            //    for (int i = 0; i < 100000; i++)
            //    {
            //        s2 += nums.AsQueryExpr().Count().Run();
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
