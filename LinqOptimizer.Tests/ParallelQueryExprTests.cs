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

        
    }
}
