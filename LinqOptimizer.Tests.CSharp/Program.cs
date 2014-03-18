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

        struct Node
        {
            public float x;
            public float y;
        }

        public static void Main(string[] args)
        {

            //var input = Enumerable.Range(1, 10).Select(x => (float)x).ToArray();
            //var nodes = input.Select(num => new Node { x = num, y = num }).ToArray();
            //using (var context = new GpuContext())
            //{
            //    using (IGpuArray<Node> buffer = context.CreateGpuArray(nodes))
            //    {
            //        int c = 12;
            //        var query = (from node in buffer.AsGpuQueryExpr()
            //                     let x = node.x + c
            //                     select new Node { x = x, y = x + c }).ToArray();

            //        var test = context.Run(query);

            //        var _test =
            //            (from node in nodes
            //             let x = node.x + c
            //             select new Node { x = x, y = x + c }).ToArray();

            //        var check = test.SequenceEqual(_test);

            //    }
            //}

            (new GpuQueryTests()).ZipWithReduction();
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
