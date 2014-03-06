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

            var input = Enumerable.Range(1, 100000000).Select(x => (float)x).ToArray();
            
            using (var context = new GpuContext())
            {
                using (var buffer = context.CreateGpuArray(input))
                {
                    var query = GpuQueryExpr.Zip(buffer, buffer, (a, b) => a * b).Sum();
                    Measure(() => context.Run(query), 100);
                    var parallelInput = input.AsParallel();
                    Measure(() => ParallelEnumerable.Zip(parallelInput, parallelInput, (a, b) => a * b).Sum(), 100);
                    Measure(() => Enumerable.Zip(input, input, (a, b) => a * b).Sum(), 100);
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
