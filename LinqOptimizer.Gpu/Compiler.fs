namespace LinqOptimizer.Gpu
    open System
    open System.Linq
    open System.Linq.Expressions
    open System.Runtime.InteropServices
    open LinqOptimizer.Core
    open OpenCL.Net
    open LinqOptimizer.Core.Utils

    module internal Compiler =
        type Length = int
        type Size = int

        type QueryContext = { CurrentVarExpr : ParameterExpression; AccVarExpr : ParameterExpression; FlagVarExpr : ParameterExpression;
                                BreakLabel : LabelTarget; ContinueLabel : LabelTarget;
                                InitExprs : Expression list; AccExpr : Expression; CombinerExpr : Expression; ResultType : Type; 
                                VarExprs : ParameterExpression list; Exprs : Expression list; ReductionType : ReductionType }

        type CompilerResult = { Source : string; ReductionType : ReductionType; Args : IGpuArray [] }
        
        let intType = typeof<int>
        let floatType = typeof<single>
        let doubleType = typeof<double>
        let byteType = typeof<byte>
        let gpuArrayTypeDef = typedefof<GpuArray<_>>

        let breakLabel () = labelTarget "brk"
        let continueLabel () = labelTarget "cont"

        let rec compile (queryExpr : QueryExpr) : CompilerResult = 

            let rec compile' (queryExpr : QueryExpr) (context : QueryContext) =
                let typeToStr (t : Type) = 
                    match t with
                    | TypeCheck intType _ -> "int"
                    | TypeCheck floatType _ -> "float"
                    | TypeCheck doubleType _ -> "float"
                    | TypeCheck byteType _ -> "byte"
                    | _ when t.IsValueType -> t.Name
                    | _ -> failwithf "Not supported %A" t

                let varExprToStr (varExpr : ParameterExpression) (vars : ParameterExpression list) = 
                    let index = vars |> List.findIndex (fun varExpr' -> varExpr = varExpr')
                    sprintf' "%s%d" (varExpr.ToString()) index
                
                let isCustomStruct (t : Type) = t.IsValueType && not t.IsPrimitive
                let structToStr (t : Type) = 
                    let fieldsStr = 
                        (t.GetFields(), "")
                        ||> Array.foldBack (fun fieldInfo fieldsStr -> sprintf' "%s %s; %s" (typeToStr fieldInfo.FieldType) fieldInfo.Name fieldsStr) 
                    sprintf' "typedef struct { %s } %s;" fieldsStr t.Name
                let collectCustomStructs (expr : Expression) = 
                    match expr with
                    | AnonymousTypeAssign(_ ,AnonymousTypeConstruction(members, args)) 
                        when isCustomStruct <| args.Last().Type -> 
                        [args.Last().Type] 
                    | Assign (Parameter (paramExpr), expr') when isCustomStruct paramExpr.Type -> 
                        [paramExpr.Type]
                    | _ when isCustomStruct expr.Type -> [expr.Type]
                    | _ -> []
                let customStructsToStr (types : seq<Type>) = 
                    types 
                    |> Seq.map structToStr
                    |> Seq.fold (fun first second -> sprintf' "%s%s%s" first Environment.NewLine second) ""
                let structsDefinitionStr (exprs : Expression list) =
                    exprs
                    |> List.collect collectCustomStructs
                    |> customStructsToStr

                let rec exprToStr (expr : Expression) (vars : ParameterExpression list) =
                    match expr with
                    | AnonymousTypeMember expr ->
                        match vars |> List.tryFind (fun varExpr -> varExpr.Name = expr.Member.Name) with
                        | Some varExpr -> varExprToStr varExpr vars
                        | None -> expr.Member.Name
                    | AnonymousTypeAssign(_ ,AnonymousTypeConstruction(members, args)) ->
                        sprintf' "%s %s = %s" (typeToStr <| args.Last().Type) (members.Last().Name) (exprToStr <| args.Last() <| vars)
                    | FieldMember (expr, fieldMember) -> sprintf' "%s.%s" (exprToStr expr vars) fieldMember.Name
                    | ValueTypeMemberInit (members, bindings) ->
                        let bindingsStr = bindings |> Seq.fold (fun bindingsStr binding -> sprintf' ".%s = %s, %s" binding.Member.Name (exprToStr binding.Expression vars) bindingsStr) ""
                        sprintf' "(%s) { %s }" (typeToStr expr.Type) bindingsStr
                    | Constant (value, TypeCheck intType _) -> sprintf' "%A" value
                    | Constant (value, TypeCheck floatType _) -> sprintf' "%A" value
                    | Constant (value, TypeCheck doubleType _) -> sprintf' "%A" value
                    | Constant (value, TypeCheck byteType _) -> sprintf' "%A" value
                    | Parameter (paramExpr) -> varExprToStr paramExpr vars
                    | Assign (Parameter (paramExpr), expr') -> sprintf' "%s = %s" (varExprToStr paramExpr vars) (exprToStr expr' vars)
                    | Plus (leftExpr, rightExpr) -> sprintf' "(%s + %s)" (exprToStr leftExpr vars) (exprToStr rightExpr vars)
                    | Subtract (leftExpr, rightExpr) -> sprintf' "(%s - %s)" (exprToStr leftExpr vars) (exprToStr rightExpr vars)
                    | Times (leftExpr, rightExpr) -> sprintf' "(%s * %s)" (exprToStr leftExpr vars) (exprToStr rightExpr vars)
                    | Divide (leftExpr, rightExpr) -> sprintf' "(%s * %s)" (exprToStr leftExpr vars) (exprToStr rightExpr vars)
                    | Modulo (leftExpr, rightExpr) -> sprintf' "(%s %% %s)" (exprToStr leftExpr vars) (exprToStr rightExpr vars)
                    | Equal (leftExpr, rightExpr) -> sprintf' "(%s == %s)" (exprToStr leftExpr vars) (exprToStr rightExpr vars)
                    | IFThenElse (testExpr, thenExpr, elseExpr) -> 
                        sprintf' "if (%s) { %s; } else { %s; }" (exprToStr testExpr vars) (exprToStr thenExpr vars) (exprToStr elseExpr vars)
                    | Goto (kind, target, value) when kind = GotoExpressionKind.Continue -> sprintf' "goto %s" target.Name 
                    | Block (_, exprs, _) -> 
                        exprs
                            |> Seq.map (fun expr -> sprintf' "%s" (exprToStr expr vars))
                            |> Seq.reduce (fun first second -> sprintf' "%s;%s%s" first Environment.NewLine second)
                    | Convert (expr, t) -> sprintf' "((%s) %s)" (typeToStr t) (exprToStr expr vars)
                    | Nop _ -> ""
                    | _ -> failwithf "Not supported %A" expr


                match queryExpr with
                | Source (Constant (value, Named (TypeCheck gpuArrayTypeDef _, [|_|])) as expr, sourceType, QueryExprType.Gpu) ->
                    let vars = context.VarExprs
                    let sourceTypeStr = typeToStr sourceType
                    let resultTypeStr = typeToStr context.ResultType
                    let gpuArraySource = value :?> IGpuArray
                    let sourceLength = gpuArraySource.Length
                    let structsStr = structsDefinitionStr context.Exprs
                    let exprsStr = context.Exprs
                                       |> List.map (fun expr -> sprintf' "%s;" (exprToStr expr vars))
                                       |> List.fold (fun first second -> sprintf' "%s%s%s" first Environment.NewLine second) ""
                    let varsStr = vars 
                                      |> List.filter (fun varExpr -> not (isAnonymousType varExpr.Type))
                                      |> List.map (fun varExpr -> sprintf' "%s %s;" (typeToStr varExpr.Type) (varExprToStr varExpr vars)) 
                                      |> List.fold (fun first second -> sprintf' "%s%s%s" first Environment.NewLine second) ""
                    match context.ReductionType with
                    | ReductionType.Map ->
                        let source = KernelTemplates.mapTemplate structsStr sourceTypeStr resultTypeStr varsStr (varExprToStr context.CurrentVarExpr vars) exprsStr (varExprToStr context.AccVarExpr vars)
                        { Source = source; ReductionType = context.ReductionType; Args = [| gpuArraySource |] }
                    | ReductionType.Filter ->
                        let source = KernelTemplates.mapFilterTemplate structsStr sourceTypeStr resultTypeStr varsStr (varExprToStr context.CurrentVarExpr vars) exprsStr (varExprToStr context.FlagVarExpr vars) (varExprToStr context.AccVarExpr vars) 
                        { Source = source; ReductionType = context.ReductionType; Args = [| (value :?> IGpuArray) |] }
                    | ReductionType.Sum | ReductionType.Count -> 
                        let gpuArray = value :?> IGpuArray
                        let source = KernelTemplates.reduceTemplate structsStr sourceTypeStr resultTypeStr resultTypeStr varsStr (varExprToStr context.CurrentVarExpr vars) exprsStr (varExprToStr context.AccVarExpr vars) "0" "+"
                        { Source = source; ReductionType = context.ReductionType; Args = [| gpuArray |] }
                    | _ -> failwithf "Not supported %A" context.ReductionType
                | ZipWith ((Constant (first, Named (TypeCheck gpuArrayTypeDef _, [|_|])) as firstExpr), 
                            (Constant (second, Named (TypeCheck gpuArrayTypeDef _, [|_|])) as secondExpr), Lambda ([firstParamExpr; secondParamExpr], bodyExpr)) ->
                    let vars = context.VarExprs @ [firstParamExpr; secondParamExpr]
                    let resultTypeStr = typeToStr context.ResultType
                    let firstGpuArray = first :?> IGpuArray
                    let secondGpuArray = second :?> IGpuArray
                    let sourceLength = firstGpuArray.Length
                    let structsStr = structsDefinitionStr context.Exprs
                    let exprsStr = context.Exprs
                                       |> List.map (fun expr -> sprintf' "%s;" (exprToStr expr vars))
                                       |> List.fold (fun first second -> sprintf' "%s%s%s" first Environment.NewLine second) ""
                    let varsStr = vars 
                                      |> List.filter (fun varExpr -> not (isAnonymousType varExpr.Type))
                                      |> List.map (fun varExpr -> sprintf' "%s %s;" (typeToStr varExpr.Type) (varExprToStr varExpr vars)) 
                                      |> List.fold (fun first second -> sprintf' "%s%s%s" first Environment.NewLine second) ""
                    match context.ReductionType with
                    | ReductionType.Map ->
                        let source = KernelTemplates.zip2Template structsStr (typeToStr firstGpuArray.Type) (typeToStr secondGpuArray.Type) resultTypeStr 
                                                                     varsStr (varExprToStr firstParamExpr vars) (varExprToStr secondParamExpr vars) 
                                                                     (varExprToStr context.CurrentVarExpr vars) (exprToStr bodyExpr vars)
                                                                     exprsStr (varExprToStr context.AccVarExpr vars)
                        { Source = source; ReductionType = context.ReductionType; Args = [| firstGpuArray; secondGpuArray |] }
                    | ReductionType.Filter ->
                        let source = KernelTemplates.zip2FilterTemplate structsStr (typeToStr firstGpuArray.Type) (typeToStr secondGpuArray.Type) resultTypeStr 
                                                                            varsStr (varExprToStr firstParamExpr vars) (varExprToStr secondParamExpr vars) 
                                                                            (varExprToStr context.CurrentVarExpr vars) (exprToStr bodyExpr vars)
                                                                            exprsStr (varExprToStr context.FlagVarExpr vars) (varExprToStr context.AccVarExpr vars)
                        { Source = source; ReductionType = context.ReductionType; Args = [| firstGpuArray; secondGpuArray |] }
                    | ReductionType.Sum | ReductionType.Count -> 
                        let source = KernelTemplates.zip2ReduceTemplate structsStr (typeToStr firstGpuArray.Type) (typeToStr secondGpuArray.Type) resultTypeStr resultTypeStr 
                                                                        varsStr (varExprToStr firstParamExpr vars) (varExprToStr secondParamExpr vars) 
                                                                        (varExprToStr context.CurrentVarExpr vars) (exprToStr bodyExpr vars) exprsStr (varExprToStr context.AccVarExpr vars) "0" "+"
                        { Source = source; ReductionType = context.ReductionType; Args = [| firstGpuArray; secondGpuArray |] }
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
                                InitExprs = []; AccExpr = empty; CombinerExpr = empty; ResultType = queryExpr.Type; 
                                VarExprs = [finalVarExpr; flagVarExpr]; Exprs = []; ReductionType = ReductionType.Map  }
                compile' queryExpr context
            | Filter (_) ->
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = finalVarExpr; FlagVarExpr = flagVarExpr;
                                BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = []; AccExpr = empty; CombinerExpr = empty; ResultType = queryExpr.Type; 
                                VarExprs = [finalVarExpr; flagVarExpr]; Exprs = []; ReductionType = ReductionType.Filter }
                compile' queryExpr context
            | Sum (queryExpr') ->
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = finalVarExpr; FlagVarExpr = flagVarExpr;
                                BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = []; AccExpr = empty; CombinerExpr = empty; ResultType = queryExpr.Type; 
                                VarExprs = [finalVarExpr; flagVarExpr]; Exprs = []; ReductionType = ReductionType.Sum }
                compile' queryExpr' context
            | Count (queryExpr') ->
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = finalVarExpr; FlagVarExpr = flagVarExpr;
                                BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = []; AccExpr = empty; CombinerExpr = empty; ResultType = queryExpr.Type; 
                                VarExprs = [finalVarExpr; flagVarExpr]; Exprs = []; ReductionType = ReductionType.Count }
                compile' (Transform ((lambda [|var "___empty___" queryExpr'.Type|] (constant 1)),queryExpr')) context
            | ToArray (queryExpr') -> compile queryExpr'
            | ZipWith (_, _, _) ->
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = finalVarExpr; FlagVarExpr = flagVarExpr;
                                BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = []; AccExpr = empty; CombinerExpr = empty; ResultType = queryExpr.Type; 
                                VarExprs = [finalVarExpr; flagVarExpr]; Exprs = []; ReductionType = ReductionType.Map  }
                compile' queryExpr context
            | _ -> failwithf "Not supported %A" queryExpr 

