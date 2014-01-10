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
        public static bool Foo(Tuple<int,int,int> t)
        {
            var b =  t.Item1 * t.Item1 + t.Item2 * t.Item2 == t.Item3 * t.Item3;
            if (b)
                return b;
            return b;
        }

        public static void Main(string[] args)
        {
            var max = 100;
            var mm =
                QueryExpr.Range(1, max + 1)
                    .SelectMany(a =>
                        Enumerable.Range(a, max + 1 - a)
                            .SelectMany(b =>
                                Enumerable.Range(b, max + 1 - b)
                                    .Select(c => new Tuple<int,int,int>(a,b,c)))
                    .Where(t => Foo(t)))
                    .Count()
                    .Run();

            //var ll = 
            //Enumerable.Range(1, max + 1)
            //    .SelectMany(a =>
            //        Enumerable.Range(a, max + 1 - a)
            //            .SelectMany(b =>
            //                Enumerable.Range(b, max + 1 - b)
            //                    .Select(c => new Tuple<int, int, int>(a, b, c)))
            //    .Where(t => Foo(t)))
            //    .Count();

            //var m = QueryExpr.Zip(Enumerable.Range(1, 10).ToArray(), Enumerable.Range(1, 10).ToArray(), (x, y) => x * Enumerable.Range(1,10).Count()).Run();
            //var x = QueryExpr.Range(1, 0).ToArray().Run();
            //var e1 = Extensions.CompileTemplate<int, List<int>>(
            //        t => Enumerable.Range(1, 10).AsQueryExpr().Select(x => x * t).ToList());
            
            //e1(10).ForEach(Console.WriteLine);


            //var e = Extensions.Compile<IEnumerable<int>, int>(nums => nums.Select(x => x * x).AsQueryExpr().Count());
            
            //var e = ParallelExtensions.Compile<IEnumerable<int>, int>(nums => nums.AsParallelQueryExpr().Select(x => x).Count());

            //var xs = e(Enumerable.Range(1, 10));

            //var e2 = Extensions.Compile<IEnumerable<int>, int>(ls => ls.AsQueryExpr().Count());

            ////Console.WriteLine(e2(Enumerable.Range(1, 10)));
            ////Console.WriteLine(e2(Enumerable.Range(1, 20)));

            //var nums = Enumerable.Range(1, 1000).ToArray();

            //Measure(() => Extensions.Compile<Tuple<int, int>, int>(ls => Enumerable.Range(ls.Item1, ls.Item2).AsQueryExpr().Count()));

            //var e3 = Extensions.Compile<Tuple<int, int>, int>(ls => Enumerable.Range(ls.Item1, ls.Item2).AsQueryExpr().Count());

            //Measure(() =>
            //{
            //    var s1 = 0;
            //    for (int i = 0; i < 10000; i++)
            //    {
            //        s1 += e3(Tuple.Create(0,i));
            //    }
            //    Console.WriteLine(s1);
            //});


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
