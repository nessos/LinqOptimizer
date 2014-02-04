namespace LinqOptimizer.Gpu
    open System
    open OpenCL.Net.Extensions
    open OpenCL.Net
    

    // internal type for accessing the encapsulated gpu buffer
    type internal IGpuArray =
        inherit IDisposable
        abstract member Length : int
        abstract member GetBuffer : unit -> IMem
    
    /// <summary>
    /// A typed wrapper object for managing GPU Bufferss
    /// </summary>
    type GpuArray<'T when 'T : struct and 'T : (new : unit -> 'T) and 'T :> ValueType> (env : Environment, length : int, buffer : IMem) =
        let mutable disposed = false
        member self.ToArray() = 
            let array = Array.create length Unchecked.defaultof<'T>
            if length = 0 then
                array
            else
                env.CommandQueues.[0].ReadFromBuffer(buffer, array, 0, int64 array.Length)
                array

        interface IGpuArray with 
            member self.Length = length
            member self.GetBuffer () = buffer
        interface System.IDisposable with 
            member this.Dispose() = 
                if buffer <> null && disposed = false then
                    disposed <- true
                    buffer.Dispose()

