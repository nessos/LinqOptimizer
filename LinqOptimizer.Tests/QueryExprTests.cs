using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Core;

namespace LinqOptimizer.Tests
{
    [TestFixture]
    class QueryExprTests
    {
        private int[] nums = Enumerable.Range(1, 5).ToArray();
 
        [Test]
        public void SelectTest()
        {
            var result = from num in nums.AsQueryExpr()
                         select num * 2;

            Assert.AreEqual(new[] { 2, 4, 6, 8, 10 }, result.Run());
        }

        [Test]
        public void SelectIndexedTest()
        {
            var result = nums.AsQueryExpr().Select((num, index) => index);

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4}, result.Run());
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
        public void WhereIndexedTest()
        {
            var result = nums.AsQueryExpr().Where((num, index) => index % 2 == 0);

            Assert.AreEqual(new[] { 1, 3, 5 }, result.Run());
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
        public void SelectManyComprehensionTest()
        {
            var result = (from num in nums.AsQueryExpr()
                          from _num in nums
                          select num * _num).Sum();

            Assert.AreEqual(225, result.Run());
        }

        [Test]
        public void SelectManyNestedTest()
        {
            var result = nums.AsQueryExpr()
                         .SelectMany(num => nums.SelectMany(_num => new[] { num * _num }))
                         .Sum();

            Assert.AreEqual(225, result.Run());
        }

        [Test]
        public void SelectManyPipelineTest()
        {

            var result =  nums.AsQueryExpr()
                            .Where(num => num % 2 == 0)
                            .SelectMany(num => nums.Select(_num => num * _num))
                            .Select(x => x + 1)
                            .Sum();

            Assert.AreEqual(100, result.Run());
        }

        [Test]
        public void EnumerableSourceTest()
        { 
            IEnumerable<int> _nums = nums.Select(x => x);

            var result = from _num in _nums.AsQueryExpr()
                         select _num * 2;

            Assert.AreEqual(new[] { 2, 4, 6, 8, 10 }, result.Run());
        }

        [Test]
        public void ListSourceTest()
        {
            List<int> _nums = nums.ToList();

            var result = from _num in _nums.AsQueryExpr()
                         select _num * 2;

            Assert.AreEqual(new[] { 2, 4, 6, 8, 10 }, result.Run());
        }

        [Test]
        public void TakeNTest()
        {
            var result = nums.AsQueryExpr().Take(2);

            Assert.AreEqual(new[] { 1, 2 }, result.Run());
        }

        [Test]
        public void SkipNTest()
        {
            var result = nums.AsQueryExpr().Skip(2);

            Assert.AreEqual(new[] { 3, 4, 5 }, result.Run());
        }

        [Test]
        public void TakeAndSkipNTest()
        {
            var result = nums.AsQueryExpr().Skip(2).Take(2);

            Assert.AreEqual(new[] { 3, 4 }, result.Run());
        }

        [Test]
        public void NestedTakeAndSkipNTest()
        {
            var result = nums.AsQueryExpr().Take(2).SelectMany(_ => nums.Skip(2).Take(2));

            Assert.AreEqual(new[] { 3, 4, 3, 4 }, result.Run());
        }

        [Test]
        public void ForEachTest()
        {
            var result = new List<int>();
            nums.AsQueryExpr().ForEach(num => result.Add(num)).Run();
            
            Assert.AreEqual(nums, result);
        }

        [Test]
        public void GroupByTest()
        {
            var result = (from num in new[] { 1, 1, 2, 2 }.AsQueryExpr()
                          group num by num into g
                          select g.Count()).Sum();

            Assert.AreEqual(4, result.Run());
        }

        [Test]
        public void OrderByTest()
        {
            var result = from num in nums.Reverse().AsQueryExpr()
                         orderby num
                         select num;

            Assert.AreEqual(nums, result.Run());

            var _result = from num in nums.AsQueryExpr()
                          orderby num descending 
                          select num;

            Assert.AreEqual(nums.Reverse(), _result.Run());
        }


        [Test]
        public void CountTest ()
        {
            var result = nums.AsQueryExpr().Select(i => i).Count();

            Assert.AreEqual(nums.Select(i => i).Count(), result.Run());
        }

        [Test]
        public void ToListTest()
        {
            var result = nums.AsQueryExpr().ToList();

            Assert.AreEqual(nums.ToList(), result.Run());
        }

        [Test]
        public void ToArrayTest()
        {
            var result = nums.AsQueryExpr().ToArray();

            Assert.AreEqual(nums.ToArray(), result.Run());
        }

        [Test]
        public void RangeTest()
        {
            var result = Enumerable.Range(1, 10).AsQueryExpr();

            Assert.AreEqual(Enumerable.Range(1, 10), result.Run());
        }

        [Test]
        public void RepeatTest()
        {
            var result = Enumerable.Repeat(42, 10).AsQueryExpr();

            Assert.AreEqual(Enumerable.Repeat(42, 10), result.Run());
        }

        [Test]
        public void RepeatValueTypeCastTest()
        {
            var t = (object) DateTime.Now;
            var result = Enumerable.Repeat(t, 10).AsQueryExpr();

            Assert.AreEqual(Enumerable.Repeat(t, 10), result.Run());
        }

        [Test]
        public void RangeWhereTest()
        {
            var result = Enumerable.Range(1, 10).AsQueryExpr().Where(f => f < 5);

            Assert.AreEqual(Enumerable.Range(1, 10).Where(f => f < 5), result.Run());
        }

        [Test]
        public void RepeatWhereTest()
        {
            var result = Enumerable.Repeat(42, 10).AsQueryExpr().Where(f => f < 5);

            Assert.AreEqual(Enumerable.Repeat(42, 10).Where(f => f < 5), result.Run());
        }

        //[Test]
        //public void ZipTest()
        //{
        //    var left  = new int [] { 1,2,3 };
        //    var right = new int [] { 4,5,6 };

        //    var result = QueryExpr.Zip(left, right, (l,r) => l * r);

        //    Assert.AreEqual(Enumerable.Zip(left, right, (l, r) => l * r), result.Run());
        //}

        [Test]
        public void RangeInvalidArgTest ()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => Enumerable.Range(0, -1).AsQueryExpr().Run());
        }

        [Test]
        public void RepeatInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => Enumerable.Repeat(0, -1).AsQueryExpr().Run());
        }

        [Test]
        public void NestedRangeInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => Enumerable.Range(1,10).AsQueryExpr().SelectMany( _ => Enumerable.Range(0, -1)).Run());
        }

        [Test]
        public void NestedRepeatInvalidArgTest()
        {
            Assert.Catch(typeof(ArgumentOutOfRangeException), () => Enumerable.Range(1, 10).AsQueryExpr().SelectMany(_ => Enumerable.Repeat(0, -1)).Run());
        }

    }
}
