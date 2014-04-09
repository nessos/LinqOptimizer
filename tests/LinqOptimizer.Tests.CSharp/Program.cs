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
            Func<Tuple<float, float, float>, IEnumerable<Tuple<int, int, int>>>
              func =
                Extensions.Compile<Tuple<float, float, float>, IEnumerable<Tuple<int, int, int>>>(
                    t =>
                        from yp in Enumerable.Range(0, 10).AsQueryExpr()
                        from xp in Enumerable.Range(0, 10)
                        let _y = t.Item1 + t.Item3 * yp
                        let _x = t.Item2 + t.Item3 * xp
                        let c = new Complex(_x, _y)
                        let iters = 4
                        select Tuple.Create(xp, yp, iters)
                    );

            var m = Tuple.Create(1.0f,
                                 1.0f,
                                 1.0f);
            var result = func(m);
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
