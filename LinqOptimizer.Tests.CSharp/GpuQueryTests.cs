using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Gpu;
using LinqOptimizer.Gpu.CSharp;
using FsCheck.Fluent;
using System.Threading;
using System.Runtime.InteropServices;

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

        #region Struct Tests
        [StructLayout(LayoutKind.Sequential)]
        struct Node
        {
            public int x;
            public int y;
        }

        [Test]
        public void Structs()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(nums =>
                {
                    var nodes = nums.Select(num => new Node { x = num, y = num }).ToArray();
                    using (var _nodes = context.CreateGpuArray(nodes))
                    {
                        var xs = context.Run((from node in _nodes.AsGpuQueryExpr()
                                             let x = node.x + 2
                                             let y = node.y + 1
                                             select new Node { x = x, y = y }).ToArray());
                        var ys = (from node in nodes
                                  let x = node.x + 2
                                  let y = node.y + 1
                                  select new Node { x = x, y = y }).ToArray();

                        return xs.SequenceEqual(ys);
                    }
                }).QuickCheckThrowOnFailure();
            }
        }
        #endregion

        [Test]
        public void ConstantLifting()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(nums =>
                {
                    using (var _nums = context.CreateGpuArray(nums))
                    {
                        int c = nums.Length;
                        var xs = context.Run((from num in _nums.AsGpuQueryExpr()
                                             let y = num + c
                                             let k = y + c
                                             select c + y + k).ToArray());

                        var ys = (from num in nums
                                  let y = num + c
                                  let k = y + c
                                  select c + y + k).ToArray();
                        return xs.SequenceEqual(ys);
                    }
                }).QuickCheckThrowOnFailure();
            }
        }


        [Test]
        public void GpuArrayIndexer()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int>(n =>
                {
                    if (n < 0) n = 0;
                    var nums = Enumerable.Range(0, n).ToArray();
                    using (var _nums = context.CreateGpuArray(nums))
                    {
                        using (var __nums = context.CreateGpuArray(nums))
                        {
                            int length = __nums.Length;
                            var xs = context.Run((from num in _nums.AsGpuQueryExpr()
                                                  let y = __nums[num % length]
                                                  select num + y).ToArray());

                            var ys = (from num in nums
                                      let y = nums[num % length]
                                      select num + y).ToArray();

                            return xs.SequenceEqual(ys);
                        }
                    }
                }).QuickCheckThrowOnFailure();
            }
        }

        [Test]
        public void MathFunctions()
        {
            using (var context = new GpuContext())
            {
                Spec.ForAny<int[]>(xs =>
                {
                    using (var _xs = context.CreateGpuArray(xs))
                    {

                        var gpuResult = context.Run((from n in _xs.AsGpuQueryExpr()
                                                     let pi = Math.PI
                                                     let c = Math.Cos(n)
                                                     let s = Math.Sin(n)
                                                     let f = Math.Floor(pi)
                                                     let sq = Math.Sqrt(n * n)
                                                     let ex = Math.Exp(pi)
                                                     let p = Math.Pow(pi, 2)
                                                     select f * pi * c * s * sq * ex * p).ToArray());

                        var cpuResult = (from n in xs
                                         let pi = Math.PI
                                         let c = Math.Cos(n)
                                         let s = Math.Sin(n)
                                         let f = Math.Floor(pi)
                                         let sq = Math.Sqrt(n * n)
                                         let ex = Math.Exp(pi)
                                         let p = Math.Pow(pi, 2)
                                         select f * pi * c * s * sq * ex * p).ToArray();

                        return gpuResult.Zip(cpuResult, (x, y) => System.Math.Abs(x - y) < 0.001f)
                                        .SequenceEqual(Enumerable.Range(1, xs.Length).Select(_ => true));
                    }
                }).QuickCheckThrowOnFailure();
            }
        }

    }
}
