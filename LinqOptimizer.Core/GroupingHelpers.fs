namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Threading;
    open System.Collections.Concurrent

    type Grouping<'Key, 'Value> =
        new(key, count, values) = { key = key; count = count; index = -1; position = -1; values = values }
        val private key : 'Key
        val private values : 'Value[]
        val mutable private count : int 
        val mutable private index : int
        val mutable private position : int
        
        
        member self.IncrCount() = self.count <- self.count + 1
        member self.InterlockedIncrCount() = Interlocked.Increment(&self.count)
        member self.Count = self.count
        member self.Position = self.position
        member self.Index with get () = self.index
                          and set (index) = self.index <- index
        member self.IncrPosition() = self.position <- self.position + 1
        member self.InterlockedIncrPosition() = Interlocked.Increment(&self.position)
            


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

        static member GroupBy(keys : 'Key[], values : 'Value[]) : IEnumerable<IGrouping<'Key, 'Value>> = 
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
                    grouping.IncrCount() 
            // rearrange
            let mutable currentIndex = 0
            for i = 0 to values.Length - 1 do
                let key = keys.[i]
                let value = values.[i]
                dict.TryGetValue(key, &grouping) |> ignore 
                if grouping.Index = -1 then
                    grouping.Index <- currentIndex 
                    currentIndex <- grouping.Index + grouping.Count

                grouping.IncrPosition() 
                values'.[grouping.Index + grouping.Position] <- value
            // collect results
            dict.Values |> Seq.map (fun value -> value :> _)

        static member ParallelGroupBy(keys : 'Key[], values : 'Value[]) : IEnumerable<IGrouping<'Key, 'Value>> = 
            let values' : 'Value[] = Array.zeroCreate values.Length 
            let dict = new ConcurrentDictionary<'Key, Grouping<'Key, 'Value>>()
            // grouping count
            ParallelismHelpers.ReduceCombine(keys, (fun () -> dict), 
                                    (fun (dict : ConcurrentDictionary<'Key, Grouping<'Key, 'Value>>) key -> 
                                        let mutable grouping = Unchecked.defaultof<Grouping<'Key, 'Value>>
                                        if not <| dict.TryGetValue(key, &grouping) then
                                            grouping <- new Grouping<'Key, 'Value>(key, 1, values')
                                            if not <| dict.TryAdd(key, grouping) then
                                                dict.TryGetValue(key, &grouping) |> ignore
                                                grouping.InterlockedIncrCount() |> ignore
                                        else
                                            grouping.InterlockedIncrCount() |> ignore
                                        dict
                                    ), (fun dict _ -> dict), (fun x -> x)) |> ignore 
            // fix grouping index
            let mutable currentIndex = 0 
            for grouping in dict.Values do
                grouping.Index <- currentIndex
                currentIndex <- currentIndex + grouping.Count
            // rearrange
            ParallelismHelpers.ReduceCombine(keys.Length, (fun () -> dict), 
                                    (fun (dict : ConcurrentDictionary<'Key, Grouping<'Key, 'Value>>) index -> 
                                        let mutable grouping = Unchecked.defaultof<Grouping<'Key, 'Value>>
                                        dict.TryGetValue(keys.[index], &grouping) |> ignore

                                        let position = grouping.InterlockedIncrPosition()
                                        values'.[grouping.Index + position] <- values.[index]
                                        dict
                                    ), (fun dict _ -> dict), (fun x -> x)) |> ignore 
            // collect results
            dict.Values |> Seq.map (fun value -> value :> _)

            
            

