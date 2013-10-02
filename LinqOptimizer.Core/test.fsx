

#r @".\bin\Debug\LinqOptimizer.Core.dll"

open LinqOptimizer.Core
open System
open System.Linq
open System.Linq.Expressions

#time

let nums = { 1..100000000 }

nums |> Query.ofSeq
     |> Query.map (fun x -> x * x)
     |> Query.filter (fun x -> x % 2 = 0)
     |> Query.map (fun x -> float x)
     |> Query.sum
     |> Query.run

nums |> Seq.map (fun x -> x * x)
     |> Seq.filter (fun x -> x % 2 = 0)
     |> Seq.map (fun x -> float x)
     |> Seq.sum

nums    .Select(fun x -> x * x)
        .Where(fun x -> x % 2 = 0)
        .Select(fun x -> float x)
        .Sum()

//nums |> Array.map (fun x -> x * x)
//     |> Array.filter (fun x -> x % 2 = 0)
//     |> Array.map (fun x -> float x)
//     |> Array.sum

let s = Query.ofSeq nums
let m = Query.map (fun x -> x * x) s


nums
|> ParallelQuery.ofSeq
|> ParallelQuery.map (fun x -> x + 1)
|> ParallelQuery.run

[1..5]
|> Query.ofSeq
|> Query.collect (fun t -> Seq.init 10 (fun _ -> t))
|> Query.run
|> Seq.toArray
