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

        let m = Query.range(1, 1000) 
                |> Query.groupBy (fun x -> string x)
                |> Query.map (fun (_,x) -> Seq.sum x)
                |> Query.run

//        let m = Query.range(1,10)
//                |> Query.map (fun i -> i, i + 1)
//                |> Query.map (fun (a,b) -> a, a * a, b * b)
//                |> Query.map (fun (x,y,z) -> x + y + z)
//                |> Query.run

//        let p = Query.range(1,10)
//                |> Query.map (fun i -> i, i + 1)
//                |> Query.map (fun (_,b) -> b, b, b * b)
//                |> Query.map (fun (x,_,z) -> x + z)
//                |> Query.run
//
//        let x = 
//            Query.range(1, 1000) 
//            |> Query.map(fun i -> i, i * i) 
//            |> Query.take 10
//            |> Query.map(fun (a,b) -> a - b)
//            |> Query.run
//
//        let y =
//            Query.range(1,10)
//            |> Query.map(fun i -> i,i * i)
//            |> Query.map(fun t -> snd t)
//            |> Query.run

//        let z =
//            Query.range(1,10)
//            |> Query.map(fun i -> i,i * i)
//            |> Query.map(fun ((a,b) as tt) -> a + snd tt)
//            |> Query.run

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
