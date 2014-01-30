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
                    var x = context.Run(xs.AsGpuQueryExpr().Select(n => n * 2));
                    var y = xs.Select(n => n * 2).ToArray();
                    return x.SequenceEqual(y);
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

                    var x = context.Run(xs.AsGpuQueryExpr()
                                          .Select(n => n * 2)
                                          .Select(n => n + 1));

                    var y = xs
                            .Select(n => n * 2)
                            .Select(n => n + 1);

                    return x.SequenceEqual(y);

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

                    var x = context.Run(from n in xs.AsGpuQueryExpr()
                                        where n % 2 == 0
                                        select n + 1);
                    var y = (from n in xs
                             where n % 2 == 0
                             select n + 1).ToArray();

                    return x.SequenceEqual(y);

                }).QuickCheckThrowOnFailure();
            }
        }


    }
}
