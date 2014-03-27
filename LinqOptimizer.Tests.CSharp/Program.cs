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
