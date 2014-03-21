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
        [StructLayout(LayoutKind.Sequential)]
        struct Node
        {
            public float x;
            public float y;
        }

        public static void Main(string[] args)
        {

            var xs = Enumerable.Range(1, 10).Select(x => x).ToArray();
            using (var context = new GpuContext())
            {
                using (var _xs = context.CreateGpuArray(xs))
                {
                    var query = (from n in _xs.AsGpuQueryExpr()
                                 let pi = Math.PI
                                 let c = Math.Cos(n)
                                 let s = Math.Sin(n)
                                 let f = Math.Floor(pi)
                                 let sq = Math.Sqrt(n * n)
                                 let ex = Math.Exp(pi)
                                 let p = Math.Pow(pi, 2)
                                 select f * pi * c * s * sq * ex * p).ToArray();

                    var gpuResult = context.Run(query);

                    var cpuResult =
                        (from n in xs
                         let pi = Math.PI
                         let c = Math.Cos(n)
                         let s = Math.Sin(n)
                         let f = Math.Floor(pi)
                         let sq = Math.Sqrt(n * n)
                         let ex = Math.Exp(pi)
                         let p = Math.Pow(pi, 2)
                         select f * pi * c * s * sq * ex * p).ToArray();

                    var check = gpuResult.Zip(cpuResult, (x, y) => System.Math.Abs(x - y) < 0.001f).ToArray();
                }
            }

            //(new GpuQueryTests()).GpuArrayIndexer();
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
