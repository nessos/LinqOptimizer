namespace LinqOptimizer.Gpu
    open System
    open System.Linq.Expressions
    open System.Runtime.InteropServices
    open LinqOptimizer.Core
    open OpenCL.Net

    module internal Compiler =
        type Length = int
        type Size = int

        type QueryContext = { CurrentVarExpr : ParameterExpression; AccVarExpr : ParameterExpression; FlagVarExpr : ParameterExpression;
                                BreakLabel : LabelTarget; ContinueLabel : LabelTarget;
                                InitExprs : Expression list; AccExpr : Expression; CombinerExpr : Expression; ReturnExpr : Expression; 
                                VarExprs : ParameterExpression list; Exprs : Expression list; ReductionType : ReductionType }

        type CompilerResult = { Source : string; ReductionType : ReductionType; Args : (IGpuArray * Type * Length * Size) []; Results : (obj * Type * Length * Size) [] }
        
        let intType = typeof<int>
        let floatType = typeof<single>
        let doubleType = typeof<double>
        let byteType = typeof<byte>
        let gpuArrayTypeDef = typedefof<GpuArray<_>>

        let breakLabel () = labelTarget "brk"
        let continueLabel () = labelTarget "cont"

        let compile (queryExpr : QueryExpr) : CompilerResult = 
            
            let mapTemplate = sprintf "
                            __kernel void kernelCode(__global %s* ___input___, __global %s* ___result___)
                            {
                                %s
                                int ___id___ = get_global_id(0);
                                %s = ___input___[___id___];
                                %s
                                ___result___[___id___] = %s;
                            }"

            let mapFilterTemplate = sprintf "
                            __kernel void kernelCode(__global %s* ___input___, __global int* ___flags___, __global %s* ___result___)
                            {
                                %s
                                int ___id___ = get_global_id(0);
                                %s = ___input___[___id___];
                                %s
                                cont:
                                ___flags___[___id___] = %s;
                                ___result___[___id___] = %s;
                            }"

            let reduceTemplate = sprintf "
                            __kernel void kernelCode(__global %s* ___input___, __global %s* ___result___, __local %s* ___partial___)
                            {
                                %s
                                int ___localId___  = get_local_id(0);
                                int ___groupSize___ = get_local_size(0);
                                %s = ___input___[get_global_id(0)];
                                %s
                                ___partial___[___localId___] = %s;
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

            let rec compile' (queryExpr : QueryExpr) (context : QueryContext) =
                let typeToStr (t : Type) = 
                    match t with
                    | TypeCheck intType _ -> "int"
                    | TypeCheck floatType _ -> "float"
                    | TypeCheck doubleType _ -> "float"
                    | TypeCheck byteType _ -> "byte"
                    | _ -> failwithf "Not supported %A" t
                let varExprToStr (varExpr : ParameterExpression) = 
                    let index = context.VarExprs |> List.findIndex (fun varExpr' -> varExpr = varExpr')
                    sprintf "%s%d" (varExpr.ToString()) index

                let rec exprToStr (expr : Expression) =
                    match expr with
                    | Constant (value, TypeCheck intType _) -> sprintf "%A" value
                    | Constant (value, TypeCheck floatType _) -> sprintf "%A" value
                    | Constant (value, TypeCheck doubleType _) -> sprintf "%A" value
                    | Constant (value, TypeCheck byteType _) -> sprintf "%A" value
                    | Parameter (paramExpr) -> varExprToStr paramExpr
                    | Assign (Parameter (paramExpr), expr') -> sprintf "%s = %s" (varExprToStr paramExpr) (exprToStr expr')
                    | Plus (leftExpr, rightExpr) -> sprintf "(%s + %s)" (exprToStr leftExpr) (exprToStr rightExpr)
                    | Times (leftExpr, rightExpr) -> sprintf "(%s * %s)" (exprToStr leftExpr) (exprToStr rightExpr)
                    | Modulo (leftExpr, rightExpr) -> sprintf "(%s %% %s)" (exprToStr leftExpr) (exprToStr rightExpr)
                    | Equal (leftExpr, rightExpr) -> sprintf "(%s == %s)" (exprToStr leftExpr) (exprToStr rightExpr)
                    | IFThenElse (testExpr, thenExpr, elseExpr) -> 
                        sprintf "if (%s) { %s; } else { %s; }" (exprToStr testExpr) (exprToStr thenExpr) (exprToStr elseExpr)
                    | Goto (kind, target, value) when kind = GotoExpressionKind.Continue -> sprintf "goto %s" target.Name 
                    | Block (_, exprs, _) -> 
                        exprs
                            |> Seq.map (fun expr -> sprintf "%s" (exprToStr expr))
                            |> Seq.reduce (fun first second -> sprintf "%s;%s%s" first Environment.NewLine second)
                    | Nop _ -> ""
                    | _ -> failwithf "Not supported %A" expr


                match queryExpr with
                | Source (Constant (value, Named (TypeCheck gpuArrayTypeDef _, [|_|])) as expr, sourceType, QueryExprType.Gpu) ->
                    let sourceTypeStr = typeToStr sourceType
                    let resultType = context.CurrentVarExpr.Type
                    let resultTypeStr = typeToStr resultType
                    let gpuArraySource = value :?> IGpuArray
                    let sourceLength = gpuArraySource.Length
                    let varsStr = context.VarExprs 
                                      |> List.map (fun varExpr -> sprintf "%s %s;" (typeToStr varExpr.Type) (varExprToStr varExpr)) 
                                      |> List.reduce (fun first second -> sprintf "%s%s%s" first Environment.NewLine second)
                    let exprsStr = context.Exprs
                                       |> List.map (fun expr -> sprintf "%s;" (exprToStr expr))
                                       |> List.reduce (fun first second -> sprintf "%s%s%s" first Environment.NewLine second)
                    match context.ReductionType with
                    | ReductionType.Map ->
                        let resultArray = Array.CreateInstance(resultType, sourceLength) :> obj
                        let source = mapTemplate sourceTypeStr resultTypeStr varsStr (varExprToStr context.CurrentVarExpr) exprsStr (varExprToStr context.AccVarExpr)
                        { Source = source; ReductionType = context.ReductionType; Args = [| (gpuArraySource, sourceType, sourceLength, Marshal.SizeOf(sourceType)) |];
                                                                                  Results = [| (resultArray, resultType, sourceLength, Marshal.SizeOf(resultType)) |] }
                    | ReductionType.Filter ->
                        let flagsArray = Array.CreateInstance(typeof<int>, sourceLength) :> obj
                        let resultArray = Array.CreateInstance(resultType, sourceLength) :> obj
                        let source = mapFilterTemplate sourceTypeStr resultTypeStr varsStr (varExprToStr context.CurrentVarExpr) exprsStr (varExprToStr context.FlagVarExpr) (varExprToStr context.AccVarExpr) 
                        { Source = source; ReductionType = context.ReductionType; Args = [| (value :?> IGpuArray, sourceType, sourceLength, Marshal.SizeOf(sourceType)) |];
                                                                                  Results = [| (flagsArray, typeof<int>, sourceLength, Marshal.SizeOf(typeof<int>))
                                                                                               (resultArray, resultType, sourceLength, Marshal.SizeOf(resultType)) |] }
                    | ReductionType.Sum -> 
                        let source = reduceTemplate sourceTypeStr resultTypeStr resultTypeStr varsStr (varExprToStr context.CurrentVarExpr) exprsStr (varExprToStr context.AccVarExpr) "+"
                        { Source = source; ReductionType = context.ReductionType; Args = [| (value :?> IGpuArray, sourceType, sourceLength, Marshal.SizeOf(sourceType)) |];
                                                                                  Results = [| |] }
                    | _ -> failwithf "Not supported %A" context.ReductionType
                | Transform (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                    let exprs' = assign context.CurrentVarExpr bodyExpr :: context.Exprs
                    compile' queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' }
                | Filter (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                    match context.ReductionType with
                    | ReductionType.Map | ReductionType.Filter ->
                        let exprs' = ifThenElse bodyExpr (assign context.FlagVarExpr (constant 0)) (block [] [assign context.FlagVarExpr (constant 1); (``continue`` context.ContinueLabel)]) :: assign context.CurrentVarExpr paramExpr :: context.Exprs
                        compile' queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs'; ReductionType = ReductionType.Filter } 
                    | _ ->
                        let exprs' = ifThenElse bodyExpr empty (``continue`` context.ContinueLabel) :: assign context.CurrentVarExpr paramExpr :: context.Exprs
                        compile' queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' } 
                | _ -> failwithf "Not supported %A" queryExpr 

            let finalVarExpr = var "___final___" queryExpr.Type
            let flagVarExpr = var "___flag___" typeof<int>
            match queryExpr with
            | Transform (_) ->
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = finalVarExpr; FlagVarExpr = flagVarExpr;
                                BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = []; AccExpr = empty; CombinerExpr = empty; ReturnExpr = empty; 
                                VarExprs = [finalVarExpr; flagVarExpr]; Exprs = []; ReductionType = ReductionType.Map  }
                compile' queryExpr context
            | Filter (_) ->
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = flagVarExpr; FlagVarExpr = flagVarExpr;
                                BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = []; AccExpr = empty; CombinerExpr = empty; ReturnExpr = empty; 
                                VarExprs = [finalVarExpr; flagVarExpr]; Exprs = []; ReductionType = ReductionType.Filter }
                compile' queryExpr context
            | Sum (queryExpr') ->
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = finalVarExpr; FlagVarExpr = flagVarExpr;
                                BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = []; AccExpr = empty; CombinerExpr = empty; ReturnExpr = empty; 
                                VarExprs = [finalVarExpr; flagVarExpr]; Exprs = []; ReductionType = ReductionType.Sum }
                compile' queryExpr' context
            | Count (_) ->
                let context : QueryContext = raise <| new NotImplementedException()
                compile' queryExpr context
            | _ -> failwithf "Not supported %A" queryExpr 

