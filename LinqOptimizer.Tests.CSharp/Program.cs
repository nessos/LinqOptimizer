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


namespace LinqOptimizer.Tests
{
    class Program
    {



        public static void Main(string[] args)
        {
            Random random = new Random();
            //var nums = Enumerable.Range(1, 10000000).Select(_ => random.Next(1, 10000000)).Select(x => x.ToString()).ToArray();

            //////var list = new KeyValuePair<int, int>[100000000];
            //////Measure(() =>
            //////{
            //////    for (int i = 0; i < nums.Length - 1; i++)
            //////    {
            //////        var item = nums[i];
            //////        list[i] = new KeyValuePair<int, int>(item, item);
            //////    }
            //////});
            //////Measure(() => Array.Sort(nums));
            //var keys = nums.Select(x => x).ToArray();
            //var values = nums.Select(x => x).ToArray();
            //var comparer = new StringComparer();
            //Measure(() => Array.Sort(keys, values, comparer));
            //Measure(() => Array.Sort(keys, values));
            //Measure(() => DoQuickSort(nums, 0, nums.Length - 1));
            //Measure(() => ParallelSort.QuicksortSequential(values));
            //Measure(() => ParallelSort.QuicksortParallel(values));
            //Measure(() => Array.Sort(list, new KeyValuePairComparer<int>()));
            //Measure(() => nums.OrderBy(x => x).ToList());

            //Measure(() => nums.GroupBy(x => x).ToList());
            //Measure(() => nums.Aggregate(new Dictionary<int, List<int>>(), (acc, v) => { return Grouping(acc, v); }));
            //Measure(() => Console.WriteLine(nums.AsParallel().Aggregate(() => 0.0, (acc, v) => { return ((double) v + 1 + 1 + 1)  + acc; }, (left, right) => { return left + right; }, x => x)));
            //Measure(() => Console.WriteLine(nums.AsParallel().Select(x => (double)x).Sum()));
            //Measure(() =>
            //{
            //    var dict = new Dictionary<int, List<int>>();
            //    for (int i = 0; i < nums.Length; i++)
            //    {
            //        dict = Grouping(dict, nums[i], nums[i]);
            //    }
            //});

            //var _nums = nums.ToList();
            //Measure(() => _nums.Sort((x, y) => { if (x < y) return -1; else if (x == y) return 0; else return 1; }));

            //Measure(() => Console.WriteLine(ParallelismHelpers.ReduceCombine(nums, () => 0.0, (acc, v) => { return (double) v + acc; }, (left, right) => { return left + right; }, x => x)));
            //Measure(() => Console.WriteLine(ParallelismHelpers.ReduceCombine(nums, () => new Dictionary<int, List<int>>(), (acc, v) => { return Grouping(acc, v, v); }, (left, right) => { return Merge(left, right); }, x => x)));

            //var tests = new FsCheckQueryExpr();
            //tests.SumDouble();
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
