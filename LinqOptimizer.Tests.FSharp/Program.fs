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
        time(fun () ->
              let x = 
                Query.range(1, 100000000) 
                |> Query.map(fun i -> i,i) 
                |> Query.map(fun (a,b) -> a - b)
                |> Query.length
                |> Query.run
              x )

        //let y = Query.range(1,10) |> Query.where(fun m -> m % 2 = 0) |> Query.run

//        let test = new ``F# Query tests``()
//        let t = test.``precompile function``()
//        let a = ResizeArray<int>([1..20])
        
//        let t = Query.compile(fun x -> Query.iter (fun m -> a.Add(m)) (Query.ofSeq x))
//        t([1..10]) 

        //PrecompileHelpers.``fun x -> Query.iter (fun m -> a.Add(m)) (Query.ofSeq x)``

//        let a = ref 42
//
//        let xs = 
//            [1..10]
//            |> Query.ofSeq
//            |> Query.map (fun x -> ())
//            |> Query.run
           
            
        0 // return an integer exit code
