namespace LinqOptimizer.Gpu
    open System
    open System.Collections.Generic
    open System.Collections.Concurrent
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
        /// Creates a GpuArray object
        /// </summary>
        /// <param name="gpuQuery">The array to be copied</param>
        /// <returns>A GpuArray object</returns>
        member self.Create<'T when 'T : struct and 'T : (new : unit -> 'T) and 'T :> ValueType>(array : 'T[]) = 
            if array.Length = 0 then
                new GpuArray<'T>(env, array.Length, null)
            else
                match Cl.CreateBuffer(env.Context, MemFlags.ReadWrite ||| MemFlags.None ||| MemFlags.UseHostPtr, new IntPtr(array.Length * sizeof<'T>), array) with
                | inputBuffer, ErrorCode.Success -> 
                    let gpuArray = new GpuArray<'T>(env, array.Length, inputBuffer)
                    buffers.Add(gpuArray)
                    gpuArray
                | _, error -> failwithf "OpenCL.CreateBuffer failed with error code %A" error 
            

        /// <summary>
        /// Compiles a gpu query to gpu kernel code, runs the kernel and returns the result.
        /// </summary>
        /// <param name="gpuQuery">The query to run.</param>
        /// <returns>The result of the query.</returns>
        member self.Run<'TQuery> (gpuQuery : IGpuQueryExpr<'TQuery>) : 'TQuery =
            let createGpuArray (t : Type) (env : Environment) (length : int) (buffer : IMem) = 
                let gpuArray = Activator.CreateInstance(typedefof<GpuArray<_>>.MakeGenericType [| t |], [| env :> obj; length :> obj; buffer :> obj|]) 
                buffers.Add(gpuArray :?> IGpuArray)
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
                    
            let kernelBuffers = new List<IMem>()
            for input, t, length, size in compilerResult.Args do
                if length <> 0 then 
                    kernelBuffers.Add(input.GetBuffer())
            for input, t, length, size in compilerResult.Results do
                if length <> 0 then 
                    match Cl.CreateBuffer(env.Context, MemFlags.ReadWrite ||| MemFlags.None ||| MemFlags.UseHostPtr, new IntPtr(length * size), input) with
                    | inputBuffer, ErrorCode.Success -> kernelBuffers.Add(inputBuffer)
                    | _, error -> failwithf "OpenCL.CreateBuffer failed with error code %A" error 
            kernelBuffers |> Seq.iteri (fun i inputBuffer -> 
                                                        match Cl.SetKernelArg(kernel, uint32 i, inputBuffer) with
                                                        | ErrorCode.Success -> ()
                                                        | error -> failwithf "OpenCL.SetKernelArg failed with error code %A" error)
                                
            match compilerResult.ReductionType with
            | ReductionType.Map -> 
                // last arg is the output buffer
                let (output, t, length, size) = compilerResult.Results.[0]
                if length = 0 then
                    createGpuArray t env length null :?> _
                else
                    let outputBuffer = kernelBuffers.[kernelBuffers.Count - 1]
                    match Cl.EnqueueNDRangeKernel(env.CommandQueues.[0], kernel, uint32 1, null, [| new IntPtr(length) |], [| new IntPtr(1) |], uint32 0, null) with
                    | ErrorCode.Success, event ->
                        use event = event
                        readFromBuffer env.CommandQueues.[0] t outputBuffer output 
                        createGpuArray t env length outputBuffer :?> _
                    | _, error -> failwithf "OpenCL.EnqueueNDRangeKernel failed with error code %A" error
            | ReductionType.Filter -> 
                // last 2 args are the flags and output buffer
                let (output, t, length, size) = compilerResult.Results.[1]
                if length = 0 then
                    createGpuArray t env length null :?> _
                else
                    let outputBuffer = kernelBuffers.[kernelBuffers.Count - 1]
                    use outputBuffer = outputBuffer 
                    let (flags, t, length, size) = compilerResult.Results.[0]
                    let flagsBuffer = kernelBuffers.[kernelBuffers.Count - 2]
                    use flagsBuffer = flagsBuffer 
                    match Cl.EnqueueNDRangeKernel(env.CommandQueues.[0], kernel, uint32 1, null, [| new IntPtr(length) |], [| new IntPtr(1) |], uint32 0, null) with
                    | ErrorCode.Success, event ->
                        use event = event
                        readFromBuffer env.CommandQueues.[0] t outputBuffer output 
                        readFromBuffer env.CommandQueues.[0] t flagsBuffer flags
                        let result = createDynamicArray t (flags :?> int[]) output
                        match Cl.CreateBuffer(env.Context, MemFlags.ReadWrite ||| MemFlags.None ||| MemFlags.UseHostPtr, new IntPtr(length * size), result) with
                        | resultBuffer, ErrorCode.Success -> 
                            createGpuArray t env result.Length resultBuffer :?> _
                        | _, error -> failwithf "OpenCL.CreateBuffer failed with error code %A" error 
                    | _, error -> failwithf "OpenCL.EnqueueNDRangeKernel failed with error code %A" error
            | reductionType -> failwith "Invalid ReductionType %A" reductionType


        interface System.IDisposable with 
            member this.Dispose() = 
                buffers |> Seq.iter (fun displosable -> try displosable.Dispose() with _ -> ())
                let disposable = cache |> Seq.map (fun (keyValue : KeyValuePair<_, _>) -> let program, kernel = keyValue.Value in [program :> IDisposable; kernel :> IDisposable]) |> Seq.concat |> Seq.toList
                (env :> IDisposable) :: disposable |> Seq.iter (fun displosable -> try displosable.Dispose() with _ -> ())
                
                
                        
            
            

