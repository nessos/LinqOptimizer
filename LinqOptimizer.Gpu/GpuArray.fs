namespace LinqOptimizer.Gpu
    open System
    open OpenCL.Net.Extensions
    open OpenCL.Net
    

    /// <summary>
    /// Interface for managing GPU Buffers
    /// </summary>
    type IGpuArray =
        inherit IDisposable
        abstract member Length : int
        abstract member Capacity : int
        abstract member Size : int
        abstract member Type : Type
        abstract member GetBuffer : unit -> IMem
        abstract member ToArray : unit -> Array
        abstract member Refresh : unit -> unit
        abstract member Flash : unit -> unit

    /// <summary>
    /// A typed interface for managing GPU Buffers
    /// </summary>
    type IGpuArray<'T> =
        inherit IGpuArray
        abstract member Item : int -> 'T with get
        abstract member ToArray : unit -> 'T[]
        abstract member GetArray : unit -> 'T[]
        
    
    /// <summary>
    /// A typed wrapper object for managing GPU Buffers
    /// </summary>
    type GpuArray<'T when 'T : struct and 'T : (new : unit -> 'T) and 'T :> ValueType> 
                    (array : 'T[], env : Environment, length : int, capacity : int, size : int, buffer : IMem) =
        let mutable disposed = false
        new (env : Environment, length : int, capacity : int, size : int, buffer : IMem) = new GpuArray<'T>([||], env, length, capacity, size, buffer)
        
        member self.ToArray() = 
            let array = Array.create length Unchecked.defaultof<'T>
            if length = 0 then
                array
            else
                env.CommandQueues.[0].ReadFromBuffer(buffer, array, 0, int64 array.Length)
                array

        interface IGpuArray<'T> with 
            member self.Length = length
            member self.Capacity = capacity
            member self.Size = size
            member self.Type = typeof<'T>
            member self.GetBuffer () = buffer
            member self.ToArray () = self.ToArray() :> Array
            member self.ToArray () = self.ToArray()
            member self.GetArray () = array
            member this.Item with get(index) = array.[index]
            member self.Refresh () = env.CommandQueues.[0].ReadFromBuffer(buffer, array, 0, int64 array.Length)
            member self.Flash () = env.CommandQueues.[0].WriteToBuffer(buffer, array, 0, int64 array.Length)
        interface System.IDisposable with 
            member this.Dispose() = 
                if buffer <> null && disposed = false then
                    disposed <- true
                    buffer.Dispose()

