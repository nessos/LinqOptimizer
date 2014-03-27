namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    type ArrayCollector<'T> () =

        let ls = List<'T [] * int>()

        let mutable isAdd = false

        let bufferSize                = 1024
        let mutable currentBufferSize = bufferSize
        let newBuffer( size )         = currentBufferSize <- size; Array.zeroCreate<'T> size
        let mutable buffer : 'T []    = newBuffer bufferSize
        let mutable idx               = 0
       
        let flushBuffer () =
                ls.Add(buffer, idx)
                buffer <- newBuffer bufferSize
                isAdd <- false
                idx <- 0

        member this.AddRange(collector : ArrayCollector<'T>) =
            this.Flush()
            collector.Flush()
            Seq.iter ls.Add collector.ArrayList

        member this.Add(elem : 'T) =
            isAdd <- true
            if idx >= currentBufferSize then
                let newBuf = newBuffer (currentBufferSize * 2)
                Array.Copy(buffer, newBuf, idx)
                buffer <- newBuf
                this.Add(elem)
            else
                buffer.[idx] <- elem
                idx <- idx + 1

        member this.ToArray () : 'T [] =
            this.Flush()

            let mutable length = 0
            for (_, l) in ls do 
                length <- length + l

            let final = Array.zeroCreate length
            let mutable currIdx = 0
            for (array, l) in ls do 
                Array.Copy(array, 0, final, currIdx, l)
                currIdx <- currIdx + l
            final

        member this.ToList () : List<'T> =
            List<'T>(this.ToArray())

        member internal this.ArrayList with get () = ls

        member internal this.Flush () =
            if isAdd then flushBuffer ()

        interface IEnumerable<'T> with
            member this.GetEnumerator() =
                let array = this.ToArray() :> IEnumerable<'T>
                array.GetEnumerator()

            member this.GetEnumerator() =
                let list = this.ToArray()
                list.GetEnumerator()
        
        interface IOrderedEnumerable<'T> with 
            member __.CreateOrderedEnumerable<'TKey>(keySelector : Func<'T, 'TKey> , comparer : IComparer<'TKey>, desc : bool) =
                raise <| NotImplementedException()
