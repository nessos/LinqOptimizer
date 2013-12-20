namespace LinqOptimizer.Tests

    open System
    open System.Collections.Generic
    open System.Linq
    open System.Text
    open FsCheck
    open NUnit.Framework
    open LinqOptimizer.FSharp

    type TestInput<'T> =
        | List of               'T list
        | Array of              'T []
        | Resize of ResizeArray<'T>
        | Sequence of           'T list

        static member RunTest<'T> (testF : seq<'T> -> bool) (input : TestInput<'T>) =
            match input with
            | List xs       -> testF xs
            | Array xs      -> testF xs
            | Resize xs     -> testF xs  
            | Sequence xs   -> testF (Seq.map id xs)

        static member RunTestFunc<'T> (testF : Func<IEnumerable<'T>,bool>, input : TestInput<'T>) =
            match input with
            | List xs       -> testF.Invoke xs
            | Array xs      -> testF.Invoke xs
            | Resize xs     -> testF.Invoke xs  
            | Sequence xs   -> testF.Invoke (Seq.map id xs)

    [<TestFixture>]
    type ``F# Query tests`` () =
    
        let equal x y = Enumerable.SequenceEqual(x,y)

        [<Test>]
        member __.``map`` () =
            let test (xs : seq<int>) =
                let x = xs |> Query.ofSeq |> Query.map (fun n -> 2 * n) |> Query.run
                let y = xs |> Seq.map (fun n -> 2 * n)
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)
        

        [<Test>]
        member __.``filter`` () =
            let test (xs : seq<'T>) =
                let x = xs |> Query.ofSeq |> Query.filter (fun n -> hash n % 2 = 0) |> Query.run
                let y = xs |> Seq.filter (fun n -> hash n % 2 = 0)
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``takeWhile`` () =
            let test (xs : seq<int>) =
                let x = xs |> Query.ofSeq |> Query.takeWhile (fun n -> n < 10) |> Query.run
                let y = xs |> Seq.takeWhile (fun n -> n < 10)
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``skipWhile`` () =
            let test (xs : seq<int>) =
                let x = xs |> Query.ofSeq |> Query.skipWhile (fun n -> n < 10) |> Query.run
                let y = xs |> Seq.skipWhile (fun n -> n < 10)
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``pipelined`` () =
            let test (xs : seq<int>) =
                let x = xs |> Query.ofSeq 
                           |> Query.filter (fun n -> n % 2 = 0) 
                           |> Query.map (fun n -> n * 2)
                           |> Query.map (fun n -> string n)
                           |> Query.run

                let y = xs |> Seq.filter (fun n -> n % 2 = 0) 
                           |> Seq.map (fun n -> n * 2)
                           |> Seq.map (fun n -> string n)
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``sum int`` () =
            let test (xs : seq<int>) =
                let x = xs |> Query.ofSeq |> Query.map (fun x -> x * x) |> Query.sum |> Query.run
                let y = xs |> Seq.map (fun x -> x * x) |> Seq.sum
                x = y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)

        [<Test>]
        member __.``sum double`` () =
            let test (xs : seq<float>) =
                let x = xs |> Query.ofSeq |> Query.map (fun x -> x * x) |> Query.sum |> Query.run
                let y = xs |> Seq.map (fun x -> x * x) |> Seq.sum
                (Double.IsNaN x && Double.IsNaN y) || x = y
            Check.QuickThrowOnFailure (TestInput.RunTest<float> test)

        [<Test>]
        member __.``fold`` () =
            let test (xs : seq<int>) =
                let x = xs |> Query.ofSeq 
                           |> Query.map (fun x -> x * x) 
                           |> Query.fold (fun acc x -> acc + x) 0
                           |> Query.run
                let y = xs |> Seq.map (fun x -> x * x) 
                           |> Seq.fold (fun acc x -> acc + x) 0
                x = y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``collect`` () =
            let test (xs : seq<'T>) =
                let x = xs |> Query.ofSeq 
                           |> Query.collect (fun n -> Seq.map (fun n' -> n' * n) xs)
                           |> Query.run
                let y = xs |> Seq.collect (fun n -> Seq.map (fun n' -> n' * n) xs )
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)
            Check.QuickThrowOnFailure (TestInput.RunTest<float> test)

        [<Test>]
        member __.``collect (nested pipe)`` () =
            let test (xs : seq<'T>) =
                let x = xs |> Query.ofSeq 
                           |> Query.collect (fun n -> xs |> Seq.map (fun n' -> n' * n) )
                           |> Query.run
                let y = xs |> Seq.collect (fun n -> Seq.map (fun n' -> n' * n) xs )
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)
            Check.QuickThrowOnFailure (TestInput.RunTest<float> test)
         
        [<Test>]
        member __.``collect (nested groupBy)`` () =
            let test (xs : seq<int>) =
                let x = xs |> Query.ofSeq 
                           |> Query.collect (fun n -> xs |> Seq.groupBy (fun x -> x))
                           |> Query.map (fun (_,x)  -> x |> Seq.sum)
                           |> Query.run
                let y = xs |> Seq.collect (fun n ->  xs |> Seq.groupBy (fun x -> x))
                           |> Seq.map (fun (_,x)  -> x |> Seq.sum)
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)
           
        [<Test>]
        member __.``take`` () =
            fun (xs : int list, n) -> 
                // Query.skip n xs and linq does not throw then n > xs.Length
                if n <= xs.Length && n >= 0 then
                    let x = xs |> Query.ofSeq 
                               |> Query.take n
                               |> Query.run
                    let y = xs |> Seq.take n
                    equal x y
                else true
            |> Check.QuickThrowOnFailure

        [<Test>]
        member __.``skip`` () =
            fun (xs : int list, n) -> 
                // Query.skip n xs and linq does not throw then n > xs.Length
                if n <= xs.Length && n >= 0 then
                    let x = xs |> Query.ofSeq 
                               |> Query.skip n
                               |> Query.run
                    let y = xs |> Seq.skip n
                    equal x y
                else true
            |> Check.QuickThrowOnFailure


        [<Test>]
        member __.``iter`` () =
            let test (xs : seq<int>) =
                let x = ResizeArray<int>()
                let y = ResizeArray<int>()
                xs |> Query.ofSeq 
                   |> Query.iter (fun i -> x.Add i)
                   |> Query.run

                xs |> Seq.iter (fun i -> y.Add i)
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)

        [<Test>]
        member __.``groupBy`` () =
            let test (xs : seq<int>) =
                let x = xs |> Query.ofSeq 
                           |> Query.groupBy (fun x -> string x)
                           |> Query.map (fun (_,x) -> Seq.sum x)
                           |> Query.run
                let y = xs |> Seq.groupBy (fun x -> string x)
                           |> Seq.map (fun (_,x) -> Seq.sum x)
                       
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``sort`` () =
            let test (xs : seq<int>) = 
                let x = xs |> Query.ofSeq 
                           |> Query.sort
                           |> Query.run
                let y = xs |> Seq.sort
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest<int> test)

        [<Test>]
        member __.``sortBy`` () =
            let test (xs : seq<int>) =
                let x = xs |> Query.ofSeq 
                           |> Query.sortBy (fun x -> -x)
                           |> Query.run
                let y = xs |> Seq.sortBy (fun x -> -x)
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``toArray`` () =
            let test (xs : seq<'T>) =
                let x = xs |> Query.ofSeq 
                           |> Query.toArray
                           |> Query.run
                let y = xs |> Seq.toArray
                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``length`` () =
            let test (xs : seq<obj>) =
                let x = xs |> Query.ofSeq 
                           |> Query.length
                           |> Query.run
                let y = xs |> Seq.length

                x = y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``mapi`` () =
            let test (xs : seq<obj>) =
                let x = xs |> Query.ofSeq 
                           |> Query.mapi (fun _ i -> i)
                           |> Query.run
                let y = xs |> Seq.mapi (fun i _ -> i)

                equal x y
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``precompile function``() =
            let test (xs : seq<int>) =
                let t = Query.compile(fun x -> Query.length (Query.ofSeq x) )
                let x = t(xs)
                let y = xs |> Seq.length
                x = y
            Check.QuickThrowOnFailure (TestInput.RunTest test)
            
        [<Test>]
        member __.``precompile action``() =
            let test (xs : seq<int>) =
                let a = ResizeArray<int>()
                let b = ResizeArray<int>()

                let t = Query.compile(fun x -> (Query.ofSeq x)|> Query.iter (fun m -> a.Add(m)))
                t(xs)

                xs |> Seq.iter b.Add
                equal a b
            Check.QuickThrowOnFailure (TestInput.RunTest test)

        [<Test>]
        member __.``precompile function pipelined``() =
            let test (xs : seq<int>) =
                let t = Query.compile(fun x -> x |> Query.ofSeq |> Query.length )
                let x = t(xs)
                let y = xs |> Seq.length
                x = y
            Check.QuickThrowOnFailure (TestInput.RunTest test)
            
        [<Test>]
        member __.``precompile action pipelined``() =
            let test (xs : seq<int>) =
                let a = ResizeArray<int>()
                let b = ResizeArray<int>()

                let t = Query.compile(fun x -> x |> Query.ofSeq |> Query.iter (fun m -> a.Add(m)))
                t(xs)

                xs |> Seq.iter b.Add
                equal a b
            Check.QuickThrowOnFailure (TestInput.RunTest test)
