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

    internal struct MyStruct
    {

    }

    class FooT : IFoo
    {
        public FooT() { }
    }

    internal interface IFoo { }

    public class Foo 
    {
        internal int Foobar() { return 42; }
        internal int thefield = 42;
        internal int TheProp { get; set; }

        public object GetMyStruct() { return new MyStruct(); }
    }


    public class Program
    {

        public static void Main(string[] args)
        {
            var max = 100;

            //var mm = QueryExpr.Range(1, max)
            //         .Select(i => new Tuple<int, int>(i, i + 1))
            //         .Select(t => new Tuple<int, int, int>(t.Item1 + 1, t.Item2 + 1, t.Item2 + 1))
            //         .Select(m => new Tuple<int, int, int, int>(m.Item1 + 2, m.Item2 + 2, m.Item3 + 2, m.Item3 + 2))
            //         .Run();


            var mm = QueryExpr.Range(1, max)
                     .Select(i => new FooT())
                     .Select(m => m)
                     .Run();

            //Func<int, int, int, bool> f = (x, y, z) => x * x + y * y == z * z;

            //var mm =
                //Enumerable.Range(1, max + 1).AsQueryExpr()
                //    .SelectMany(a =>
                //        Enumerable.Range(a, max + 1 - a)
                //            .SelectMany(b =>
                //                Enumerable.Range(b, max + 1 - b)
                //                    .Select(c => new Tuple<int, int, int>(a, b, c))))
                //    .Where(t => f(t.Item1, t.Item2, t.Item3))
                //    .ForEach(t => Console.WriteLine("{0} {1} {2}",t.Item1, t.Item2, t.Item3))
                //    .Run();

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
