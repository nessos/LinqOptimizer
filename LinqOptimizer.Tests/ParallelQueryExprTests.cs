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
            var result = from num in nums.AsParallel().AsQueryExpr()
                         select num * 2;

            Assert.AreEqual(new[] { 2, 4, 6, 8, 10 }, result.Run());
        }

        [Test]
        public void WhereTest()
        {
            var result = from num in nums.AsParallel().AsQueryExpr()
                         where num % 2 == 0
                         select num;

            Assert.AreEqual(new[] { 2, 4 }, result.Run());
        }

        [Test]
        public void PipelineTest()
        {
            var result = nums.AsQueryExpr()
                         .Where(num => num % 2 == 0)
                         .Select(num => num * 2)
                         .Select(num => num.ToString())
                         .Select(num => num + "!");

            Assert.AreEqual(new[] { "4!", "8!" }, result.Run());
        }


        [Test]
        public void SumTest()
        {
            var result = (from num in nums.AsParallel().AsQueryExpr()
                          select num * 2).Sum();

            Assert.AreEqual(30, result.Run());
        }


        [Test]
        public void SelectManyTest()
        {
            var result = nums.AsParallel().AsQueryExpr()
                         .SelectMany(num => nums.Select(_num => num * _num))
                         .Sum();

            Assert.AreEqual(225, result.Run());
        }

        [Test]
        public void SelectManyComprehensionTest()
        {
            var result = (from num in nums.AsParallel().AsQueryExpr()
                          from _num in nums
                          select num * _num).Sum();

            Assert.AreEqual(225, result.Run());
        }

        [Test]
        public void SelectManyNestedTest()
        {
            var result = nums.AsParallel().AsQueryExpr()
                         .SelectMany(num => nums.SelectMany(_num => new[] { num * _num }))
                         .Sum();

            Assert.AreEqual(225, result.Run());
        }

        [Test]
        public void SelectManyPipelineTest()
        {

            var result = nums.AsParallel().AsQueryExpr()
                            .Where(num => num % 2 == 0)
                            .SelectMany(num => nums.Select(_num => num * _num))
                            .Select(x => x + 1)
                            .Sum();

            Assert.AreEqual(100, result.Run());
        }


        [Test]
        public void GroupByTest()
        {
            var result = (from num in new[] { 1, 1, 2, 2 }.AsParallel().AsQueryExpr()
                          group num by num into g
                          select g.Count()).Sum();

            Assert.AreEqual(4, result.Run());
        }

        [Test]
        public void OrderByTest()
        {
            var result = from num in nums.Reverse().AsParallel().AsQueryExpr()
                         orderby num
                         select num;

            Assert.AreEqual(nums, result.Run());

            var _result = from num in nums.AsParallel().AsQueryExpr()
                          orderby num descending
                          select num;

            Assert.AreEqual(nums.Reverse(), _result.Run());
        }

    }
}
