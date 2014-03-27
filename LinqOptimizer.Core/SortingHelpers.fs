namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Threading.Tasks
    open System.Threading


    type private MergeArrayType =
    | FromArrayType
    | ToArrayType
 
   
   
    type Sort =
        
        static member SequentialSort(keys : 'Key[], values : 'Value[], orders : Order[]) = 
            Array.Sort(keys, values)
            if orders.Length = 1 then
                match orders.[0] with
                | Descending -> 
                        Array.Reverse(values)
                | _ -> ()


        
        static member ParallelSort<'Key, 'Value when 'Key :> IComparable<'Key>>(keys : 'Key[], array : 'Value[], orders : Order[]) = 
            // Taken from Carl Nolan's parallel inplace merge
            // The merge of the two array
            let merge (toArray: 'Value [], toKeys : 'Key[]) (fromArray: 'Value [], fromKeys : 'Key[]) (low1: int) (low2: int) (high1: int) (high2: int) =
                let mutable ptr1 = low1
                let mutable ptr2 = high1
 
                for ptr in low1..high2 do
                    if (ptr1 > low2) then
                        toArray.[ptr] <- fromArray.[ptr2]
                        toKeys.[ptr] <- fromKeys.[ptr2]
                        ptr2 <- ptr2 + 1
                    elif (ptr2 > high2) then
                        toArray.[ptr] <- fromArray.[ptr1]
                        toKeys.[ptr] <- fromKeys.[ptr1]
                        ptr1 <- ptr1 + 1
                    elif (fromKeys.[ptr1].CompareTo(fromKeys.[ptr2]) <= 0) then
                        toArray.[ptr] <- fromArray.[ptr1]
                        toKeys.[ptr] <- fromKeys.[ptr1]
                        ptr1 <- ptr1 + 1
                    else
                        toArray.[ptr] <- fromArray.[ptr2]
                        toKeys.[ptr] <- fromKeys.[ptr2]
                        ptr2 <- ptr2 + 1
 
            // define the sort operation
            let parallelSort () =
 
                // control flow parameters
                let totalWorkers = int (2.0 ** float (int (Math.Log(float Environment.ProcessorCount, 2.0))))
                let auxArray : 'Value array = Array.zeroCreate array.Length
                let auxKeys : 'Key array = Array.zeroCreate array.Length
                let workers : Task array = Array.zeroCreate (totalWorkers - 1)
                let iterations = int (Math.Log((float totalWorkers), 2.0))
 

 
                // Number of elements for each array, if the elements number is not divisible by the workers
                // the remainders will be added to the first worker (the main thread)
                let partitionSize = ref (int (array.Length / totalWorkers))
                let remainder = array.Length % totalWorkers
 
                // Define the arrays references for processing as they are swapped during each iteration
                let swapped = ref false
 
                let inline getMergeArray (arrayType: MergeArrayType) =
                    match (arrayType, !swapped) with
                    | (FromArrayType, true) -> (auxArray, auxKeys)
                    | (FromArrayType, false) -> (array, keys)
                    | (ToArrayType, true) -> (array, keys)
                    | (ToArrayType, false) -> (auxArray, auxKeys)
 
                use barrier = new Barrier(totalWorkers, fun (b) ->
                    partitionSize := !partitionSize <<< 1
                    swapped := not !swapped)
 
                // action to perform the sort an merge steps
                let action (index: int) =   
                         
                    //calculate the partition boundary
                    let low = index * !partitionSize + match index with | 0 -> 0 | _ -> remainder
                    let high = (index + 1) * !partitionSize - 1 + remainder
 
                    // Sort the specified range - could implement QuickSort here
                    let sortLen = high - low + 1
                    Array.Sort(keys, array, low, sortLen)
 
                    barrier.SignalAndWait()
 
                    let rec loopArray loopIdx actionIdx loopHigh =
                        if loopIdx < iterations then
                            if (actionIdx % 2 = 1) then
                                barrier.RemoveParticipant()
                            else
                                let newHigh = loopHigh + !partitionSize / 2
                                merge (getMergeArray FromArrayType) (getMergeArray ToArrayType) low loopHigh (loopHigh + 1) newHigh
                                barrier.SignalAndWait()
                                loopArray (loopIdx + 1) (actionIdx >>> 1) newHigh
                    loopArray 0 index high
 
                for index in 1 .. workers.Length do
                    workers.[index - 1] <- Task.Factory.StartNew(fun() -> action index)
 
                action 0
 
                // if odd iterations return auxArray otherwise array (swapped will be false)
                if not (iterations % 2 = 0) then  
                    Array.blit auxArray 0 array 0 array.Length
 
            parallelSort()
            if orders.Length = 1 then
                match orders.[0] with
                | Descending -> 
                        Array.Reverse(array)
                | _ -> ()

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

