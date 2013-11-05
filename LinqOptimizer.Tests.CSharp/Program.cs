using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using LinqOptimizer.Core;
using LinqOptimizer.CSharp;

namespace LinqOptimizer.Tests
{
    class Program
    {
        static IEnumerable<int> Identity(IEnumerable<int> x) { return x; }

        public static void Main(string[] args)
        {
            //Random random = new Random();
            //var nums = Enumerable.Range(1, 100000000).Select(_ => random.Next(1, 10000000)).Select(x => x).ToArray();
            //var keys = nums.ToArray();    

            //var xs = Enumerable.Range(1, 10).AsQueryExpr().Select(x => Identity(Enumerable.Range(1, x).Select(i => i * x))).Run();
            //var xs = Enumerable.Range(1, 10).AsQueryExpr().Select(x => 1 + Enumerable.Range(1, 10).Sum() ).Run();

            //var xs = new int[] { 1, 1 };
            //    //Enumerable.Range(1, 10);

            //var x = xs.AsQueryExpr()
            //        .SelectMany(m => Enumerable.Repeat(m, Enumerable.Range(1, 10).Sum()).Select(i => i * i))
            //        .Run();

            var n = int.Parse(Console.ReadLine());

            Measure(() => Console.WriteLine(STotientLinq(n)));
            Measure(() => Console.WriteLine(STotientOpt(n)));
            Measure(() => Console.WriteLine(STotientHand(n)));

            //var tests = new QueryTests();
            //tests.SelectMany();

        }

        static int STotientLinq(int n)
        {
            return Enumerable.Range(0, n)
                    .Select(nn => Enumerable.Range(1, nn).Where(k => gcd(k) == 1).Count())
                    .Sum();
        }

        static int STotientOpt(int n)
        {
            //var mi = typeof(Class1).GetMethod("mygcd");
            //Expression<Func<int,bool>> f = Expression.Lambda<Func<int,bool>>(Expression.Equal(Expression.Call(mi, Expression.Constant(Expression.Parameter(typeof(int),"k"))), Expression.Constant(1)), Expression.Parameter(typeof(int), "k"));

            return Enumerable.Range(0, n).AsQueryExpr()
                    .Select(nn => Enumerable.Range(1, nn).Where(k => gcd(k) == 1).Count())
                    .Sum()
                    .Run();
        }

        static int STotientHand(int n)
        {
            var sum = 0;
            for (int nn = 0; nn < n; nn++)
            {
                var count = 0;
                for (int k = 1; k < nn; k++)
                {
                    if (gcd(k) == 1)
                        count += 1;
                }
                sum += count;
            }
            return sum;
        }

        [System.Runtime.CompilerServices.MethodImpl(MethodImplOptions.NoInlining)]
        static int gcd(int a) { return 42; }


        [System.Runtime.CompilerServices.MethodImpl(MethodImplOptions.NoInlining)]
        static int gcd(int a, int b)
        {
            //int Remainder;
            //while (b != 0)
            //{
            //    Remainder = a % b;
            //    a = b;
            //    b = Remainder;
            //}
            //return a;
            return 42;
        }

        //static int gcd(int a, int b)
        //{
        //    while (a != b)
        //        if (a > b)
        //            a = a - b;
        //        else
        //            b = b - a;
        //    return a;
        //}

        static void Measure(Action action)
        {
            var watch = new Stopwatch();
            watch.Start();
            action();
            Console.WriteLine(watch.Elapsed);
        }
    }
}
