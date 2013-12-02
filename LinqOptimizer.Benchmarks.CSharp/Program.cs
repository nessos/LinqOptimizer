using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LinqOptimizer.CSharp;

namespace LinqOptimizer.Benchmarks.CSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            //var v = Enumerable.Range(1, 50000000).Select(x => (double)x).ToArray();
            //Measure("Sum Linq", () => SumLinq(v),
            //        "Sum Opt", () => SumLinqOpt(v),
            //        (x1, x2) => x1 == x2);

            //Measure("Sum of Squares Linq", () => SumSqLinq(v),
            //        "Sum of Squares Opt", () => SumSqLinqOpt(v),
            //        (x1, x2) => x1 == x2);

            //var v2 = v.Take(20).ToArray();
            //Measure("Cartesian Linq", () => CartLinq(v, v2),
            //        "Cartesian Opt", () => CartLinqOpt(v, v2),
            //        (x1, x2) => x1 == x2);

            var s = 50000000;
            Measure("GroupBy Linq", () => GroupLinq(s),
                    "GroupBy Opt", () => GroupLinqOpt(s),
                    (x1, x2) => Enumerable.SequenceEqual(x1, x2));

            //var n = 1000;
            //Measure("Pythagorean Triples Linq", () => PythagoreanTriplesLinq(n),
            //        "Pythagorean Triples Opt", () => PythagoreanTriplesLinqOpt(n),
            //        (x1, x2) => Enumerable.SequenceEqual(x1, x2));

        }

        static void Measure<T>(string title1, Func<T> action1, string title2, Func<T> action2, Func<T,T,bool> validate)
        {
            var sw = new Stopwatch();
            sw.Start();
            var t1 = action1();
            sw.Stop();
            Console.WriteLine("\"{0}\":\t{1}",title1, sw.Elapsed);
            sw.Restart();
            var t2 = action2();
            sw.Stop();
            Console.WriteLine("\"{0}\":\t{1}", title2, sw.Elapsed);
            Console.WriteLine("Validate : {0}", validate(t1, t2));
            Console.WriteLine();
        }

        static double SumLinq(double[] values)
        {
            return values.Sum();
        }

        static double SumLinqOpt(double[] values)
        {
            return values.AsQueryExpr().Sum().Run();
        }

        static double SumSqLinq(double[] values)
        {
            return values.Select(x => x * x).Sum();
        }

        static double SumSqLinqOpt(double[] values)
        {
            return values.AsQueryExpr().Select(x => x * x).Sum().Run();
        }

        static double CartLinq(double[] dim1, double[] dim2)
        {
            return (from x in dim1
                    from y in dim2
                    select x * y).Sum();
        }

        static double CartLinqOpt(double[] dim1, double[] dim2)
        {
            return (from x in dim1.AsQueryExpr()
                    from y in dim2
                    select x * y).Sum().Run();
        }

        static int[] GroupLinq(int size)
        {
            var rnd = new Random(size);
            return Enumerable.Range(1, size)
                   .Select(x => 100 * rnd.NextDouble() - 50)
                   .GroupBy(x => (int)x % 10)
                   .Select(x => x.Count())
                   .ToArray();
        }

        static int[] GroupLinqOpt(int size)
        {
            var rnd = new Random(size);
            return Enumerable.Range(1, size).AsQueryExpr()
                   .Select(x => 100 * rnd.NextDouble() - 50)
                   .GroupBy(x => (int)x % 10)
                   .Select(x => x.Count())
                   .ToArray()
                   .Run();
        }

        static Tuple<int, int, int>[] PythagoreanTriplesLinq(int max)
        {
            return (from a in Enumerable.Range(1, max + 1)
                    from b in Enumerable.Range(a, max + 1 - a)
                    from c in Enumerable.Range(b, max + 1 - b)
                    where a * a + b * b == c * c
                    select Tuple.Create(a, b, c)).ToArray();
        }

        static Tuple<int, int, int>[] PythagoreanTriplesLinqOpt(int max)
        {
            return (from a in Enumerable.Range(1, max + 1).AsQueryExpr()
                    from b in Enumerable.Range(a, max + 1 - a)
                    from c in Enumerable.Range(b, max + 1 - b)
                    where a * a + b * b == c * c
                    select Tuple.Create(a, b, c)).ToArray().Run();
        }

    }
}
