using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Tests
{
    [TestFixture]
    class ParallelQueryExprTests
    {
        private int[] nums = Enumerable.Range(1, 5).ToArray();

        [Test]
        public void SelectTest()
        {
            var result = from num in nums.AsParallelQueryExpr()
                         select num * 2;

            var temp = result.Run();
            Assert.AreEqual(new[] { 2, 4, 6, 8, 10 }, temp);
        }

        [Test]
        public void WhereTest()
        {
            var result = from num in nums.AsParallelQueryExpr()
                         where num % 2 == 0
                         select num;

            Assert.AreEqual(new[] { 2, 4 }, result.Run());
        }

        [Test]
        public void PipelineTest()
        {
            var result = nums.AsParallelQueryExpr()
                         .Where(num => num % 2 == 0)
                         .Select(num => num * 2)
                         .Select(num => num.ToString())
                         .Select(num => num + "!");

            Assert.AreEqual(new[] { "4!", "8!" }, result.Run());
        }


        [Test]
        public void SumTest()
        {
            var result = (from num in nums.AsParallelQueryExpr()
                          select num * 2).Sum();

            Assert.AreEqual(30, result.Run());
        }


        [Test]
        public void SelectManyTest()
        {
            var result = nums.AsParallelQueryExpr()
                         .SelectMany(num => nums.Select(_num => num * _num))
                         .Sum();

            Assert.AreEqual(225, result.Run());
        }

        [Test]
        public void SelectManyComprehensionTest()
        {
            var result = (from num in nums.AsParallelQueryExpr()
                          from _num in nums
                          select num * _num).Sum();

            Assert.AreEqual(225, result.Run());
        }

        [Test]
        public void SelectManyNestedTest()
        {
            var result = nums.AsParallelQueryExpr()
                         .SelectMany(num => nums.SelectMany(_num => new[] { num * _num }))
                         .Sum();

            Assert.AreEqual(225, result.Run());
        }

        [Test]
        public void SelectManyPipelineTest()
        {

            var result = nums.AsParallelQueryExpr()
                            .Where(num => num % 2 == 0)
                            .SelectMany(num => nums.Select(_num => num * _num))
                            .Select(x => x + 1)
                            .Sum();

            Assert.AreEqual(100, result.Run());
        }

        [Test]
        public void GroupByTest()
        {
            var result = (from num in new[] { 1, 1, 2, 2 }.AsParallelQueryExpr()
                          group num by num into g
                          select g.Count()).Sum();

            Assert.AreEqual(4, result.Run());
        }

        [Test]
        public void OrderByTest()
        {
            Random random = new Random();
            var nums = Enumerable.Range(1, 10).Select(_ => random.Next()).ToArray();

            var result = from num in nums.AsParallelQueryExpr()
                         orderby num
                         select num;

            Assert.AreEqual(nums.OrderBy(x => x), result.Run());

            var _result = from num in nums.AsParallelQueryExpr()
                          orderby num descending
                          select num;

            Assert.AreEqual(nums.OrderByDescending(x => x), _result.Run());
        }

    }
}
