namespace Nessos.LinqOptimizer.Core
    open System
    open System.Collections.Generic
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    type ParallelismHelpers =
        static member TotalWorkers = int (2.0 ** float (int (Math.Log(float Environment.ProcessorCount, 2.0))))

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

            let seqReduceCount = 
                if length > ParallelismHelpers.TotalWorkers then 
                    length / ParallelismHelpers.TotalWorkers
                else
                    ParallelismHelpers.TotalWorkers
            let rec reduceCombine s e =
                async { 
                    if e - s <= seqReduceCount then
                        let s' = if s > 0 then s + 1 else s
                        let result = 
                            let mutable r = init.Invoke()
                            for i = s' to e do
                                r <- reducer.Invoke(r, i) 
                            r
                        return result
                    else 
                        let m = (s + e) / 2
                        let! result =  Async.Parallel [| reduceCombine s m; reduceCombine m e |]
                        return combiner.Invoke(result.[0], result.[1])
                }
            reduceCombine 0 (length - 1) |> Async.RunSynchronously |> selector.Invoke

        
        static member ReduceCombine<'T, 'Acc, 'R>( partitioner : Partitioner<'T>,
                                                    init : Func<'Acc>, 
                                                    reducer : Func<'Acc, 'T, 'Acc>,
                                                    combiner : Func<'Acc, 'Acc, 'Acc>,
                                                    selector : Func<'Acc, 'R>) : 'R = 

            let split (partitions : IEnumerator<'T> [])  =
                let half = partitions.Length / 2
                (partitions |> Seq.take half |> Seq.toArray, partitions |> Seq.skip half |> Seq.toArray)
                
            let rec reduceCombine (partitions : IEnumerator<'T>[]) =
                async {
                    match partitions with
                    | [||] -> return init.Invoke()
                    | [|partition|] ->
                        let result = 
                            let mutable r = init.Invoke()
                            while partition.MoveNext() do
                                r <- reducer.Invoke(r, partition.Current)
                            r
                        return result
                    | _ -> 
                        let (left, right) = split partitions
                        let! result =  Async.Parallel [| reduceCombine left; reduceCombine right |]
                        return combiner.Invoke(result.[0], result.[1])
                }
            reduceCombine (partitioner.GetPartitions(ParallelismHelpers.TotalWorkers) |> Seq.toArray) 
            |> Async.RunSynchronously |> selector.Invoke
            
