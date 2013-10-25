namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    type Grouping<'Key, 'Value> =
        new(key, count, values) = { key = key; count = count; index = -1; position = -1; values = values }
        val private key : 'Key
        val private values : 'Value[]
        val mutable private count : int
        val mutable private index : int
        val mutable private position : int
        
        member self.IncrementCount() = self.count <- self.count + 1
        member self.Count = self.count
        member self.Index = self.index
        member self.UpdateIndex(index : int) =
            if self.index = -1 then
                self.index <- index
                index + self.count
            else
                index 
        member self.AddValue(value : 'Value) = 
            self.position <- self.position + 1
            self.values.[self.index + self.position] <- value


        interface IGrouping<'Key, 'Value> with
            member self.Key = self.key

            member self.GetEnumerator() : IEnumerator<'Value> =
                let positionRef = ref self.index
                { new IEnumerator<'Value> with
                      member __.Current = self.values.[!positionRef - 1]
                    interface System.Collections.IEnumerator with
                      member __.Current = self.values.[!positionRef] :> _
                      member __.MoveNext() = 
                        if !positionRef >= self.index + self.count then
                            false
                        else
                            incr positionRef 
                            true
                      member __.Reset() = positionRef := self.index
                    interface IDisposable with
                      member __.Dispose() = () }

            member self.GetEnumerator() : IEnumerator =
                let enumerator : IEnumerator<'Value> = (self :> IGrouping<'Key, 'Value>).GetEnumerator()
                enumerator :> _

    type Grouping = 
        static member GroupBy(keys : 'Key[], values : 'Value[]) : IGrouping<'Key, 'Value>[] = 
            let values' : 'Value[] = Array.zeroCreate values.Length 
            let dict = new Dictionary<'Key, Grouping<'Key, 'Value>>()
            let mutable grouping = Unchecked.defaultof<Grouping<'Key, 'Value>>
            // grouping count
            for i = 0 to values.Length - 1 do
                let key = keys.[i]
                if not <| dict.TryGetValue(key, &grouping) then
                    grouping <- new Grouping<'Key, 'Value>(key, 1, values')
                    dict.Add(key, grouping)
                else
                    grouping.IncrementCount()
            // rearrange
            let mutable currentIndex = 0
            for i = 0 to values.Length - 1 do
                let key = keys.[i]
                let value = values.[i]
                dict.TryGetValue(key, &grouping) |> ignore 
                currentIndex <- grouping.UpdateIndex(currentIndex)
                grouping.AddValue(value)
            // collect results
            let result : IGrouping<'Key, 'Value>[] = Array.zeroCreate dict.Values.Count 
            let mutable i = 0
            for value in dict.Values do
                result.[i] <- value :> _
                i <- i + 1
            result

