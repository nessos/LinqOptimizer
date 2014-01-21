using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqOptimizer.Gpu.CSharp;
using FsCheck.Fluent;

namespace LinqOptimizer.Tests
{
    [TestFixture]
    class GpuQueryTests
    {
        [Test]
        public void Select()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsGpuQueryExpr().Select(n => n * 2).Run();
                var y = xs.Select(n => n * 2).ToArray();
                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }


        [Test]
        public void Pipelined()
        {
            Spec.ForAny<int[]>(xs =>
            {
                var x = xs.AsGpuQueryExpr()
                        .Select(n => n * 2)
                        .Select(n => n + 1)
                        .Run();

                var y = xs
                        .Select(n => n * 2)
                        .Select(n => n + 1);

                return x.SequenceEqual(y);
            }).QuickCheckThrowOnFailure();
        }
    }
}
