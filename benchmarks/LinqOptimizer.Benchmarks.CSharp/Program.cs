using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Nessos.LinqOptimizer.CSharp;

namespace LinqOptimizer.Benchmarks.CSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var rnd = new Random();
            var v = Enumerable.Range(1, 200000000).Select(x => rnd.NextDouble()).ToArray();
            Func<double,double,bool> cmp = (x1, x2) => Math.Abs(x1 - x2) < 1E-07;
            
            Measure("Sum Linq", () => SumLinq(v),
                    "Sum Opt", () => SumLinqOpt(v),
                    cmp);

            Measure("Sum of Squares Linq", () => SumSqLinq(v),
                    "Sum of Squares Opt", () => SumSqLinqOpt(v),
                    cmp);

            var v1 = v.Take(v.Length / 10).ToArray();
            var v2 = v.Take(20).ToArray();
            Measure("Cartesian Linq", () => CartLinq(v1, v2),
                    "Cartesian Opt", () => CartLinqOpt(v1, v2),
                    cmp);

            var g = Enumerable.Range(1, 20000000).Select(x => 100000000 * rnd.NextDouble() - 50000000).ToArray();
            Measure("GroupBy Linq", () => GroupLinq(g),
                    "GroupBy Opt", () => GroupLinqOpt(g),
                    (x1, x2) => Enumerable.SequenceEqual(x1, x2));

            var n = 1000;
            Measure("Pythagorean Triples Linq", () => PythagoreanTriplesLinq(n),
                    "Pythagorean Triples Opt", () => PythagoreanTriplesLinqOpt(n),
                    cmp);


            /////////////////////
            //var pv = Enumerable.Range(1, 400000000).Select(x => rnd.NextDouble()).ToArray();
            //var pv1 = pv.Take(pv.Length / 10).ToArray();
            //var pv2 = pv.Take(20).ToArray();

            var pv = v;
            var pv1 = v1;
            var pv2 = v2;

            Measure("Parallel Sum Linq", () => ParallelSumLinq(pv),
                    "Parallel Sum Opt", () => ParallelSumLinqOpt(pv),
                    cmp);

            Measure("Parallel Sum of Squares Linq", () => ParallelSumSqLinq(pv),
                    "Parallel Sum of Squares Opt", () => ParallelSumSqLinqOpt(pv),
                    cmp);

            Measure("Parallel Cartesian Linq", () => ParallelCartLinq(pv1, pv2),
                    "Parallel Cartesian Opt", () => ParallelCartLinqOpt(pv1, pv2),
                    cmp);

            Measure("Parallel GroupBy Linq", () => ParallelGroupLinq(g),
                    "Parallel GroupBy Opt", () => ParallelGroupLinqOpt(g),
                    (x1, x2) => Enumerable.SequenceEqual(x1, x2));

            Measure("Parallel Pythagorean Triples Linq", () => ParallelPythagoreanTriplesLinq(n),
                    "Parallel Pythagorean Triples Opt", () => ParallelPythagoreanTriplesLinqOpt(n),
                    cmp);

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
            var equal = validate(t1, t2);
            Console.WriteLine("Validate : {0}", equal);
            if(!equal)
                Console.WriteLine("Values {0}, {1}", t1, t2);
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

        static int[] GroupLinq(double[] values)
        {
            return values
                   .GroupBy(x => (int)x / 100)
                   .OrderBy(x => x.Key)
                   .Select(k => k.Count())
                   .ToArray();
        }

        static int[] GroupLinqOpt(double[] values)
        {
            return values.AsQueryExpr()
                   .GroupBy(x => (int)x / 100)
                   .OrderBy(x => x.Key)
                   .Select(k => k.Count())
                   .ToArray()
                   .Run();
        }

        static int PythagoreanTriplesLinq(int max)
        {
            return (from a in Enumerable.Range(1, max + 1)
                    from b in Enumerable.Range(a, max + 1 - a)
                    from c in Enumerable.Range(b, max + 1 - b)
                    where a * a + b * b == c * c
                    select true).Count();
        }

        static int PythagoreanTriplesLinqOpt(int max)
        {
            return (from a in QueryExpr.Range(1, max + 1)
                    from b in Enumerable.Range(a, max + 1 - a)
                    from c in Enumerable.Range(b, max + 1 - b)
                    where a * a + b * b == c * c
                    select true).Count().Run();
        }


        ///////////////////////////////////////////////////////////////////

        static double ParallelSumLinq(double[] values)
        {
            return values.AsParallel().Sum();
        }

        static double ParallelSumLinqOpt(double[] values)
        {
            return values.AsParallelQueryExpr().Sum().Run();
        }

        static double ParallelSumSqLinq(double[] values)
        {
            return values.AsParallel().Select(x => x * x).Sum();
        }

        static double ParallelSumSqLinqOpt(double[] values)
        {
            return values.AsParallelQueryExpr().Select(x => x * x).Sum().Run();
        }

        static double ParallelCartLinq(double[] dim1, double[] dim2)
        {
            return (from x in dim1.AsParallel()
                    from y in dim2
                    select x * y).Sum();
        }

        static double ParallelCartLinqOpt(double[] dim1, double[] dim2)
        {
            return (from x in dim1.AsParallelQueryExpr()
                    from y in dim2
                    select x * y).Sum().Run();
        }

        static int[] ParallelGroupLinq(double[] values)
        {
            return values.AsParallel()
                   .GroupBy(x => (int)x / 100)
                   .OrderBy(x => x.Key)
                   .Select(k => k.Count())
                   .ToArray();
        }

        static int[] ParallelGroupLinqOpt(double[] values)
        {
            return values.AsParallelQueryExpr()
                   .GroupBy(x => (int)x / 100)
                   .OrderBy(x => x.Key)
                   .Select(k => k.Count())
                   .ToArray()
                   .Run();
        }

        static int ParallelPythagoreanTriplesLinq(int max)
        {
            return (from a in Enumerable.Range(1, max + 1).AsParallel()
                    from b in Enumerable.Range(a, max + 1 - a)
                    from c in Enumerable.Range(b, max + 1 - b)
                    where a * a + b * b == c * c
                    select true).Count();
        }

        static int ParallelPythagoreanTriplesLinqOpt(int max)
        {
            return (from a in Enumerable.Range(1, max + 1).AsParallelQueryExpr()
                    from b in Enumerable.Range(a, max + 1 - a)
                    from c in Enumerable.Range(b, max + 1 - b)
                    where a * a + b * b == c * c
                    select true).Count().Run();
        }
    }
}
