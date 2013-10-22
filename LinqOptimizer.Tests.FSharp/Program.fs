// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

namespace LinqOptimizer.Tests

open LinqOptimizer.FSharp

module Program = 
    [<EntryPoint>]
    let main argv = 
        
//        let test = new ``F# Query tests``()
//        let t = test.groupBy()

        
        let xs = 
            [1..10]
            |> Query.ofSeq
            |> Query.collect (fun n -> [1..10] |> Seq.take 2 |> Seq.map (fun i -> i * i) )
            |> Query.run

            
        0 // return an integer exit code
