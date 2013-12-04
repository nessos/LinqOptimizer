using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqOptimizer.Core;
using LinqOptimizer.CSharp;


using System.Xml.Linq;
using LinqOptimizer.Base;

namespace LinqOptimizer.Tests
{
    public class Program
    {

      


        public static void Main(string[] args)
        {
            var xs =
                Enumerable.Range(1, 10)
                    .AsQueryExpr()
                    .TakeWhile(x => x < 5)
                    .Run();
            
            

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
