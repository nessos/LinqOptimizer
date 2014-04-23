namespace Nessos.LinqOptimizer.Core
    open System
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent
    open System.Threading.Tasks

    type ParallelismHelpers =
        static member TotalWorkers = int (2.0 ** float (int (Math.Log(float Environment.ProcessorCount, 2.0))))

        static member GetPartitions (s : int, e : int) = 
            let toSeq (enum : IEnumerator<_>)= 
                seq {
                    while enum.MoveNext() do
                        yield enum.Current
                }
            let partitioner = Partitioner.Create(s, e)
            let partitions = partitioner.GetPartitions(ParallelismHelpers.TotalWorkers) |> Seq.collect toSeq |> Seq.toArray 
            partitions

        static member ReduceCombine<'T, 'Acc, 'R>( array : 'T[],
                                                    init : Func<'Acc>, 
                                                    reducer : Func<'T[], int, int, 'Acc, 'Acc>,
                                                    combiner : Func<'Acc, 'Acc, 'Acc>,
                                                    selector : Func<'Acc, 'R>) : 'R = 
            if array.Length = 0 then
                selector.Invoke(init.Invoke())
            else
                let partitions = ParallelismHelpers.GetPartitions(0, array.Length)
                let cells = partitions |> Array.map (fun _ -> ref Unchecked.defaultof<'Acc>)
                let tasks = partitions |> Array.mapi (fun index (s, e) -> 
                                                        Task.Factory.StartNew(fun () -> 
                                                                    let result = reducer.Invoke(array, s - 1, e - 1, init.Invoke())
                                                                    cells.[index] := result
                                                                    ()))
                Task.WaitAll(tasks)
                let result = cells |> Array.fold (fun acc cell -> combiner.Invoke(acc, cell.Value)) (init.Invoke())
                selector.Invoke(result)

        static member ReduceCombine<'T, 'Acc, 'R>(values : IList<'T>, 
                                                    init : Func<'Acc>, 
                                                    reducer : Func<'Acc, 'T, 'Acc>,
                                                    combiner : Func<'Acc, 'Acc, 'Acc>,
                                                    selector : Func<'Acc, 'R>) : 'R = 
            ParallelismHelpers.ReduceCombine(values.Count, init, (fun acc index -> reducer.Invoke(acc, values.[index])), combiner, selector)
    

        static member ReduceCombine<'T, 'Acc, 'R>(length : int, 
                                                        init : Func<'Acc>, 
                                                        reducer : Func<'Acc, int, 'Acc>,
                                                        combiner : Func<'Acc, 'Acc, 'Acc>,
                                                        selector : Func<'Acc, 'R>) : 'R = 
            if length = 0 then
                selector.Invoke(init.Invoke())
            else
                let partitions = ParallelismHelpers.GetPartitions(0, length)
                let cells = partitions |> Array.map (fun _ -> ref Unchecked.defaultof<'Acc>)
                let tasks = partitions |> Array.mapi (fun index (s, e) -> 
                                                        Task.Factory.StartNew(fun () -> 
                                                                    let result = 
                                                                        let mutable r = init.Invoke()
                                                                        for i = s to e - 1 do
                                                                            r <- reducer.Invoke(r, i) 
                                                                        r
                                                                    cells.[index] := result
                                                                    ()))
                Task.WaitAll(tasks)
                let result = cells |> Array.fold (fun acc cell -> combiner.Invoke(acc, cell.Value)) (init.Invoke())
                selector.Invoke(result)


        
        static member ReduceCombine<'T, 'Acc, 'R>( partitioner : Partitioner<'T>,
                                                    init : Func<'Acc>, 
                                                    reducer : Func<'Acc, 'T, 'Acc>,
                                                    combiner : Func<'Acc, 'Acc, 'Acc>,
                                                    selector : Func<'Acc, 'R>) : 'R = 
                let partitions = partitioner.GetPartitions(ParallelismHelpers.TotalWorkers).ToArray()
                let cells = partitions |> Array.map (fun _ -> ref Unchecked.defaultof<'Acc>)
                let tasks = partitions |> Array.mapi (fun index partition -> 
                                                        Task.Factory.StartNew(fun () -> 
                                                                    let result = 
                                                                        let mutable r = init.Invoke()
                                                                        while partition.MoveNext() do
                                                                            r <- reducer.Invoke(r, partition.Current)
                                                                        r
                                                                    cells.[index] := result
                                                                    ()))
                Task.WaitAll(tasks)
                let result = cells |> Array.fold (fun acc cell -> combiner.Invoke(acc, cell.Value)) (init.Invoke())
                selector.Invoke(result)
            
