namespace LinqOptimizer.Gpu
    open System
    open LinqOptimizer.Core

    open OpenCL.Net.Extensions
    open OpenCL.Net

    type GpuHelpers =
        
        

        static member Run(queryExpr : QueryExpr) : obj =
            let compilerResult = Compiler.compile queryExpr
            
            use env = "*".CreateCLEnvironment()
            
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
                                    // last arg is the output buffer
                                    let (output, t, length, size) = compilerResult.Args.[compilerResult.Args.Length - 1]
                                    let outputBuffer = inputBuffers.[inputBuffers.Count - 1]
                                    match t with
                                    | TypeCheck Compiler.intType _ -> 
                                        let output = output :?> int[]
                                        if output.Length <> 0 then
                                            env.CommandQueues.[0].ReadFromBuffer(outputBuffer, output, 0, int64 -1)
                                    | TypeCheck Compiler.floatType _ ->  
                                        let output = output :?> float[]
                                        if output.Length <> 0 then
                                            env.CommandQueues.[0].ReadFromBuffer(outputBuffer, output, 0, int64 -1)
                                    | _ -> failwithf "Not supported result type %A" t
                                | _, error -> failwithf "OpenCL.EnqueueNDRangeKernel failed with error code %A" error
                            finally
                                inputBuffers |> Seq.iter (fun inputBuffer -> try inputBuffer.Dispose() with _ -> ())
                        | _, error -> failwithf "OpenCL.CreateKernel failed with error code %A" error
                    | _, error -> failwithf "OpenCL.GetProgramBuildInfo failed with error code %A" error
                | error -> failwithf "OpenCL.BuildProgram failed with error code %A" error
            | _, error -> failwithf "OpenCL.CreateProgramWithSource failed with error code %A" error

            //  
            let (output, _, _, _) = compilerResult.Args.[compilerResult.Args.Length - 1]
            output
            
            

