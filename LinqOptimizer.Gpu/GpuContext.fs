namespace LinqOptimizer.Gpu
    open System
    open System.Linq
    open System.Collections.Generic
    open System.Collections.Concurrent
    open System.Runtime.InteropServices
    open LinqOptimizer.Core
    open LinqOptimizer.Base
    open OpenCL.Net.Extensions
    open OpenCL.Net



    /// <summary>
    /// A scoped Context that manages GPU kernel execution and caching  
    /// </summary>
    type GpuContext() =

        let env = "*".CreateCLEnvironment()
        let cache = Dictionary<string, (Program * Kernel)>()
        let buffers = new System.Collections.Generic.List<IGpuArray>()

        /// <summary>
        /// Creates a GpuArray 
        /// </summary>
        /// <param name="gpuQuery">The array to be copied</param>
        /// <returns>A GpuArray object</returns>
        member self.CreateGpuArray<'T when 'T : struct and 'T : (new : unit -> 'T) and 'T :> ValueType>(array : 'T[]) = 
            if array.Length = 0 then
                new GpuArray<'T>(env, 0, 0, sizeof<'T>, null)
            else
                let capacity = int <| 2.0 ** Math.Ceiling(Math.Log(float array.Length, 2.0))
                match Cl.CreateBuffer(env.Context, MemFlags.ReadWrite ||| MemFlags.None ||| MemFlags.UseHostPtr, new IntPtr(capacity * sizeof<'T>), array) with
                | inputBuffer, ErrorCode.Success -> 
                    let gpuArray = new GpuArray<'T>(env, array.Length, capacity, sizeof<'T>, inputBuffer)
                    buffers.Add(gpuArray)
                    gpuArray
                | _, error -> failwithf "OpenCL.CreateBuffer failed with error code %A" error 
            

        /// <summary>
        /// Compiles a gpu query to gpu kernel code, runs the kernel and returns the result.
        /// </summary>
        /// <param name="gpuQuery">The query to run.</param>
        /// <returns>The result of the query.</returns>
        member self.Run<'TQuery> (gpuQuery : IGpuQueryExpr<'TQuery>) : 'TQuery =
            let createBuffer (t : Type) (env : Environment) (length : int) =
                match Cl.CreateBuffer(env.Context, MemFlags.ReadWrite ||| MemFlags.None ||| MemFlags.AllocHostPtr, new IntPtr(length * Marshal.SizeOf(t))) with
                | inputBuffer, ErrorCode.Success -> inputBuffer
                | _, error -> failwithf "OpenCL.CreateBuffer failed with error code %A" error 
                
            let createGpuArray (t : Type) (env : Environment) (length : int) (buffer : IMem) = 
                let gpuArray : IGpuArray = 
                    match t with
                    | TypeCheck Compiler.intType _ -> 
                        new GpuArray<int>(env, length, length, Marshal.SizeOf(t), buffer) :> _
                    | TypeCheck Compiler.floatType _ ->  
                        new GpuArray<single>(env, length, length, Marshal.SizeOf(t), buffer) :> _
                    | _ -> failwithf "Not supported result type %A" t
                buffers.Add(gpuArray)
                gpuArray

            let readFromBuffer (queue : CommandQueue) (t : Type) (outputBuffer : IMem) (output : obj) =
                match t with
                | TypeCheck Compiler.intType _ -> 
                    let output = output :?> int[]
                    queue.ReadFromBuffer(outputBuffer, output, 0, int64 output.Length)
                | TypeCheck Compiler.floatType _ ->  
                    let output = output :?> single[]
                    queue.ReadFromBuffer(outputBuffer, output, 0, int64 output.Length)
                | _ -> failwithf "Not supported result type %A" t

            let createDynamicArray (t : Type) (flags : int[]) (output : obj) : Array =
                match t with
                | TypeCheck Compiler.intType _ ->
                    let output = output :?> int[]
                    let result = new System.Collections.Generic.List<int>(flags.Length)
                    for i = 0 to flags.Length - 1 do
                        if flags.[i] = 0 then
                            result.Add(output.[i])
                    let result = result.ToArray() 
                    result :> _
                | TypeCheck Compiler.floatType _ ->  
                    let output = output :?> float[]
                    let result = new System.Collections.Generic.List<float>(flags.Length)
                    for i = 0 to flags.Length - 1 do
                        if flags.[i] = 0 then
                            result.Add(output.[i])
                    result.ToArray() :> _
                | _ -> failwithf "Not supported result type %A" t


            let queryExpr = gpuQuery.Expr
            let compilerResult = Compiler.compile queryExpr
            let kernel = 
                if cache.ContainsKey(compilerResult.Source) then
                    let (_, kernel) = cache.[compilerResult.Source]
                    kernel
                else
                    let program, kernel = 
                        match Cl.CreateProgramWithSource(env.Context, 1u, [| compilerResult.Source |], null) with
                        | program, ErrorCode.Success ->
                            match Cl.BuildProgram(program, uint32 env.Devices.Length, env.Devices, " -cl-fast-relaxed-math  -cl-mad-enable ", null, IntPtr.Zero) with
                            | ErrorCode.Success -> 
                                match Cl.CreateKernel(program, "kernelCode") with
                                | kernel, ErrorCode.Success -> 
                                    (program, kernel)
                                | _, error -> failwithf "OpenCL.CreateKernel failed with error code %A" error
                            | error -> failwithf "OpenCL.BuildProgram failed with error code %A" error
                        | _, error -> failwithf "OpenCL.CreateProgramWithSource failed with error code %A" error
                    cache.Add(compilerResult.Source, (program, kernel))
                    kernel
                    
            let argIndex = ref -1
            let addKernelArg (buffer : IMem) = 
                incr argIndex 
                match Cl.SetKernelArg(kernel, uint32 !argIndex, buffer) with
                | ErrorCode.Success -> ()
                | error -> failwithf "OpenCL.SetKernelArg failed with error code %A" error
            for input, t, length, size in compilerResult.Args do
                if length <> 0 then 
                    addKernelArg (input.GetBuffer())
                                
            match compilerResult.ReductionType with
            | ReductionType.Map -> 
                let (input, _, length, size) = compilerResult.Args.[0]
                if length = 0 then
                    createGpuArray queryExpr.Type env length null :> obj :?> _
                else
                    let outputBuffer = createBuffer queryExpr.Type env length 
                    addKernelArg outputBuffer 
                    match Cl.EnqueueNDRangeKernel(env.CommandQueues.[0], kernel, uint32 1, null, [| new IntPtr(length) |], [| new IntPtr(1) |], uint32 0, null) with
                    | ErrorCode.Success, event ->
                        use event = event
                        createGpuArray queryExpr.Type env length outputBuffer :> obj :?> _
                    | _, error -> failwithf "OpenCL.EnqueueNDRangeKernel failed with error code %A" error
            | ReductionType.Filter -> 
                let (input, _, length, size) = compilerResult.Args.[0]
                if length = 0 then
                    createGpuArray queryExpr.Type env length null :> obj :?> _
                else
                    let output = Array.CreateInstance(queryExpr.Type, length)
                    use outputBuffer = createBuffer queryExpr.Type env length
                    let flags = Array.CreateInstance(typeof<int>, length)
                    use flagsBuffer = createBuffer typeof<int> env length 
                    addKernelArg flagsBuffer 
                    addKernelArg outputBuffer 
                    match Cl.EnqueueNDRangeKernel(env.CommandQueues.[0], kernel, uint32 1, null, [| new IntPtr(length) |], [| new IntPtr(1) |], uint32 0, null) with
                    | ErrorCode.Success, event ->
                        use event = event
                        readFromBuffer env.CommandQueues.[0] queryExpr.Type outputBuffer output 
                        readFromBuffer env.CommandQueues.[0] typeof<int> flagsBuffer flags
                        let result = createDynamicArray queryExpr.Type (flags :?> int[]) output
                        match Cl.CreateBuffer(env.Context, MemFlags.ReadWrite ||| MemFlags.None ||| MemFlags.UseHostPtr, new IntPtr(length * size), result) with
                        | resultBuffer, ErrorCode.Success -> 
                            createGpuArray queryExpr.Type env result.Length resultBuffer :> obj :?> _
                        | _, error -> failwithf "OpenCL.CreateBuffer failed with error code %A" error 
                    | _, error -> failwithf "OpenCL.EnqueueNDRangeKernel failed with error code %A" error
            | ReductionType.Sum | ReductionType.Count ->
                let (gpuArray, _, length, _) = compilerResult.Args.[0]
                if length = 0 then
                    0 :> obj :?> _
                else
                    let maxGroupSize = 
                        match Cl.GetDeviceInfo(env.Devices.[0], DeviceInfo.MaxWorkGroupSize) with
                        | info, ErrorCode.Success -> info.CastTo<int>()
                        | _, error -> failwithf "OpenCL.GetDeviceInfo failed with error code %A" error

                    let (maxGroupSize, outputLength) = if gpuArray.Capacity < maxGroupSize then (gpuArray.Capacity, 1)
                                                       else (maxGroupSize, gpuArray.Capacity / maxGroupSize)

                    match Cl.SetKernelArg(kernel, (incr argIndex; uint32 !argIndex), new IntPtr(sizeof<int>), length) with
                    | ErrorCode.Success -> ()
                    | error -> failwithf "OpenCL.SetKernelArg failed with error code %A" error
                    let output = Array.CreateInstance(queryExpr.Type, outputLength)
                    use outputBuffer = createBuffer queryExpr.Type env outputLength
                    addKernelArg outputBuffer
                    // local buffer
                    match Cl.SetKernelArg(kernel, (incr argIndex; uint32 !argIndex), new IntPtr(Marshal.SizeOf(queryExpr.Type) * maxGroupSize), null) with
                    | ErrorCode.Success ->
                        match Cl.EnqueueNDRangeKernel(env.CommandQueues.[0], kernel, uint32 1, null, [| new IntPtr(gpuArray.Capacity) |], [| new IntPtr(maxGroupSize) |], uint32 0, null) with
                        | ErrorCode.Success, event ->
                            use event = event
                            readFromBuffer env.CommandQueues.[0] queryExpr.Type outputBuffer output 
                            match queryExpr.Type with
                            | TypeCheck Compiler.intType _ ->
                                (output :?> int[]).Sum() :> obj :?> _ 
                            | TypeCheck Compiler.floatType _ ->  
                                (output :?> System.Single[]).Sum() :> obj :?> _  
                            | t -> failwithf "Not supported result type %A" t
                        | error, _  -> failwithf "OpenCL.EnqueueNDRangeKernel failed with error code %A" error
                    | error -> failwithf "OpenCL.SetKernelArg failed with error code %A" error
            | reductionType -> failwith "Invalid ReductionType %A" reductionType


        interface System.IDisposable with 
            member this.Dispose() = 
                buffers |> Seq.iter (fun displosable -> try displosable.Dispose() with _ -> ())
                let disposable = cache |> Seq.map (fun (keyValue : KeyValuePair<_, _>) -> let program, kernel = keyValue.Value in [program :> IDisposable; kernel :> IDisposable]) |> Seq.concat |> Seq.toList
                (env :> IDisposable) :: disposable |> Seq.iter (fun displosable -> try displosable.Dispose() with _ -> ())
                
                
                        
            
            

