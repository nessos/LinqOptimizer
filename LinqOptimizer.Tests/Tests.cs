using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Tests
{
    [TestFixture]
    class LinqTests
    {
        private int[] nums = new[] { 1, 2, 3 , 4, 5 };
 
        [Test]
        public void SelectTest()
        {
            var result = from num in nums.AsQueryExpr()
                         select num * 2;

            Assert.AreEqual(new[] { 2, 4, 6, 8, 10 }, result.Run());
        }

        [Test]
        public void WhereTest()
        {
            var result = from num in nums.AsQueryExpr()
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
            var result = (from num in nums.AsQueryExpr()
                         select num * 2).Sum();

            Assert.AreEqual(30, result.Run());
        }

        [Test]
        public void AggregateTest()
        {
            var result = (from num in nums.AsQueryExpr()
                          select num * 2).Aggregate(0, (acc, value) => acc + value);

            Assert.AreEqual(30, result.Run());
        }


        [Test]
        public void SelectManyTest()
        {
            var result = nums.AsQueryExpr()
                         .SelectMany(num => nums.Select(_num => num * _num))
                         .Sum();

            Assert.AreEqual(225, result.Run());
        }

        [Test]
        public void SelectManyPipelineTest()
        {

            var result =  nums.AsQueryExpr()
                            .Where(num => num % 2 == 0)
                            .SelectMany(num => nums.Select(_num => num * _num))
                            .Select(num => num + 1)
                            .Sum();

            Assert.AreEqual(100, result.Run());
        }
    }
}
