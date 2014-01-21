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

namespace LinqOptimizer.Tests
{
    public class Program
    {

        public static void Main(string[] args)
        {

            //var test = new int[] { 1, 2, 3 }.AsGpuQueryExpr().Select(x => x + 1).Run();

            (new GpuQueryTests()).Pipelined();

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
