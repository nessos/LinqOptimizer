namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Threading.Tasks

   
    type Sort =
        
        static member SequentialSort(keys : 'Key[], values : 'Value[], orders : Order[]) = 
            Array.Sort(keys, values)
            if orders.Length = 1 then
                match orders.[0] with
                | Descending -> 
                        Array.Reverse(values)
                | _ -> ()

        static member ParallelSort(keys : 'Key[], values : 'Value[], orders : Order[]) = 
            Sort.ParallelSort(keys, values, 0, keys.Length - 1, orders)


        static member ParallelSort<'Key, 'Value when 'Key :> IComparable<'Key>>(keys : 'Key[], values : 'Value[], left : int, right : int, orders : Order[]) = 
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
                    if orders.Length = 1 then
                        match orders.[0] with
                        | Descending -> 
                            Array.Reverse(values)
                        | _ -> ()
                else
                    let pivot = partition keys values left right
                    Parallel.Invoke([| Action(fun () -> Sort.ParallelSort(keys, values, left, pivot - 1, orders)); 
                                       Action(fun () -> Sort.ParallelSort(keys, values, pivot + 1, right, orders)) |])
        

    // Composite keys for sorting, used only for code generation
    [<Struct>]
    [<CustomComparison>]
    [<CustomEquality>]
    type Keys<'T1, 'T2 when 'T1 :> IComparable<'T1> and 'T2 :> IComparable<'T2>>
        (t1 : 'T1, t2 : 'T2, o1 : Order, o2 : Order) =
        member self.T1 = t1
        member self.T2 = t2

        override self.Equals(_) =
            raise <| new NotImplementedException()
        override self.GetHashCode() =
            raise <| new NotImplementedException()

        interface IComparable<Keys<'T1, 'T2>> with

            member self.CompareTo(keys : Keys<'T1, 'T2>) =
                let cmpt1 = t1.CompareTo(keys.T1)
                if cmpt1 = 0 then
                    if o2 = Order.Ascending then
                        t2.CompareTo(keys.T2)
                    else
                        -t2.CompareTo(keys.T2)
                else
                    if o1 = Order.Ascending then
                        cmpt1
                    else
                        -cmpt1

        interface IComparable with
            member self.CompareTo(_) =
                raise <| new NotImplementedException()

