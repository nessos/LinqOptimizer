using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Gpu;
using LinqOptimizer.Gpu.CSharp;
using FsCheck.Fluent;
using System.Threading;

namespace LinqOptimizer.Tests
{
    [TestFixture]
    class GpuQueryTests
    {
        [Test]
        public void Select()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(xs =>
                {
                    using (var _xs = context.CreateGpuArray(xs))
                    {
                        var x = context.Run(_xs.AsGpuQueryExpr().Select(n => n * 2).ToArray());
                        var y = xs.Select(n => n * 2).ToArray();
                        return x.SequenceEqual(y);
                    }
                }).QuickCheckThrowOnFailure();    
            }
        }


        [Test]
        public void Pipelined()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(xs =>
                {
                    using (var _xs = context.CreateGpuArray(xs))
                    {

                        var x = context.Run(_xs.AsGpuQueryExpr()
                                              .Select(n => (float)n * 2)
                                              .Select(n => n + 1).ToArray());
                        var y = xs
                                .Select(n => (float)n * 2)
                                .Select(n => n + 1).ToArray();

                        return x.SequenceEqual(y);
                    }

                }).QuickCheckThrowOnFailure();
            }
        }

        [Test]
        public void Where()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(xs =>
                {

                    using (var _xs = context.CreateGpuArray(xs))
                    {

                        var x = context.Run((from n in _xs.AsGpuQueryExpr()
                                            where n % 2 == 0
                                            select n + 1).ToArray());
                        var y = (from n in xs
                                 where n % 2 == 0
                                 select n + 1).ToArray();

                        return x.SequenceEqual(y);
                    }
                }).QuickCheckThrowOnFailure();
            }
        }

        [Test]
        public void Sum()
        {
            using (var context = new GpuContext())
            {

                Spec.ForAny<int[]>(xs =>
                {
                    using (var _xs = context.CreateGpuArray(xs))
                    {

                        var x = context.Run((from n in _xs.AsGpuQueryExpr()
                                             select n + 1).Sum());
                        var y = (from n in xs
                                 select n + 1).Sum();

                        return x == y;
                    }
                }).QuickCheckThrowOnFailure();
            }
        }

        [Test]
        public void Count()
        {
            using (var context = new GpuContext())
            {

                Spec.ForAny<int[]>(xs =>
                {
                    using (var _xs = context.CreateGpuArray(xs))
                    {
                        var x = context.Run((from n in _xs.AsGpuQueryExpr()
                                             where n % 2 == 0
                                             select n + 1).Count());

                        var y = (from n in xs
                                 where n % 2 == 0
                                 select n + 1).Count();

                        return x == y;
                    }
                }).QuickCheckThrowOnFailure();
            }
        }

        [Test]
        public void ToArray()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(xs =>
                {
                    using (var _xs = context.CreateGpuArray(xs))
                    {
                        var x = context.Run(_xs.AsGpuQueryExpr().Select(n => n * 2).ToArray());
                        var y = xs.Select(n => n * 2).ToArray();
                        return x.SequenceEqual(y);
                    }
                }).QuickCheckThrowOnFailure();
            }
        }

        [Test]
        public void Zip()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(ms =>
                {
                    using (var _ms = context.CreateGpuArray(ms))
                    {
                        var xs = context.Run(GpuQueryExpr.Zip(_ms, _ms, (a, b) => a * b).Select(x => x + 1).ToArray());
                        var ys = Enumerable.Zip(ms, ms, (a, b) => a * b).Select(x => x + 1).ToArray();

                        return xs.SequenceEqual(ys);
                    }
                }).QuickCheckThrowOnFailure();
            }
        }

        [Test]
        public void ZipWithFilter()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(ms =>
                {
                    using (var _ms = context.CreateGpuArray(ms))
                    {
                        var xs = context.Run(GpuQueryExpr.Zip(_ms, _ms, (a, b) => a * b).Where(x => x % 2 == 0).ToArray());
                        var ys = Enumerable.Zip(ms, ms, (a, b) => a * b).Where(x => x % 2 == 0).ToArray();

                        return xs.SequenceEqual(ys);
                    }
                }).QuickCheckThrowOnFailure();
            }
        }

        [Test]
        public void ZipWithReduction()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(ms =>
                {
                    using (var _ms = context.CreateGpuArray(ms))
                    {
                        var xs = context.Run(GpuQueryExpr.Zip(_ms, _ms, (a, b) => a * b).Sum());
                        var ys = Enumerable.Zip(ms, ms, (a, b) => a * b).Sum();

                        return xs == ys;
                    }
                }).QuickCheckThrowOnFailure();
            }
        }

        [Test]
        public void LinqLet()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(nums =>
                {
                    using (var _nums = context.CreateGpuArray(nums))
                    {
                        var x =
                            context.Run((from num in _nums.AsGpuQueryExpr()
                                         let a = num * 2
                                         let c = a + 1
                                         let b = a * 2
                                         let e = b - 5
                                         let d = c * c
                                         let m = 3
                                         select a + b + c + d + e + m + num).Sum());

                        var y =
                            (from num in nums
                             let a = num * 2
                             let c = a + 1
                             let b = a * 2
                             let e = b - 5
                             let d = c * c
                             let m = 3
                             select a + b + c + d + e + m + num).Sum();

                        return x == y;
                    }
                }).QuickCheckThrowOnFailure();
                
            }
        }


    }
}
