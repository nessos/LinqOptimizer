open System
open System.Linq
open System.Collections
open System.Collections.Generic
open System.Diagnostics
open Nessos.LinqOptimizer.FSharp
open Nessos.LinqOptimizer.Base

let measuref<'T>(title1, action1 : unit -> 'T, title2, action2 : unit -> 'T, validate : 'T * 'T -> bool) =
    let sw = new Stopwatch();
    sw.Start();
    let t1 = action1();
    sw.Stop();
    Console.WriteLine("\"{0}\":\t{1}",title1, sw.Elapsed);
    sw.Restart();
    let t2 = action2();
    sw.Stop();
    Console.WriteLine("\"{0}\":\t{1}", title2, sw.Elapsed);
    let v = validate(t1, t2)
    if not v then
        Console.WriteLine("Values {0}, {1}", t1, t2)
    Console.WriteLine("Validate : {0}", v);
    Console.WriteLine();

let SumLinq(values : double []) = 
    Seq.sum values

let SumLinqOpt(values : double[]) = 
    values |> Query.ofSeq |> Query.sum |> Query.run

let SumSqLinq(values : double[]) =
    values |> Seq.map (fun x -> x * x) |> Seq.sum

let SumSqLinqOpt(values : double[]) =
    values |> Query.ofSeq |> Query.map(fun x -> x * x) |> Query.sum |> Query.run

let CartLinq (dim1 : double[], dim2 : double[]) =
    dim1 |> Seq.collect (fun x -> Seq.map (fun y -> x * y) dim2) |> Seq.sum

let CartLinqOpt(dim1 : double[], dim2 : double[]) =
    dim1 |> Query.ofSeq |> Query.collect (fun x -> Seq.map (fun y -> x * y) dim2) |> Query.sum |> Query.run


let GroupLinq(values : double[]) =
    values
    |> Seq.groupBy(fun x -> int x / 100)
    |> Seq.sortBy (fun (key, vs) -> key)
    |> Seq.map(fun (key, vs) -> Seq.length vs)
    |> Seq.toArray

let GroupLinqOpt(values : double[]) =
    values
    |> Query.ofSeq
    |> Query.groupBy(fun x -> int x / 100)
    |> Query.sortBy (fun (key, vs) -> key)
    |> Query.map(fun (key, vs) -> Seq.length vs)
    |> Query.toArray
    |> Query.run


let PythagoreanTriplesLinq(max) =
    Enumerable.Range(1, max + 1)
    |> Seq.collect(fun a ->
        Enumerable.Range(a, max + 1 - a)
        |> Seq.collect(fun b ->
            Enumerable.Range(b, max + 1 - b)
            |> Seq.map (fun c -> a, b, c)))
    |> Seq.filter (fun (a,b,c) -> a * a + b * b = c * c)
    |> Seq.length
                

let PythagoreanTriplesLinqOpt(max) =
    Query.range(1, max + 1)
    |> Query.collect(fun a ->
        Enumerable.Range(a, max + 1 - a)
        |> Seq.collect(fun b ->
            Enumerable.Range(b, max + 1 - b)
            |> Seq.map (fun c -> a, b, c)))
    |> Query.filter (fun (a,b,c) -> a * a + b * b = c * c)
    |> Query.length
    |> Query.run


//////////////////////////////////////////////////////////////////

let ParallelSumLinq(values : double []) = 
    values.AsParallel().Sum()

let ParallelSumLinqOpt(values : double[]) = 
    values |> PQuery.ofSeq |> PQuery.sum |> PQuery.run

let ParallelSumSqLinq(values : double[]) =
    values.AsParallel().Select(fun x -> x * x).Sum()

let ParallelSumSqLinqOpt(values : double[]) =
    values |> PQuery.ofSeq |> PQuery.map(fun x -> x * x) |> PQuery.sum |> PQuery.run

let ParallelCartLinq (dim1 : double[], dim2 : double[]) =
    dim1.AsParallel().SelectMany(fun x -> dim2.Select(fun y -> x * y)).Sum()

let ParallelCartLinqOpt(dim1 : double[], dim2 : double[]) =
    dim1 |> PQuery.ofSeq |> PQuery.collect (fun x -> Seq.map (fun y -> x * y) dim2) |> PQuery.sum |> PQuery.run

let ParallelGroupLinq(values : double[]) =
    values.AsParallel()
        .GroupBy(fun x -> (int)x / 100)
        .OrderBy(fun (x : IGrouping<_,_>) -> x.Key)
        .Select(fun (k : IGrouping<_,_>) -> k.Count())
        .ToArray()

let ParallelGroupLinqOpt(values : double[]) =
    values
    |> PQuery.ofSeq
    |> PQuery.groupBy(fun x -> int x / 100)
    |> PQuery.sortBy (fun (key, vs) -> key)
    |> PQuery.map(fun (key, vs) -> Seq.length vs)
    |> PQuery.toArray
    |> PQuery.run


let ParallelPythagoreanTriplesLinq(max) =
    Enumerable.Range(1, max + 1).AsParallel()
        .SelectMany(fun a ->
            Enumerable.Range(a, max + 1 - a)
                .SelectMany(fun b ->
                    Enumerable.Range(b, max + 1 - b)
                        .Select(fun c -> (a,b,c))))
        .Where(fun (a,b,c) -> a * a + b * b = c * c)
        .Count()

let ParallelPythagoreanTriplesLinqOpt(max) =
    Enumerable.Range(1, max + 1)
    |> PQuery.ofSeq
    |> PQuery.collect(fun a ->
        Enumerable.Range(a, max + 1 - a)
        |> Seq.collect(fun b ->
            Enumerable.Range(b, max + 1 - b)
            |> Seq.map (fun c -> a, b, c)))
    |> PQuery.filter (fun (a,b,c) -> a * a + b * b = c * c)
    |> PQuery.length
    |> PQuery.run

//////////////////////////////////////////////////////////////////

[<EntryPoint>]
let main argv = 
    let rnd = new Random()
    let v = Enumerable.Range(1, 200000000).Select(fun x -> rnd.NextDouble()).ToArray()
    let cmp (x1, x2 : double) = Math.Abs(x1 - x2) < 1E-07

    measuref("Sum Seq", (fun () -> SumLinq v), 
             "Sum Opt", (fun () -> SumLinqOpt v), 
             cmp)
    
    measuref("Sum Squares Seq", (fun () -> SumSqLinq v), 
             "Sum Squares Opt", (fun () -> SumSqLinqOpt v), 
             cmp)
    
    let v1 = v.Take(v.Length / 10).ToArray()
    let v2 = v.Take(20).ToArray()
    measuref("Cartesian Seq", (fun () -> CartLinq(v1, v2)), 
             "Cartesian Linq Opt", (fun () -> CartLinqOpt(v1, v2)), 
             cmp)

    let g = Enumerable.Range(1, 20000000).Select(fun x -> 100000000. * rnd.NextDouble() - 50000000.).ToArray()
    measuref("Group Seq", (fun () -> GroupLinq g), 
             "Group Opt", (fun () -> GroupLinqOpt g), 
             fun (x, y) -> Enumerable.SequenceEqual(x, y))
    
    let n = 1000
    measuref("Pythagorean Seq", (fun () -> PythagoreanTriplesLinq n), 
             "Pythagorean Opt", (fun () -> PythagoreanTriplesLinqOpt n ), 
             fun (x, y) -> x = y)

    ////////////////////////////////////////////////////////////////////////////

    measuref("Parallel Sum Seq", (fun () -> ParallelSumLinq v), 
             "Parallel Sum Opt", (fun () -> ParallelSumLinqOpt v), 
             cmp)
    
    measuref("Parallel Sum Squares Seq", (fun () -> ParallelSumSqLinq v), 
             "Parallel Sum Squares Opt", (fun () -> ParallelSumSqLinqOpt v), 
             cmp)
    
    measuref("Parallel Cartesian Linq", (fun () -> ParallelCartLinq(v1, v2)), 
             "Parallel Cartesian Opt", (fun () ->  ParallelCartLinqOpt(v1, v2)), 
             cmp)

    measuref("Parallel Group Linq", (fun () -> ParallelGroupLinq g), 
             "Parallel Group Opt", (fun () ->  ParallelGroupLinqOpt g), 
             fun (x, y) -> Enumerable.SequenceEqual(x, y))
    
    measuref("Parallel Pythagorean Linq", (fun () -> ParallelPythagoreanTriplesLinq n), 
             "Parallel Pythagorean Opt", (fun () ->  ParallelPythagoreanTriplesLinqOpt n ), 
             fun (x, y) -> x = y)

    0 
