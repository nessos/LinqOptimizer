open System
open System.Linq
open System.Diagnostics
open LinqOptimizer.FSharp
open LinqOptimizer.Base

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
    Console.WriteLine("Validate : {0}", validate(t1, t2));
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


let GroupLinq(size) =
    let rnd = new Random(size);
    Enumerable.Range(1, size)
    |> Seq.map (fun x -> 100. * rnd.NextDouble() - 50.)
    |> Seq.groupBy(fun x -> int x % 10)
    |> Seq.map(fun (_,x) -> Seq.length x)
    |> Seq.toArray

let GroupLinqOpt(size) =
    let rnd = new Random(size);
    Enumerable.Range(1, size)
    |> Query.ofSeq
    |> Query.map (fun x -> 100. * rnd.NextDouble() - 50.)
    |> Query.groupBy(fun x -> int x % 10)
    |> Query.map(fun (_,x) -> Seq.length x)
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
    |> Seq.toArray
                

let PythagoreanTriplesLinqOpt(max) =
    Enumerable.Range(1, max + 1)
    |> Query.ofSeq
    |> Query.collect(fun a ->
        Enumerable.Range(a, max + 1 - a)
        |> Seq.collect(fun b ->
            Enumerable.Range(b, max + 1 - b)
            |> Seq.map (fun c -> a, b, c)))
    |> Query.filter (fun (a,b,c) -> a * a + b * b = c * c)
    |> Query.toArray
    |> Query.run

[<EntryPoint>]
let main argv = 
    let vs = Array.init 50000000 double

    measuref("Sum Linq", (fun () -> SumLinq vs), "Sum Opt", (fun () -> SumLinqOpt vs), fun (x, y) -> x = y)
    
    measuref("Sum Sq Linq", (fun () -> SumSqLinq vs), "Sum Sq Opt", (fun () -> SumSqLinqOpt vs), fun (x, y) -> x = y)
    
    let v2 = vs |> Seq.take 10 |> Seq.toArray
    measuref("Cartesian Linq", (fun () -> CartLinq(vs, v2)), "Cartesian Linq Opt", (fun () -> CartLinqOpt(vs, v2)), fun (x,y) -> x = y)

    let s = 50000000
    measuref("Group Linq", (fun () -> GroupLinq s), "Group Opt", (fun () -> GroupLinqOpt s), fun (x, y) -> x = y)
    
    let n = 1000
    measuref("Pythagorean Linq", (fun () -> PythagoreanTriplesLinq n), "Pythagorean Opt", (fun () -> PythagoreanTriplesLinqOpt n ), fun (x, y) -> x = y)


    0 // return an integer exit code
