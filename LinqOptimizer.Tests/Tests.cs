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
        public void TestSelect()
        {
            var result = from num in nums.AsQueryExpr()
                         select num * 2;

            Assert.AreEqual(new[] { 2, 4, 6, 8, 10 }, result.Run());
        }

        [Test]
        public void TestWhere()
        {
            var result = from num in nums.AsQueryExpr()
                         where num % 2 == 0
                         select num;

            Assert.AreEqual(new[] { 2, 4 }, result.Run());
        }
    }
}
