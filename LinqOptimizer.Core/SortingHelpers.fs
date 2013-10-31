namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Threading.Tasks

    type ParallelSort =
        
        static member QuicksortSequential(keys : 'Key[], values : 'Value[]) = 
            ParallelSort.QuicksortSequential(keys, values, 0, keys.Length - 1)
        
        static member QuicksortParallel(keys : 'Key[], values : 'Value[]) = 
            ParallelSort.QuicksortParallel(keys, values, 0, keys.Length - 1)
        
        static member QuicksortSequential(keys : 'Key[], values : 'Value[], left : int, right : int) = 
            if right > left then
                Array.Sort(keys, values, left, right - left)
        

        static member QuicksortParallel(keys : 'Key[], values : 'Value[], left : int, right : int) = 
            if right > left then
            
                if right - left < 2048 then
                    ParallelSort.QuicksortSequential(keys, values, left, right)
                else
                    let pivot = ParallelSort.Partition(keys, values, left, right)
                    Parallel.Invoke([| Action(fun () -> ParallelSort.QuicksortParallel(keys, values, left, pivot - 1)); 
                                       Action(fun () -> ParallelSort.QuicksortParallel(keys, values, pivot + 1, right)) |])
        

        static member Partition(keys : 'Key[], values : 'Value[], low : int, high : int) = 

            let inline swap (arr : 'T[]) i j =
                let tmp = arr.[i]
                arr.[i] <- arr.[j]
                arr.[j] <- tmp

            let pivotPos = (high + low) / 2
            let pivot = keys.[pivotPos]
            swap keys low pivotPos
            swap values low pivotPos

            let mutable left = low
            for i = low + 1 to high do
            
                if keys.[i] < pivot then
                    left <- left + 1
                    swap keys i left
                    swap values i left
            

            swap keys low left
            swap values low left
            left
       

