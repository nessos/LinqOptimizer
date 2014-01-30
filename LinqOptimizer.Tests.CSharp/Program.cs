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

            var input = Enumerable.Range(1, 65000000).Select(x => (double)x).ToArray();

            //var result = input.AsGpuQueryExpr().Where(x => x % 2 == 0).Select(x => x + 1).Run();


            //Measure(() => input.AsParallelQueryExpr()
            //                    //.Where(x => x % 2 == 0)
            //                    .Select(x => x * 2).Run());
            //Measure(() => input.AsParallelQueryExpr()
            //        //.Where(x => x % 2 == 0)
            //        .Select(x => x * 2).Run());
            //Measure(() => input.AsQueryExpr()
            //        .Select(x => x * 2).Select(x => x * 2).Select(x => x * 2).Sum().Run());
            //Measure(() => input.AsQueryExpr()
            //                .Select(x => x * 2).Select(x => x * 2).Select(x => x * 2).Sum().Run());

            //Measure(() => input.AsParallelQueryExpr()
            //        .Select(x => x * 2).Select(x => x * 2).Select(x => x * 2).Sum().Run());
            //Measure(() => input.AsParallelQueryExpr()
            //                    .Select(x => x * 2).Select(x => x * 2).Select(x => x * 2).Sum().Run());

            //Measure(() => input.Select(x => x * 2).Select(x => x * 2).Select(x => x * 2).Sum());

            //Measure(() => input.AsParallel()
            //                    .Select(x => x * 2).Select(x => x * 2).Select(x => x * 2).Sum());
            
            //Measure(() => input.AsParallel().Where(x => x % 2 == 0).Select(x => x * 2).ToArray());
            //Measure(() => input.Where(x => x % 2 == 0).Select(x => x * 2).ToArray());
            //Measure(() => input.AsGpuQueryExpr()
            //                    .Select(x => x).Run());
            //Measure(() => input.AsGpuQueryExpr()
            //                    .Select(x => x).Run());

            //Measure(() => input.AsGpuQueryExpr()
            //    //.Where(x => x % 2 == 0)
            //        .Select(x => x * 2).Run());
            //Measure(() => input.AsGpuQueryExpr()
            //    //.Where(x => x % 2 == 0)
            //                .Select(x => x * 2).Run());


            (new GpuQueryTests()).Where();

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
