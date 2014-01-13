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
            Enumerable.Range(1, max + 1)
            |> PQuery.ofSeq
            |> PQuery.collect(fun a ->
                Enumerable.Range(a, max + 1 - a)
                |> Seq.collect(fun b ->
                    Enumerable.Range(b, max + 1 - b)
                    |> Seq.map (fun c -> let t = a, b, c in box t :?> System.Tuple<int,int,int>)))
            |> PQuery.filter (fun t -> t.Item1 * t.Item1 + t.Item2 * t.Item2 = t.Item3 * t.Item3)
            |> PQuery.length
            |> PQuery.run

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
