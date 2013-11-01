namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Threading.Tasks

    type ParallelSort =
        
 
        static member QuicksortParallel(keys : 'Key[], values : 'Value[]) = 
            ParallelSort.QuicksortParallel(keys, values, 0, keys.Length - 1)

        static member QuicksortParallel<'Key, 'Value when 'Key :> IComparable<'Key>>(keys : 'Key[], values : 'Value[], left : int, right : int) = 
            let swap (arr : 'T[]) i j =
                let tmp = arr.[i]
                arr.[i] <- arr.[j]
                arr.[j] <- tmp
            let partition (keys : 'Key[]) (values : 'Value[]) low high = 

                    let pivotPos = (high + low) / 2
                    let pivot = keys.[pivotPos]
                    swap keys low pivotPos
                    swap values low pivotPos

                    let mutable left = low
                    for i = low + 1 to high do
            
                        if keys.[i].CompareTo(pivot) < 0 then
                            left <- left + 1
                            swap keys i left
                            swap values i left
            

                    swap keys low left
                    swap values low left
                    left

            if right > left then
            
                if right - left < 2048 then
                    Array.Sort(keys, values, left, (right - left) + 1)
                else
                    let pivot = partition keys values left right
                    Parallel.Invoke([| Action(fun () -> ParallelSort.QuicksortParallel(keys, values, left, pivot - 1)); 
                                       Action(fun () -> ParallelSort.QuicksortParallel(keys, values, pivot + 1, right)) |])
        

        
       

