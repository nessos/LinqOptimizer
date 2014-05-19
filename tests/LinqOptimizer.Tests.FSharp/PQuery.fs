namespace Nessos.LinqOptimizer.Tests

    open System
    open System.Collections.Generic
    open System.Linq
    open System.Text
    open FsCheck
    open NUnit.Framework
    open Nessos.LinqOptimizer.FSharp


    [<TestFixture>]
    type ``F# Parallel Query tests`` () =
    
        let equal x y = Enumerable.SequenceEqual(x,y)

        [<Test>]
        member __.``map`` () =
            let test (xs : seq<int>) =
                let x = xs |> PQuery.ofSeq |> PQuery.map (fun n -> 2 * n) |> PQuery.run
                let y = xs |> Seq.map (fun n -> 2 * n)
                Seq.sum x = Seq.sum y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``filter`` () =
            let test (xs : seq<int>) =
                let x = xs |> PQuery.ofSeq |> PQuery.filter (fun n -> n % 2 = 0) |> PQuery.run
                let y = xs |> Seq.filter (fun n -> n % 2 = 0)
                Seq.sum x = Seq.sum y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``pipelined`` () =
            let test (xs : seq<int>) =
                let x = xs |> PQuery.ofSeq 
                           |> PQuery.filter (fun n -> n % 2 = 0) 
                           |> PQuery.map (fun n -> n * 2)
                           |> PQuery.run

                let y = xs |> Seq.filter (fun n -> n % 2 = 0) 
                           |> Seq.map (fun n -> n * 2)
                Seq.sum x = Seq.sum y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``sum int`` () =
            let test (xs : seq<int>) =
                let x = xs |> PQuery.ofSeq |> PQuery.map (fun x -> x * x) |> PQuery.sum |> PQuery.run
                let y = xs |> Seq.map (fun x -> x * x) |> Seq.sum
                x = y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)

        [<Test>]
        member __.``sum double`` () =
            let test (xs : seq<float>) =
                let x = xs |> PQuery.ofSeq |> PQuery.map (fun x -> x * x) |> PQuery.sum |> PQuery.run
                let y = xs |> Seq.map (fun x -> x * x) |> Seq.sum
                (Double.IsNaN x && Double.IsNaN y) || Math.Ceiling(x) = Math.Ceiling(y)
            Check.QuickThrowOnFailure (TestInput.RunTest<float> test)

        [<Test>]
        member __.``sum int64`` () =
            let test (xs : seq<int64>) =
                let x = xs |> PQuery.ofSeq |> PQuery.map (fun x -> x * x) |> PQuery.sum |> PQuery.run
                let y = xs |> Seq.map (fun x -> x * x) |> Seq.sum
                x = y
            Check.QuickThrowOnFailure (TestInput.RunTest<int64> test)

        [<Test>]
        member __.``collect`` () =
            let test (xs : seq<'T>) =
                let x = xs |> PQuery.ofSeq 
                           |> PQuery.collect (fun n -> Seq.map (fun n' -> n' * n) xs)
                           |> PQuery.run
                let y = xs |> Seq.collect (fun n -> Seq.map (fun n' -> n' * n) xs )
                Seq.sum x = Seq.sum y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)
            Check.QuickThrowOnFailure (TestInput.RunTest<float> test)

        [<Test>]
        member __.``collect (nested pipe)`` () =
            let test (xs : seq<'T>) =
                let x = xs |> PQuery.ofSeq 
                           |> PQuery.collect (fun n -> xs |> Seq.map (fun n' -> n' * n) )
                           |> PQuery.run
                let y = xs |> Seq.collect (fun n -> Seq.map (fun n' -> n' * n) xs )
                Seq.sum x = Seq.sum y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)
            Check.QuickThrowOnFailure (TestInput.RunTest<float> test)
         
        [<Test>]
        member __.``collect (nested groupBy)`` () =
            let test (xs : seq<int>) =
                let x = xs |> PQuery.ofSeq 
                           |> PQuery.collect (fun n -> xs |> Seq.groupBy (fun x -> x))
                           |> PQuery.map (fun (a,x)  -> x |> Seq.sum)
                           |> PQuery.run
                let y = xs |> Seq.collect (fun n ->  xs |> Seq.groupBy (fun x -> x))
                           |> Seq.map (fun (a,x)  -> x |> Seq.sum)
                Seq.sum x = Seq.sum y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)

        [<Test>]
        member __.``groupBy`` () =
            let test (xs : seq<int>) =
                let x = xs |> PQuery.ofSeq 
                           |> PQuery.groupBy (fun x -> string x)
                           |> PQuery.map (fun (a,x) -> Seq.sum x)
                           |> PQuery.run
                let y = xs |> Seq.groupBy (fun x -> string x)
                           |> Seq.map (fun (a,x) -> Seq.sum x)
                       
                Seq.sum x = Seq.sum y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``sort`` () =
            let test (xs : seq<int>) =
                let x = xs |> PQuery.ofSeq 
                           |> PQuery.sort
                           |> PQuery.run
                let y = xs |> Seq.sort
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``sortBy`` () =
            let test (xs : seq<int>) =
                let x = xs |> PQuery.ofSeq 
                           |> PQuery.sortBy (fun x -> -x)
                           |> PQuery.run
                let y = xs |> Seq.sortBy (fun x -> -x)
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``toArray`` () =
            let test (xs : seq<DateTime>) =
                let x = xs |> PQuery.ofSeq 
                           |> PQuery.toArray
                           |> PQuery.run
                let y = xs |> Seq.toArray
                equal (Seq.sort x) (Seq.sort y)
            Check.QuickThrowOnFailure (TestInput.RunTest test) 

        [<Test>]
        member __.``precompile function``() =
            let test (xs : seq<int>) =
                let t = PQuery.compile<seq<int>,int>(PrecompileHelpers.``fun x -> PQuery.length (PQuery.ofSeq x)``)
                let x = t(xs)
                let y = xs |> Seq.length
                x = y
            Check.QuickThrowOnFailure (TestInput.RunTest test)