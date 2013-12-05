namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    type CollectorList<'T> () =
        inherit List<'T>()

        interface IOrderedEnumerable<'T> with 
            member __.CreateOrderedEnumerable<'TKey>(keySelector : Func<'T, 'TKey> , comparer : IComparer<'TKey>, desc : bool) =
                raise <| NotImplementedException()

//    type ArrayCollector<'T> () =
//        
//        let ls = List<ICollection<'T>>()
//        
//        member __.Add(element : 'T) =
//            if ls.Count = 0 then
//                ls.Add(List<'T>())
//            ls.[ls.Count - 1].Add(element)
//
//        member __.AddRange(array : 'T []) = 
//            ls.Add(array)
//
//        member __.AddRange(lst : List<'T>) = 
//            ls.Add(lst)
//
//        member __.ToArray () = 
//            let length =  ls |> Seq.map (fun l -> l.Count) |> Seq.sum
//            let array = Array.zeroCreate<'T> length
//            let mutable i = 0
//            for col in ls do
//                for e in col do
//                    array.[i] <- e
//                    i <- i + 1
//            array
//
//        member this.ToList () =
//            let length =  ls |> Seq.map (fun l -> l.Count) |> Seq.sum
//            let lst = List<'T>(length)
//            let mutable i = 0
//            for col in ls do
//                for e in col do
//                    lst.[i] <- e
//                    i <- i + 1
//            lst
//                
//
//    module Foo =
//        let ac = ArrayCollector<int>()
//        let ra = ResizeArray<int>()
//        let x = Array.init 100000 id
//
//        #time
//
//        for i = 1 to 1000 do
//            ac.Add(i)
//        for i = 1 to 1000 do
//            ac.AddRange(x)
//        let ar1 = ac.ToArray()
//
//        for i = 1 to 1000 do
//            ra.Add(i)
//        for i = 1 to 1000 do
//            ra.AddRange(x)
//        let ar2 = ra.ToArray()
//

