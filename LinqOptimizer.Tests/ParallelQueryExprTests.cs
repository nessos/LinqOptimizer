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
        private int[] nums = new[] { 1, 2, 3, 4, 5 };

        [Test]
        public void SelectTest()
        {
            var result = from num in nums.AsParallel().AsQueryExpr()
                         select num * 2;

            Assert.AreEqual(new[] { 2, 4, 6, 8, 10 }, result.Run());
        }

        
    }
}
