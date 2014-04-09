using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nessos.LinqOptimizer.CSharp;
using Nessos.LinqOptimizer.Base;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.Numerics;

namespace Nessos.LinqOptimizer.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var a1 = Enumerable.Range(0, 10).ToArray();
            var a2 = Enumerable.Range(0, 10).ToArray();

            Func<float, float, float, IEnumerable<Tuple<int, int, int>>>
                func =
                    Extensions.Compile<float, float, float, IEnumerable<Tuple<int, int, int>>>(
                        (ymin, xmin, step) =>
                            from yp in a1.AsQueryExpr()
                            from xp in a2
                            let _y = ymin + step * yp
                            let _x = xmin + step * xp
                            let c = new Complex(_x, _y)
                            let iters = 4
                            select Tuple.Create(xp, yp, iters)
                        );

            var result = func(1.0f, 1.0f, 1.0f);
        }

        static void Measure(Action action)
        {
            Measure(action, 1);
        }

        static void Measure(Action action, int iterations)
        {
            var watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                action();
            }
            Console.WriteLine(new TimeSpan(watch.Elapsed.Ticks / iterations));
        }
    }
}
