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

            var input = Enumerable.Range(1, 10).Select(x => (float)x).ToArray();
            var nodes = input.Select(num => new Node { x = num, y = num }).ToArray();
            using (var context = new GpuContext())
            {
                using (var buffer = context.CreateGpuArray(nodes))
                {
                    var query = (from node in buffer.AsGpuQueryExpr()
                                 let x = node.x + 1
                                 let y = node.y + 2
                                 select new Node { x = x, y = y }).ToArray();

                    var test = context.Run(query);

                    var _test =
                        (from node in nodes
                         let x = node.x + 1
                         let y = node.y + 2
                         select new Node { x = x, y = y }).ToArray();

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
