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
//        let ls = List<'T [] * int>()
//
//        let mutable isAdd = false
//
//        let bufferSize                = 1024
//        let mutable currentBufferSize = bufferSize
//        let newBuffer( size )         = currentBufferSize <- size; Array.zeroCreate size
//        let mutable buffer : 'T []    = newBuffer bufferSize
//        let mutable idx               = 0
//       
//        let flushBuffer () =
//                ls.Add(buffer, idx)
//                buffer <- newBuffer bufferSize
//                isAdd <- false
//                idx <- 0
//
//        member this.AddRange(array : 'T[]) =
//            if isAdd then
//                flushBuffer()
//            ls.Add(array, array.Length)
//
//        member this.Add(elem : 'T) =
//            isAdd <- true
//            if idx >= currentBufferSize then
//                currentBufferSize <- currentBufferSize * 2
//                let newBuffer = newBuffer currentBufferSize
//                Array.Copy(buffer, newBuffer, idx)
//                this.Add(elem)
//            else
//                buffer.[idx] <- elem
//                idx <- idx + 1
//
//        member this.ToArray () : 'T [] =
//            if isAdd then
//                flushBuffer()
//
//            let mutable length = 0
//            for (_, l) in ls do 
//                length <- length + l
//
//            let final = Array.zeroCreate length
//            let mutable currIdx = 0
//            for (array, l) in ls do 
//                Array.Copy(array, 0, final, currIdx, l)
//                currIdx <- currIdx + l
//            final
//
//        member this.ArrayList with get () = ls
//                
//
//    module Foo =
//        let ac = ArrayCollector<int>()
//        let ra = ResizeArray<int>()
//        let x = Array.init 100000 id
//
//        #time
//
//        ac.AddRange([|0..1|])
//        ac.Add(1)
//        ac.Add(2)
//        ac.AddRange([|1..5|])
//        ac.AddRange([|5..10|])
//        ac.Add(42)
//        let a = ac.ToArray()
//
//        let y = ac.ArrayList
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

