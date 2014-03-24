using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqOptimizer.CSharp;
using LinqOptimizer.Gpu.CSharp;
using LinqOptimizer.Base;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.IO;
using LinqOptimizer.Gpu;

namespace LinqOptimizer.Tests
{


    public class Program
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Complex
        {
            public double A;
            public double B;
        }

        private static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }
        public static void Main(string[] args)
        {
            int fftSize = 2;
            int size = 8388608;

            var xs = Enumerable.Range(1, size).Select(x => x - 1).ToArray();
            var random = new Random();
            var input = Enumerable.Range(1, size).Select(x => new Complex { A = random.NextDouble(), B = 0.0 }).ToArray();
            var output = Enumerable.Range(1, size).Select(x => new Complex { A = 0.0, B = 0.0 }).ToArray();
            using (var context = new GpuContext())
            {

                using (var _xs = context.CreateGpuArray(xs))
                {
                    var _input = context.CreateGpuArray(input);
                    var _output = context.CreateGpuArray(output);
                    Thread.Sleep(5000);



                    Measure(() =>
                        {
                            for (int i = 0; i < System.Math.Log(size, 2.0); i++)
                            {
                                var query = (from x in _xs.AsGpuQueryExpr()
                                             let b = (((int)System.Math.Floor((double)x / fftSize)) * (fftSize / 2))
                                             let offset = x % (fftSize / 2)
                                             let x0 = b + offset
                                             let x1 = x0 + size / 2
                                             let val0 = _input[x0]
                                             let val1 = _input[x1]
                                             let angle = -2 * System.Math.PI * (x / fftSize)
                                             let t = new Complex { A = System.Math.Cos(angle), B = System.Math.Sin(angle) }
                                             select new Complex
                                             {
                                                 A = val0.A + t.A * val1.A - t.B * val1.B,
                                                 B = val0.B + t.B * val1.A + t.A * val1.B
                                             });
                                fftSize *= 2;
                                context.Fill(query, _output);
                                Swap(ref _input, ref _output);
                            }
                        });
                }
            }
            Measure(() =>
                {
                    fftSize = 2;
                    for (int i = 0; i < System.Math.Log(size, 2.0); i++)
                    {
                        output =
                            (from x in xs
                             let b = (((int)System.Math.Floor((double)x / fftSize)) * (fftSize / 2))
                             let offset = x % (fftSize / 2)
                             let x0 = b + offset
                             let x1 = x0 + size / 2
                             let val0 = input[x0]
                             let val1 = input[x1]
                             let angle = -2 * System.Math.PI * (x / fftSize)
                             let t = new Complex { A = System.Math.Cos(angle), B = System.Math.Sin(angle) }
                             select new Complex
                             {
                                 A = val0.A + t.A * val1.A - t.B * val1.B,
                                 B = val0.B + t.B * val1.A + t.A * val1.B
                             }).ToArray();

                        fftSize *= 2;
                        Swap(ref input, ref output);
                    }
                });

                        //var check = gpuResult.Zip(cpuResult, (x, y) => System.Math.Abs(x.A - y.A) < 0.001f).ToArray();
                    


            //(new GpuQueryTests()).FFT();
        }

        static void Measure(Action action)
        {
            Measure(action, 1);
        }

        static void Measure(Action action, int iterations)
        {
            var watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                action();
            }
            Console.WriteLine(new TimeSpan(watch.Elapsed.Ticks / iterations));
        }
    }
}
