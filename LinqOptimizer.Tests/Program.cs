using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Tests
{
    class Program
    {

        public static void Main(string[] args)
        {
            
            Measure(() =>
            {
                List<string> list = new List<string>();
                for (int i = 0; i < 10000000; i++)
                {
                    var temp = new String('1', 10);
                    list.Add(temp);
                }
                Console.WriteLine(list.Count);
            });

            
            Measure(() =>
            {
                List<char[]> _list = new List<char[]>();
                var temp = new char[100000000];
                for (int i = 0; i < temp.Length; i++)
                {
                    temp[i] = '1';
                }
                Console.WriteLine(temp.Length);
            });


            //Measure(() =>
            //{
            //    List<int> _list = new List<int>();
            //    for (int i = 0; i < 300000000; i++)
            //    {
            //        _list.Add(i);
            //    }
            //});
            //Random r = new Random();
            //var nums = Enumerable.Range(1, 100000000).Select(num => r.Next(1000)).ToArray();
            

            //Func<int> f = nums.AsQueryExpr().GroupBy(num => num).Select(g => g.Count()).Sum().Compile();
            //Measure(() => Console.WriteLine(f.Invoke()));

            //Measure(() => Console.WriteLine(nums.AsParallel().GroupBy(num => num)
            //                                    .Select(g => g.Count())
            //                                    .Sum()));

            //var tests = new QueryExprTests();
            //tests.OrderByTest();
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
