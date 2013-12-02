open System
open System.Diagnostics
open LinqOptimizer.FSharp
open LinqOptimizer.Base

let measure<'T>(title1, action1, title2, action2, validate) =
    let sw = new Stopwatch();
    sw.Start();
    let t1 = action1();
    sw.Stop();
    Console.WriteLine("\"{0}\":\t{1}",title1, sw.Elapsed);
    sw.Restart();
    let t2 = action2();
    sw.Stop();
    Console.WriteLine("\"{0}\":\t{1}", title2, sw.Elapsed);
    Console.WriteLine("Validate : {0}", validate(t1, t2));
    Console.WriteLine();

let SumLinq(values : double []) = 
    Seq.sum values

let SumLinqOpt(values : double[]) = 
    values |> Query.ofSeq |> Query.sum |> Query.run

let SumSqLinq(values : double[]) =
    values |> Seq.map (fun x -> x * x) |> Seq.sum

//let SumSqLinqOpt(values : double[])
//    return values.AsQueryExpr().Select(x => x * x).Sum().Run();

//static double CartLinq(double[] dim1, double[] dim2)
//{
//    return (from x in dim1
//            from y in dim2
//            select x * y).Sum();
//}
//
//static double CartLinqOpt(double[] dim1, double[] dim2)
//{
//    return (from x in dim1.AsQueryExpr()
//            from y in dim2
//            select x * y).Sum().Run();
//}
//
//static int[] GroupLinq(int size)
//{
//    var rnd = new Random(size);
//    return Enumerable.Range(1, size)
//            .Select(x => 100 * rnd.NextDouble() - 50)
//            .GroupBy(x => (int)x % 10)
//            .Select(x => x.Count())
//            .ToArray();
//}
//
//static int[] GroupLinqOpt(int size)
//{
//    var rnd = new Random(size);
//    return Enumerable.Range(1, size).AsQueryExpr()
//            .Select(x => 100 * rnd.NextDouble() - 50)
//            .GroupBy(x => (int)x % 10)
//            .Select(x => x.Count())
//            .ToArray()
//            .Run();
//}
//
//static Tuple<int, int, int>[] PythagoreanTriplesLinq(int max)
//{
//    return (from a in Enumerable.Range(1, max + 1)
//            from b in Enumerable.Range(a, max + 1 - a)
//            from c in Enumerable.Range(b, max + 1 - b)
//            where a * a + b * b == c * c
//            select Tuple.Create(a, b, c)).ToArray();
//}
//
//static Tuple<int, int, int>[] PythagoreanTriplesLinqOpt(int max)
//{
//    return (from a in Enumerable.Range(1, max + 1).AsQueryExpr()
//            from b in Enumerable.Range(a, max + 1 - a)
//            from c in Enumerable.Range(b, max + 1 - b)
//            where a * a + b * b == c * c
//            select Tuple.Create(a, b, c)).ToArray().Run();
//}

[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    0 // return an integer exit code
