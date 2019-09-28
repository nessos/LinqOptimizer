using BenchmarkDotNet.Attributes;
using Nessos.LinqOptimizer.CSharp;
using Cistern.Linq;

namespace LinqOptimizer.Benchmarks.CSharp
{
    public partial class SequentialBenchmarks
    {
        /*
        |        Method |   Count |             Mean |          Error |         StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |-------------- |-------- |-----------------:|---------------:|---------------:|---------:|--------:|-------:|------:|------:|----------:|
        | 'Sum Cistern' |       0 |        16.852 ns |      0.1372 ns |      0.1216 ns |     2.09 |    0.02 | 0.0102 |     - |     - |      32 B |
        |    'Sum Linq' |       0 |         8.081 ns |      0.0280 ns |      0.0248 ns |     1.00 |    0.00 |      - |     - |     - |         - |
        |     'Sum Opt' |       0 |    26,887.418 ns |     91.3808 ns |     76.3071 ns | 3,327.48 |   14.58 | 5.1270 |     - |     - |   16229 B |
        |               |         |                  |                |                |          |         |        |       |       |           |
        | 'Sum Cistern' |      10 |        27.974 ns |      0.2326 ns |      0.2176 ns |     0.46 |    0.00 | 0.0102 |     - |     - |      32 B |
        |    'Sum Linq' |      10 |        60.580 ns |      0.2447 ns |      0.2289 ns |     1.00 |    0.00 | 0.0101 |     - |     - |      32 B |
        |     'Sum Opt' |      10 |    27,413.368 ns |     93.3849 ns |     82.7833 ns |   452.62 |    1.59 | 5.1270 |     - |     - |   16229 B |
        |               |         |                  |                |                |          |         |        |       |       |           |
        | 'Sum Cistern' |     100 |       133.831 ns |      0.2936 ns |      0.2603 ns |     0.27 |    0.00 | 0.0100 |     - |     - |      32 B |
        |    'Sum Linq' |     100 |       493.470 ns |      1.0039 ns |      0.9390 ns |     1.00 |    0.00 | 0.0095 |     - |     - |      32 B |
        |     'Sum Opt' |     100 |    27,309.393 ns |    107.6157 ns |    100.6638 ns |    55.34 |    0.23 | 5.1270 |     - |     - |   16229 B |
        |               |         |                  |                |                |          |         |        |       |       |           |
        | 'Sum Cistern' |   10000 |    11,721.675 ns |     25.7087 ns |     21.4679 ns |     0.27 |    0.00 |      - |     - |     - |      32 B |
        |    'Sum Linq' |   10000 |    43,967.113 ns |    113.4651 ns |    106.1353 ns |     1.00 |    0.00 |      - |     - |     - |      32 B |
        |     'Sum Opt' |   10000 |    39,896.225 ns |    135.1150 ns |    119.7759 ns |     0.91 |    0.00 | 5.0659 |     - |     - |   16229 B |
        |               |         |                  |                |                |          |         |        |       |       |           |
        | 'Sum Cistern' | 1000000 | 1,172,511.029 ns |  3,504.5527 ns |  3,278.1608 ns |     0.27 |    0.00 |      - |     - |     - |      32 B |
        |    'Sum Linq' | 1000000 | 4,403,267.500 ns | 16,557.6598 ns | 15,488.0453 ns |     1.00 |    0.00 |      - |     - |     - |      32 B |
        |     'Sum Opt' | 1000000 | 1,207,429.492 ns |  2,669.2049 ns |  2,496.7759 ns |     0.27 |    0.00 | 1.9531 |     - |     - |   16203 B |
        */
        public partial class SumBechmarks : BenchmarkBase
        {
            [Benchmark(Description = "Sum Cistern")]
            public double SumCistern()
            {
                return values.Sum();
            }
        }

        /*
        |                   Method |   Count |            Mean |          Error |         StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |------------------------- |-------- |----------------:|---------------:|---------------:|---------:|--------:|-------:|------:|------:|----------:|
        | 'Sum of Squares Cistern' |       0 |        24.68 ns |      0.0478 ns |      0.0447 ns |     0.91 |    0.00 | 0.0102 |     - |     - |      32 B |
        |    'Sum of Squares Linq' |       0 |        27.05 ns |      0.1136 ns |      0.1062 ns |     1.00 |    0.00 |      - |     - |     - |         - |
        |     'Sum of Squares Opt' |       0 |    34,392.44 ns |    143.4999 ns |    127.2089 ns | 1,271.72 |    6.29 | 5.7983 |     - |     - |   18411 B |
        |                          |         |                 |                |                |          |         |        |       |       |           |
        | 'Sum of Squares Cistern' |      10 |        78.52 ns |      0.2665 ns |      0.2493 ns |     0.62 |    0.00 | 0.0280 |     - |     - |      88 B |
        |    'Sum of Squares Linq' |      10 |       125.78 ns |      0.3848 ns |      0.3411 ns |     1.00 |    0.00 | 0.0153 |     - |     - |      48 B |
        |     'Sum of Squares Opt' |      10 |    34,251.23 ns |    155.5150 ns |    145.4688 ns |   272.37 |    1.46 | 5.7983 |     - |     - |   18412 B |
        |                          |         |                 |                |                |          |         |        |       |       |           |
        | 'Sum of Squares Cistern' |     100 |       293.53 ns |      0.8549 ns |      0.7996 ns |     0.45 |    0.00 | 0.0277 |     - |     - |      88 B |
        |    'Sum of Squares Linq' |     100 |       652.57 ns |      1.5209 ns |      1.3482 ns |     1.00 |    0.00 | 0.0153 |     - |     - |      48 B |
        |     'Sum of Squares Opt' |     100 |    34,430.53 ns |    144.0165 ns |    134.7132 ns |    52.77 |    0.23 | 5.7983 |     - |     - |   18412 B |
        |                          |         |                 |                |                |          |         |        |       |       |           |
        | 'Sum of Squares Cistern' |   10000 |    23,535.67 ns |     48.7263 ns |     45.5786 ns |     0.40 |    0.00 |      - |     - |     - |      88 B |
        |    'Sum of Squares Linq' |   10000 |    59,171.18 ns |    156.9741 ns |    139.1534 ns |     1.00 |    0.00 |      - |     - |     - |      48 B |
        |     'Sum of Squares Opt' |   10000 |    46,917.77 ns |    251.1217 ns |    234.8994 ns |     0.79 |    0.00 | 5.7373 |     - |     - |   18412 B |
        |                          |         |                 |                |                |          |         |        |       |       |           |
        | 'Sum of Squares Cistern' | 1000000 | 2,347,908.31 ns |  6,746.4530 ns |  5,980.5549 ns |     0.34 |    0.00 |      - |     - |     - |      88 B |
        |    'Sum of Squares Linq' | 1000000 | 6,868,664.30 ns | 18,140.2511 ns | 15,147.9312 ns |     1.00 |    0.00 |      - |     - |     - |      48 B |
        |     'Sum of Squares Opt' | 1000000 | 1,217,121.11 ns |  4,969.3813 ns |  4,648.3623 ns |     0.18 |    0.00 | 5.8594 |     - |     - |   18390 B |
        */
        public partial class SumOfSquaresBechmarks : BenchmarkBase
        {
            [Benchmark(Description = "Sum of Squares Cistern")]
            public double SumSqCistern()
            {
                return values.Select(x => x * x).Sum();
            }
        }

        /*
        |              Method |   Count |             Mean |          Error |         StdDev |    Ratio | RatioSD |     Gen 0 | Gen 1 | Gen 2 | Allocated |
        |-------------------- |-------- |-----------------:|---------------:|---------------:|---------:|--------:|----------:|------:|------:|----------:|
        | 'Cartesian Cistern' |       0 |         90.01 ns |      0.8249 ns |      0.7313 ns |     1.40 |    0.01 |    0.0688 |     - |     - |     216 B |
        |    'Cartesian Linq' |       0 |         64.42 ns |      0.4785 ns |      0.4476 ns |     1.00 |    0.00 |    0.0535 |     - |     - |     168 B |
        |     'Cartesian Opt' |       0 |     65,658.98 ns |    322.1956 ns |    301.3820 ns | 1,019.30 |    8.06 |   10.8643 |     - |     - |   34474 B |
        |                     |         |                  |                |                |          |         |           |       |       |           |
        | 'Cartesian Cistern' |      10 |        167.74 ns |      0.5966 ns |      0.5581 ns |     0.74 |    0.00 |    0.0918 |     - |     - |     288 B |
        |    'Cartesian Linq' |      10 |        226.30 ns |      0.4422 ns |      0.3920 ns |     1.00 |    0.00 |    0.0739 |     - |     - |     232 B |
        |     'Cartesian Opt' |      10 |     65,134.34 ns |    265.7659 ns |    235.5945 ns |   287.82 |    1.24 |   10.9863 |     - |     - |   34474 B |
        |                     |         |                  |                |                |          |         |           |       |       |           |
        | 'Cartesian Cistern' |     100 |        862.19 ns |      2.1934 ns |      2.0517 ns |     0.31 |    0.00 |    0.0916 |     - |     - |     288 B |
        |    'Cartesian Linq' |     100 |      2,770.59 ns |      6.3523 ns |      5.6311 ns |     1.00 |    0.00 |    0.1640 |     - |     - |     520 B |
        |     'Cartesian Opt' |     100 |     67,920.13 ns |    213.4488 ns |    199.6601 ns |    24.51 |    0.07 |   10.8643 |     - |     - |   34474 B |
        |                     |         |                  |                |                |          |         |           |       |       |           |
        | 'Cartesian Cistern' |   10000 |     73,755.52 ns |    173.7575 ns |    145.0953 ns |     0.28 |    0.00 |         - |     - |     - |     288 B |
        |    'Cartesian Linq' |   10000 |    265,771.58 ns |    706.6751 ns |    661.0243 ns |     1.00 |    0.00 |   10.2539 |     - |     - |   32200 B |
        |     'Cartesian Opt' |   10000 |     89,407.86 ns |    251.5113 ns |    222.9582 ns |     0.34 |    0.00 |   10.8643 |     - |     - |   34482 B |
        |                     |         |                  |                |                |          |         |           |       |       |           |
        | 'Cartesian Cistern' | 1000000 |  7,310,161.36 ns | 19,554.9373 ns | 16,329.2583 ns |     0.27 |    0.00 |         - |     - |     - |     288 B |
        |    'Cartesian Linq' | 1000000 | 26,870,184.17 ns | 45,236.8110 ns | 42,314.5411 ns |     1.00 |    0.00 | 1000.0000 |     - |     - | 3200200 B |
        |     'Cartesian Opt' | 1000000 |  2,418,785.44 ns | 11,774.2764 ns | 11,013.6655 ns |     0.09 |    0.00 |    3.9063 |     - |     - |   34432 B |
        */
        public partial class CartesianBenchmarks : BenchmarkBase
        {
            [Benchmark(Description = "Cartesian Cistern")]
            public double CartCistern()
            {
                return (from x in dim1
                        from y in dim2
                        select x * y).Sum();
            }
        }

        /*
        |             Method |   Count |             Mean |             Error |            StdDev |    Ratio | RatioSD |      Gen 0 |     Gen 1 |     Gen 2 |  Allocated |
        |------------------- |-------- |-----------------:|------------------:|------------------:|---------:|--------:|-----------:|----------:|----------:|-----------:|
        | 'Group By Cistern' |       0 |         314.9 ns |         1.2644 ns |         1.1827 ns |     1.74 |    0.01 |     0.1988 |         - |         - |      624 B |
        |    'Group By Linq' |       0 |         181.3 ns |         0.4558 ns |         0.3806 ns |     1.00 |    0.00 |     0.1173 |         - |         - |      368 B |
        |     'Group By Opt' |       0 |     236,575.6 ns |       827.2355 ns |       773.7966 ns | 1,305.04 |    4.09 |    39.0625 |         - |         - |   122633 B |
        |                    |         |                  |                   |                   |          |         |            |           |           |            |
        | 'Group By Cistern' |      10 |       2,323.8 ns |         6.1583 ns |         5.4592 ns |     1.25 |    0.00 |     0.7858 |         - |         - |     2472 B |
        |    'Group By Linq' |      10 |       1,856.2 ns |         4.1372 ns |         3.6675 ns |     1.00 |    0.00 |     0.6542 |         - |         - |     2056 B |
        |     'Group By Opt' |      10 |     248,587.5 ns |     1,191.5435 ns |     1,114.5706 ns |   133.98 |    0.75 |    45.8984 |         - |         - |   145691 B |
        |                    |         |                  |                   |                   |          |         |            |           |           |            |
        | 'Group By Cistern' |     100 |      20,043.0 ns |        74.5295 ns |        69.7150 ns |     1.06 |    0.01 |     4.7607 |         - |         - |    14976 B |
        |    'Group By Linq' |     100 |      18,823.5 ns |        86.2172 ns |        80.6476 ns |     1.00 |    0.00 |     4.4861 |         - |         - |    14192 B |
        |     'Group By Opt' |     100 |     280,661.8 ns |       754.1135 ns |       705.3982 ns |    14.91 |    0.07 |    52.2461 |         - |         - |   166780 B |
        |                    |         |                  |                   |                   |          |         |            |           |           |            |
        | 'Group By Cistern' |   10000 |   4,274,076.9 ns |    17,038.5176 ns |    15,937.8399 ns |     0.93 |    0.01 |   234.3750 |  148.4375 |   78.1250 |  1235128 B |
        |    'Group By Linq' |   10000 |   4,600,234.9 ns |    54,230.6699 ns |    50,727.4023 ns |     1.00 |    0.00 |   218.7500 |  101.5625 |   39.0625 |  1274839 B |
        |     'Group By Opt' |   10000 |   4,851,653.2 ns |    20,640.9490 ns |    19,307.5565 ns |     1.05 |    0.01 |   398.4375 |  296.8750 |  296.8750 |  2033237 B |
        |                    |         |                  |                   |                   |          |         |            |           |           |            |
        | 'Group By Cistern' | 1000000 | 597,903,566.7 ns | 9,163,144.9560 ns | 8,571,211.4771 ns |     0.87 |    0.02 | 11000.0000 | 4000.0000 | 1000.0000 | 63248008 B |
        |    'Group By Linq' | 1000000 | 687,909,520.0 ns | 8,845,291.9188 ns | 8,273,891.5488 ns |     1.00 |    0.00 | 13000.0000 | 6000.0000 | 2000.0000 | 71289272 B |
        |     'Group By Opt' | 1000000 | 666,151,733.3 ns | 7,902,040.9008 ns | 7,391,573.9614 ns |     0.97 |    0.02 | 17000.0000 | 7000.0000 | 3000.0000 | 61443144 B |
        */
        public partial class GroupByBenchmarks : BenchmarkBase
        {
            [Benchmark(Description = "Group By Cistern")]
            public int[] GroupCistern()
            {
                return values
                    .GroupBy(x => (int)x / 100)
                    .OrderBy(x => x.Key)
                    .Select(k => k.Count())
                    .ToArray();
            }
        }

        /*
        |                        Method |  max |               Mean |            Error |            StdDev |  Ratio | RatioSD |        Gen 0 | Gen 1 | Gen 2 |    Allocated |
        |------------------------------ |----- |-------------------:|-----------------:|------------------:|-------:|--------:|-------------:|------:|------:|-------------:|
        | 'Pythagorean Triples Cistern' |    0 |           465.8 ns |         1.901 ns |         1.6856 ns |   1.83 |    0.01 |       0.2470 |     - |     - |        776 B |
        |    'Pythagorean Triples Linq' |    0 |           254.1 ns |         1.019 ns |         0.9533 ns |   1.00 |    0.00 |       0.1578 |     - |     - |        496 B |
        |     'Pythagorean Triples Opt' |    0 |       225,031.5 ns |       805.354 ns |       753.3290 ns | 885.73 |    4.97 |      30.7617 |     - |     - |      96902 B |
        |                               |      |                    |                  |                   |        |         |              |       |       |              |
        | 'Pythagorean Triples Cistern' |   10 |         7,425.1 ns |        38.786 ns |        32.3877 ns |   0.77 |    0.00 |       3.5858 |     - |     - |      11264 B |
        |    'Pythagorean Triples Linq' |   10 |         9,623.9 ns |        32.828 ns |        30.7077 ns |   1.00 |    0.00 |       3.6469 |     - |     - |      11456 B |
        |     'Pythagorean Triples Opt' |   10 |       225,238.5 ns |       884.302 ns |       783.9105 ns |  23.40 |    0.12 |      30.7617 |     - |     - |      96966 B |
        |                               |      |                    |                  |                   |        |         |              |       |       |              |
        | 'Pythagorean Triples Cistern' |  100 |     2,615,738.7 ns |     7,146.229 ns |     6,684.5870 ns |   0.59 |    0.00 |    1839.8438 |     - |     - |    5781224 B |
        |    'Pythagorean Triples Linq' |  100 |     4,462,903.4 ns |    14,469.345 ns |    13,534.6342 ns |   1.00 |    0.00 |    1851.5625 |     - |     - |    5822096 B |
        |     'Pythagorean Triples Opt' |  100 |       412,640.2 ns |     1,139.718 ns |     1,010.3302 ns |   0.09 |    0.00 |      30.2734 |     - |     - |      96910 B |
        |                               |      |                    |                  |                   |        |         |              |       |       |              |
        | 'Pythagorean Triples Cistern' | 1000 | 2,169,828,593.3 ns | 8,159,438.780 ns | 7,632,344.0976 ns |   0.55 |    0.00 | 1714000.0000 |     - |     - | 5377404824 B |
        |    'Pythagorean Triples Linq' | 1000 | 3,979,017,007.1 ns | 5,244,819.772 ns | 4,649,396.1160 ns |   1.00 |    0.00 | 1715000.0000 |     - |     - | 5381416496 B |
        |     'Pythagorean Triples Opt' | 1000 |   149,938,446.4 ns |   305,989.109 ns |   271,251.3750 ns |   0.04 |    0.00 |            - |     - |     - |      97446 B |
        */
        public partial class PythagoreanTriplesBenchmarks
        {
            [Benchmark(Description = "Pythagorean Triples Cistern")]
            public int PythagoreanTriplesCistern()
            {
                return (from a in Enumerable.Range(1, max + 1)
                        from b in Enumerable.Range(a, max + 1 - a)
                        from c in Enumerable.Range(b, max + 1 - b)
                        where a * a + b * b == c * c
                        select true).Count();
            }
        }
    }
}
