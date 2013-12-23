namespace LinqOptimizer.Gpu
    open System
    open LinqOptimizer.Core

    open OpenCL.Net.Extensions
    open OpenCL.Net

    type GpuHelpers =
        
        static member Compile(queryExpr : QueryExpr) : Func<obj[], obj> =
            let kernel = Compiler.compile queryExpr
            let env = "*".CreateCLEnvironment()
            // var a = Cl.CreateBuffer(env.Context, MemFlags.ReadWrite | MemFlags.None | MemFlags.UseHostPtr, (IntPtr)(ArrayLength * 4), input, out error);
//            var program = Cl.CreateProgramWithSource(env.Context, 1u, new string[] { kernelSrc }, null, out error);
//
//            error = Cl.BuildProgram(program, (uint)env.Devices.Length, env.Devices, " -cl-fast-relaxed-math  -cl-mad-enable ", null, IntPtr.Zero);
//            var info = Cl.GetProgramBuildInfo(program, env.Devices[0], ProgramBuildInfo.Log, out error);
//
//            var kernel = Cl.CreateKernel(program, "doSomething", out error);
            //error = Cl.SetKernelArg(kernel, 0, a);
            //error = Cl.EnqueueNDRangeKernel(env.CommandQueues[0], kernel, (uint)1, null, new IntPtr[] { (IntPtr)100 }, new IntPtr[] { (IntPtr)20 }, (uint)0, null, out eventID);
            // env.CommandQueues[0].ReadFromBuffer(b, results);
            raise <| new NotImplementedException()

