// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

namespace Nessos.LinqOptimizer.Tests

open Nessos.LinqOptimizer.FSharp
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

        let max = 10

        let x = Query.ofSeq [1..10]
                |> Query.map (fun x -> x*x, (x, -x))
                |> Query.where(fun (a,(b,c)) -> a = b * b && b = -c)
                |> Query.length
                |> Query.run


//        let t = LinqOptimizer.Tests.``F# Query tests``()
//        t.``detuple #1``()
//        t.``detuple #2``()
//        t.``detuple #3``()
//        t.``detuple #4``()
//        t.``detuple #5``()
//        t.``detuple #6``()
//        t.``detuple #7``()
//        t.``detuple #8``()

//        let q = Query.range(1, max + 1)
//                |> Query.groupBy(fun x -> int x / 100)
//                |> Query.sortBy (fun (key, vs) -> key)
//                |> Query.map(fun (key, vs) -> Seq.length vs)
//                |> Query.toArray
//                |> Query.run

        let q = [| 1 .. max + 1 |] |> Seq.map double
                |> PQuery.ofSeq
                |> PQuery.groupBy(fun x -> int x / 100)
                |> PQuery.sortBy (fun (key, vs) -> key)
                |> PQuery.map(fun (key, vs) -> Seq.length vs)
                |> PQuery.toArray
                |> PQuery.run

//        let q = Query.range(1,10)
//                |> Query.map(fun i -> i, i + 1)
//                |> Query.filter(fun (a,b) -> Seq.length [||] = 0)
//                |> Query.run

//        let z =
//            [|0|]
//            |> Query.ofSeq
//            |> Query.map(fun i -> i,i * i)
//            |> Query.map(fun ((a,b) as tt) -> a + snd tt)
//            |> Query.run

// OK
//        let w = 
//            Query.range(1, max + 1)
//            |> Query.map(fun i -> i,i + 1)
//            |> Query.map(fun (a,b) -> a, b, b)
//            |> Query.map(fun (a,b,c) -> a, b, c, c)
//            |> Query.map(fun (a,b,c,d) -> a, b, c,d,d)
//            |> Query.run

// OK
//        let x = 
//            Query.range(1, max + 1)
//            |> Query.map(fun i -> i,-i)
//            |> Query.filter(fun (a,b) -> a = -b)
//            |> Query.map(fun (x,y) -> x,y)
//            |> Query.filter(fun (c,d) -> c = -d)
//            |> Query.length
//            |> Query.run

// OK
//        let x = Query.range(1, max + 1)
//                |> Query.map(fun i -> i,i * i)
//                |> Query.filter(fun ((a,b) as mm) -> a % 2 = 0)
//                |> Query.run

// OK
//        let z = Query.range(1, max + 1)
//                |> Query.map(fun i -> i,i * i)
//                |> Query.filter(fun ((a,b) as mm) -> a % 2 = 0)
//                |> Query.run

        0 // return an integer exit code
