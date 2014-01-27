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

        let max = 10

//        let q = Query.range(1, max + 1)
//                |> Query.map(fun i -> i, -i)
//                |> Query.map(fun t -> [t])
//                |> Query.run

//OK
//        let z =
//            Query.range(1,max + 1)
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
