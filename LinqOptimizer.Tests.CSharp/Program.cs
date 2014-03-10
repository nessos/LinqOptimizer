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
using LinqOptimizer.Gpu.CSharp;
using LinqOptimizer.Base;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.IO;
using LinqOptimizer.Gpu;

namespace LinqOptimizer.Tests
{


    public class Program
    {
            

        public static void Main(string[] args)
        {

            var input = Enumerable.Range(1, 1000).Select(x => (float)x).ToArray();
            
            using (var context = new GpuContext())
            {
                using (var buffer = context.CreateGpuArray(input))
                {
                    var query = (from num in buffer.AsGpuQueryExpr()
                                         let a = num * 2
                                         let c = a + 1
                                         let b = a * 2
                                         let e = b - 5
                                         let d = c * c
                                         let m = 3
                                         select a + b + c + d + e + m + num).Sum();
                    var test = context.Run(query);
                    var _test =
                        (from num in input
                         let a = num * 2
                         let c = a + 1
                         let b = a * 2
                         let e = b - 5
                         let d = c * c
                         let m = 3
                         select a + b + c + d + e + m + num).Sum();
                }
            }

            
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
