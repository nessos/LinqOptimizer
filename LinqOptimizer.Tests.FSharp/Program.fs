// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

namespace LinqOptimizer.Tests

open LinqOptimizer.FSharp
open System.Linq
open System.Collections.Generic
open System.Diagnostics

module Program = 

    let time f = 
        let sw = Stopwatch()
        sw.Start()
        let r = f()
        sw.Stop()
        printfn "Result : %A\nElapsed : %A" r sw.Elapsed

    [<EntryPoint>]
    let main argv = 

//        let z =
//            Query.range(1,10)
//            |> Query.map(fun i -> i,i * i)
//            |> Query.map(fun ((a,b) as tt) -> a + snd tt)
//            |> Query.run

        let max = 10
//        let x = 
//            Query.range(1, max + 1)
//            |> Query.map(fun i -> i,i + 1)
//            |> Query.map(fun (a,b) -> a, b, a + b)
//            |> Query.filter(fun (k,l,m) -> true)
//            |> Query.map(fun (x,y,z) -> (x,y,z))
//            |> Query.run

        let x = 
            Query.range(1, max + 1)
            |> Query.map(fun i -> i,i + 1)
            |> Query.filter(fun (a,b) -> true)
            //|> Query.map(fun (x,y) -> (x,y))
            |> Query.map(fun t -> t)
            |> Query.run

        0 // return an integer exit code
//
//        let x = 
//            Query.range(1, max + 1)
//            |> Query.map(fun i -> i, i + 1, i + 2)
//            //|> Query.map(fun (a,b,c) -> (b,c,a))
//            |> Query.filter (fun (a,b,c) -> a * a + b * b = c * c)
//            |> Query.map(fun (a,b,c) -> (b,c,a))
//            |> Query.length
//            |> Query.run


        //            |> Query.collect(fun a ->
//                Enumerable.Range(a, max + 1 - a)
//                |> Seq.collect(fun b ->
//                    Enumerable.Range(b, max + 1 - b)
//                    |> Seq.map (fun c -> a, b, c)))