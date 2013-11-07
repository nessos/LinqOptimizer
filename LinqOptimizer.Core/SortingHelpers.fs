namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Threading.Tasks

    type Sort =
        
        static member SequentialSort(keys : 'Key[], values : 'Value[], order : Order) = 
            Array.Sort(keys, values)
            match order with
            | Descending -> 
                Array.Reverse(values)
            | _ -> ()

        static member ParallelSort(keys : 'Key[], values : 'Value[], order : Order) = 
            Sort.ParallelSort(keys, values, 0, keys.Length - 1, order)


        static member ParallelSort<'Key, 'Value when 'Key :> IComparable<'Key>>(keys : 'Key[], values : 'Value[], left : int, right : int, order : Order) = 
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
                    match order with
                    | Descending -> 
                        Array.Reverse(values)
                    | _ -> ()
                else
                    let pivot = partition keys values left right
                    Parallel.Invoke([| Action(fun () -> Sort.ParallelSort(keys, values, left, pivot - 1, order)); 
                                       Action(fun () -> Sort.ParallelSort(keys, values, pivot + 1, right, order)) |])
        

        
       

