namespace LinqOptimizer.Tests

open System
open System.Collections.Generic
open System.Linq
open System.Text
open FsCheck
open NUnit.Framework
open LinqOptimizer.FSharp

[<TestFixture>]
type ``F# Query tests`` () =
    
    let equal x y = Enumerable.SequenceEqual(x,y)

    [<Test>]
    member __.``map`` () =
        fun (xs : int list) -> 
            let x = xs |> Query.ofSeq |> Query.map (fun n -> n * 2) |> Query.run
            let y = xs |> Seq.map (fun n -> n * 2)
            equal x y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``filter`` () =
        fun (xs : int list) -> 
            let x = xs |> Query.ofSeq |> Query.filter (fun n -> n % 2 = 0) |> Query.run
            let y = xs |> Seq.filter (fun n -> n % 2 = 0)
            equal x y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``pipelined`` () =
        fun (xs : int list) -> 
            let x = xs |> Query.ofSeq 
                       |> Query.filter (fun n -> n % 2 = 0) 
                       |> Query.map (fun n -> n * 2)
                       |> Query.map (fun n -> string n)
                       |> Query.run

            let y = xs |> Seq.filter (fun n -> n % 2 = 0) 
                       |> Seq.map (fun n -> n * 2)
                       |> Seq.map (fun n -> string n)
            equal x y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``sum int`` () =
        fun (xs : int list) -> 
            let x = xs |> Query.ofSeq |> Query.map (fun x -> x * x) |> Query.sum |> Query.run
            let y = xs |> Seq.map (fun x -> x * x) |> Seq.sum
            x = y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``sum double`` () =
        fun (xs : float list) -> 
            let x = xs |> Query.ofSeq |> Query.map (fun x -> x * x) |> Query.sum |> Query.run
            let y = xs |> Seq.map (fun x -> x * x) |> Seq.sum
            (Double.IsNaN x && Double.IsNaN y) || x = y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``fold`` () =
        fun (xs : int list) -> 
            let x = xs |> Query.ofSeq 
                       |> Query.map (fun x -> x * x) 
                       |> Query.fold (fun acc x -> acc + x) 0
                       |> Query.run
            let y = xs |> Seq.map (fun x -> x * x) 
                       |> Seq.fold (fun acc x -> acc + x) 0
            x = y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``collect`` () =
        fun (xs : int list) -> 
            let x = xs |> Query.ofSeq 
                       |> Query.collect (fun n -> Seq.map (fun n' -> n' * n) xs)
                       |> Query.run
            let y = xs |> Seq.collect (fun n -> Seq.map (fun n' -> n' * n) xs )
            equal x y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``collect (nested pipe)`` () =
        fun (xs : int list) -> 
            let x = xs |> Query.ofSeq 
                       |> Query.collect (fun n -> xs |> Seq.map (fun n' -> n' * n) )
                       |> Query.run
            let y = xs |> Seq.collect (fun n -> Seq.map (fun n' -> n' * n) xs )
//            equal x y
            false
        |> Check.QuickThrowOnFailure
           
    [<Test>]
    member __.``take`` () =
        fun (xs : int list, n) -> 
            let x = xs |> Query.ofSeq 
                       |> Query.take n
                       |> Query.run
            let y = xs |> Seq.take n
            equal x y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``skip`` () =
        fun (xs : int list, n) -> 
            let x = xs |> Query.ofSeq 
                       |> Query.skip n
                       |> Query.run
            let y = xs |> Seq.skip n
            equal x y
        |> Check.QuickThrowOnFailure


    [<Test>]
    member __.``iter`` () =
        fun (xs : int list) -> 
            let x = ResizeArray<int>()
            let y = ResizeArray<int>()
            xs |> Query.ofSeq 
               |> Query.iter (fun i -> x.Add i)
               |> Query.run

            xs |> Seq.iter (fun i -> y.Add i)
            equal x y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``groupBy is buggy`` () =
        fun (xs : int list) -> 
            let x = xs |> Query.ofSeq 
                       |> Query.groupBy (fun x -> x)
                       |> Query.run
            let y = xs |> Seq.groupBy (fun x -> x)
            false
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``sortBy`` () =
        fun (xs : int list, n) -> 
            let x = xs |> Query.ofSeq 
                       |> Query.sortBy (fun x -> x)
                       |> Query.run
            let y = xs |> Seq.sortBy (fun x -> x)
            equal x y
        |> Check.QuickThrowOnFailure

    [<Test>]
    member __.``toArray`` () =
        fun (xs : int list, n) -> 
            let x = xs |> Query.ofSeq 
                       |> Query.toArray
                       |> Query.run
            let y = xs |> Seq.toArray
            equal x y
        |> Check.QuickThrowOnFailure