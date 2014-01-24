namespace LinqOptimizer.Gpu
    open System
    open LinqOptimizer.Core

    open OpenCL.Net.Extensions
    open OpenCL.Net

    type GpuHelpers =
        
        

        static member Run(queryExpr : QueryExpr) : obj =
            let readFromBuffer (queue : CommandQueue) (t : Type) (outputBuffer : IMem) (output : obj) =
                match t with
                | TypeCheck Compiler.intType _ -> 
                    let output = output :?> int[]
                    if output.Length <> 0 then
                        queue.ReadFromBuffer(outputBuffer, output, 0, int64 -1)
                | TypeCheck Compiler.floatType _ ->  
                    let output = output :?> float[]
                    if output.Length <> 0 then
                        queue.ReadFromBuffer(outputBuffer, output, 0, int64 -1)
                | _ -> failwithf "Not supported result type %A" t

            let createDynamicArray (t : Type) (flags : int[]) (output : obj) : obj =
                match t with
                | TypeCheck Compiler.intType _ -> 
                    let output = output :?> int[]
                    let result = new System.Collections.Generic.List<int>()
                    for i = 0 to flags.Length - 1 do
                        if flags.[i] = 0 then
                            result.Add(output.[i])
                    result.ToArray() :> _
                | TypeCheck Compiler.floatType _ ->  
                    let output = output :?> float[]
                    let result = new System.Collections.Generic.List<float>()
                    for i = 0 to flags.Length - 1 do
                        if flags.[i] = 0 then
                            result.Add(output.[i])
                    result.ToArray() :> _
                | _ -> failwithf "Not supported result type %A" t


            let compilerResult = Compiler.compile queryExpr
            use env = "*".CreateCLEnvironment()
            // boilerplate
            match Cl.CreateProgramWithSource(env.Context, 1u, [| compilerResult.Source |], null) with
            | program, ErrorCode.Success ->
                use program = program
                match Cl.BuildProgram(program, uint32 env.Devices.Length, env.Devices, " -cl-fast-relaxed-math  -cl-mad-enable ", null, IntPtr.Zero) with
                | ErrorCode.Success -> 
                    match Cl.GetProgramBuildInfo(program, env.Devices.[0], ProgramBuildInfo.Log) with
                    | info, ErrorCode.Success -> 
                        use info = info
                        match Cl.CreateKernel(program, "kernelCode") with
                        | kernel, ErrorCode.Success -> 
                            use kernel = kernel
                            let inputBuffers = new System.Collections.Generic.List<IMem>()
                            try
                                for input, t, length, size in compilerResult.Args do
                                    match Cl.CreateBuffer(env.Context, MemFlags.ReadWrite ||| MemFlags.None ||| MemFlags.UseHostPtr, new IntPtr(if length = 0 then size else length * size), input) with
                                    | inputBuffer, ErrorCode.Success -> inputBuffers.Add(inputBuffer)
                                    | _, error -> failwithf "OpenCL.CreateBuffer failed with error code %A" error 
                                inputBuffers |> Seq.iteri (fun i inputBuffer -> 
                                                                            match Cl.SetKernelArg(kernel, uint32 i, inputBuffer) with
                                                                            | ErrorCode.Success -> ()
                                                                            | error -> failwithf "OpenCL.SetKernelArg failed with error code %A" error)
                                match Cl.EnqueueNDRangeKernel(env.CommandQueues.[0], kernel, uint32 1, null, [| new IntPtr(100) |], [| new IntPtr(1) |], uint32 0, null) with
                                | ErrorCode.Success, event ->
                                    use event = event
                                    // collect result
                                    match compilerResult.ReductionType with
                                    | ReductionType.Map -> 
                                        // last arg is the output buffer
                                        let (output, t, length, size) = compilerResult.Args.[compilerResult.Args.Length - 1]
                                        let outputBuffer = inputBuffers.[inputBuffers.Count - 1]
                                        readFromBuffer env.CommandQueues.[0] t outputBuffer output 
                                        output
                                    | ReductionType.Filter -> 
                                        // last arg is the output buffer
                                        let (output, t, length, size) = compilerResult.Args.[compilerResult.Args.Length - 1]
                                        let outputBuffer = inputBuffers.[inputBuffers.Count - 1]
                                        readFromBuffer env.CommandQueues.[0] t outputBuffer output 
                                        let (flags, t, length, size) = compilerResult.Args.[compilerResult.Args.Length - 2]
                                        let flagsBuffer = inputBuffers.[inputBuffers.Count - 2]
                                        readFromBuffer env.CommandQueues.[0] t flagsBuffer flags
                                        createDynamicArray t (flags :?> int[]) output 
                                    | reductionType -> failwith "Invalid ReductionType %A" reductionType
                                | _, error -> failwithf "OpenCL.EnqueueNDRangeKernel failed with error code %A" error
                            finally
                                inputBuffers |> Seq.iter (fun inputBuffer -> try inputBuffer.Dispose() with _ -> ())
                        | _, error -> failwithf "OpenCL.CreateKernel failed with error code %A" error
                    | _, error -> failwithf "OpenCL.GetProgramBuildInfo failed with error code %A" error
                | error -> failwithf "OpenCL.BuildProgram failed with error code %A" error
            | _, error -> failwithf "OpenCL.CreateProgramWithSource failed with error code %A" error
            
            

