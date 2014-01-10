// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

namespace LinqOptimizer.Tests

open LinqOptimizer.FSharp
open System.Linq
open System.Collections.Generic

module Program = 

    [<EntryPoint>]
    let main argv = 
        
        let max = 100
        let x =
            Query.range(1, max + 1)
            |> Query.collect(fun a ->
                Enumerable.Range(a, max + 1 - a)
                |> Seq.collect(fun b ->
                    Enumerable.Range(b, max + 1 - b)
                    |> Seq.map (fun c -> a, b, c)))
            |> Query.filter (fun (a,b,c) -> a * a + b * b = c * c)
            |> Query.length
            |> Query.run

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
