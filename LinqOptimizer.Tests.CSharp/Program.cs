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

            var input = Enumerable.Range(1, 100).ToArray();

            using (var context = new GpuContext())
            {
                using (var buffer = context.CreateGpuArray(input))
                {
                    var query = GpuQueryExpr.Zip(buffer, buffer, (a, b) => a * b).Where(x => x % 2 == 0).ToArray();
                    var test = context.Run(query);
                    var _test = Enumerable.Zip(input, input, (a, b) => a * b).Where(x => x % 2 == 0).ToArray();
                }
            }

            //(new GpuQueryTests()).Count();
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
