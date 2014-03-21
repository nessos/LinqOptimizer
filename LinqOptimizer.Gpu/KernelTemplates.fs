namespace LinqOptimizer.Gpu

    module KernelTemplates = 

        let openCLExtensions = "#pragma OPENCL EXTENSION cl_khr_fp64 : enable"

        let mapTemplate = sprintf "%s
                            __kernel void kernelCode(__global %s* ___input___, %s __global %s* ___result___)
                            {
                                %s
                                int ___id___ = get_global_id(0);
                                %s = ___input___[___id___];
                                %s
                                ___result___[___id___] = %s;
                            }"

        let mapFilterTemplate = sprintf "%s
                        __kernel void kernelCode(__global %s* ___input___, %s __global int* ___flags___, __global %s* ___result___)
                        {
                            %s
                            int ___id___ = get_global_id(0);
                            %s = ___input___[___id___];
                            %s
                            cont:
                            ___flags___[___id___] = %s;
                            ___result___[___id___] = %s;
                        }"

        let reduceTemplate = sprintf "%s
                        __kernel void kernelCode(__global %s* ___input___, %s int ___inputLength___, __global %s* ___result___, __local %s* ___partial___)
                        {
                            %s
                            int ___localId___  = get_local_id(0);
                            int ___globalId___  = get_global_id(0);
                            int ___groupSize___ = get_local_size(0);
                            %s = ___input___[___globalId___];
                            if(___globalId___ < ___inputLength___)
                            {
                                %s
                                ___partial___[___localId___] = %s;
                            }
                            else
                            {
                            cont:
                                ___partial___[___localId___] = %s;
                            }
                            barrier(CLK_LOCAL_MEM_FENCE);

                            for(int ___i___ = ___groupSize___ / 2; ___i___ > 0; ___i___ >>= 1) {
                                if(___localId___ < ___i___) {
                                    ___partial___[___localId___] = ___partial___[___localId___] %s ___partial___[___localId___ + ___i___];
                                }
                                barrier(CLK_LOCAL_MEM_FENCE);
                            }

                            if(___localId___ == 0) {
                                ___result___[get_group_id(0)] = ___partial___[0];
                            }
                        }"

        let zip2Template = sprintf "%s
                        __kernel void kernelCode(__global %s* ___first___, __global %s* ___second___, %s __global %s* ___result___)
                        {
                            %s
                            int ___id___ = get_global_id(0);
                            %s = ___first___[___id___];
                            %s = ___second___[___id___];
                            %s = %s;
                            %s
                            ___result___[___id___] = %s;
                        }"

        let zip2FilterTemplate = sprintf "%s
                        __kernel void kernelCode(__global %s* ___first___, __global %s* ___second___, %s __global int* ___flags___, __global %s* ___result___)
                        {
                            %s
                            int ___id___ = get_global_id(0);
                            %s = ___first___[___id___];
                            %s = ___second___[___id___];
                            %s = %s;
                            %s
                            cont:
                            ___flags___[___id___] = %s;
                            ___result___[___id___] = %s;
                        }"


        let zip2ReduceTemplate = sprintf "%s
                        __kernel void kernelCode(__global %s* ___first___, __global %s* ___second___, %s int ___inputLength___, __global %s* ___result___, __local %s* ___partial___)
                        {
                            %s
                            int ___localId___  = get_local_id(0);
                            int ___globalId___  = get_global_id(0);
                            int ___groupSize___ = get_local_size(0);
                            %s = ___first___[___globalId___];
                            %s = ___second___[___globalId___];
                            %s = %s;
                            if(___globalId___ < ___inputLength___)
                            {
                                %s
                                ___partial___[___localId___] = %s;
                            }
                            else
                            {
                            cont:
                                ___partial___[___localId___] = %s;
                            }
                            barrier(CLK_LOCAL_MEM_FENCE);

                            for(int ___i___ = ___groupSize___ / 2; ___i___ > 0; ___i___ >>= 1) {
                                if(___localId___ < ___i___) {
                                    ___partial___[___localId___] = ___partial___[___localId___] %s ___partial___[___localId___ + ___i___];
                                }
                                barrier(CLK_LOCAL_MEM_FENCE);
                            }

                            if(___localId___ == 0) {
                                ___result___[get_group_id(0)] = ___partial___[0];
                            }
                        }"
