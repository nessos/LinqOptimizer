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

        let z =
            Query.range(1,10)
            |> Query.map(fun i -> i,i * i)
            |> Query.map(fun ((a,b) as tt) -> a + snd tt)
            |> Query.run

        0 // return an integer exit code
