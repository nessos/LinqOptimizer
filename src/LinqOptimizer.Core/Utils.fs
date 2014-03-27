namespace Nessos.LinqOptimizer.Core
open System
open System.Collections.Concurrent

// anton tayanovskyy's fast sprintf'
type Cache<'T> private () =
        static let d = ConcurrentDictionary<string,'T>()
 
        static member Format(format: Printf.StringFormat<'T>) : 'T =
            let key = format.Value
            match d.TryGetValue(key) with
            | true, r -> r
            | _ ->
                let r = sprintf format
                d.TryAdd(key, r) |> ignore
                r

module Utils = 
    
 
    let sprintf' fmt =
        Cache<_>.Format(fmt)

