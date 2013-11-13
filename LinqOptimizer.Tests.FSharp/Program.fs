// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

namespace LinqOptimizer.Tests

open LinqOptimizer.FSharp

module Program = 

    [<EntryPoint>]
    let main argv = 
        
//        let test = new ``F# Parallel Query tests``()
//        let t = test.toArray()
        let xs = [true; false]

        let x = xs |> PQuery.ofSeq 
                   |> PQuery.toArray
                   |> PQuery.run

//        let a = ref 42
//
//        let xs = 
//            [1..10]
//            |> Query.ofSeq
//            |> Query.map (fun x -> ())
//            |> Query.run
           
            
        0 // return an integer exit code
