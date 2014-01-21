namespace LinqOptimizer.Gpu
    open System
    open System.Linq.Expressions
    open System.Runtime.InteropServices
    open LinqOptimizer.Core


    module internal Compiler =
        type Length = int
        type Size = int
        type CompilerResult = { Source : string; ReductionType : ReductionType; Args : (obj * Type * Length * Size) [] }
        
        let intType = typeof<int>
        let floatType = typeof<float>
        let doubleType = typeof<double>
        let byteType = typeof<byte>

        let breakLabel () = labelTarget "break"
        let continueLabel () = labelTarget "continue"

        let compile (queryExpr : QueryExpr) : CompilerResult = 
            let typeToStr (t : Type) = 
                match t with
                | TypeCheck intType _ -> "int"
                | TypeCheck floatType _ -> "float"
                | TypeCheck doubleType _ -> "float"
                | TypeCheck byteType _ -> "byte"
                | _ -> failwithf "Not supported %A" t
            let varExprToStr (varExpr : ParameterExpression) = varExpr.ToString()

            let rec exprToStr (expr : Expression) =
                match expr with
                | Constant (value, TypeCheck intType _) -> sprintf "%A" value
                | Constant (value, TypeCheck floatType _) -> sprintf "%A" value
                | Constant (value, TypeCheck doubleType _) -> sprintf "%A" value
                | Constant (value, TypeCheck byteType _) -> sprintf "%A" value
                | Parameter(paramExpr) -> varExprToStr paramExpr
                | Assign (Parameter (paramExpr), expr') -> sprintf "%s = %s" (varExprToStr paramExpr) (exprToStr expr')
                | Plus (leftExpr, rightExpr) -> sprintf "(%s + %s)" (exprToStr leftExpr) (exprToStr rightExpr)
                | Times (leftExpr, rightExpr) -> sprintf "(%s * %s)" (exprToStr leftExpr) (exprToStr rightExpr)
                | _ -> failwithf "Not supported %A" expr

            let rec compile' (queryExpr : QueryExpr) (context : QueryContext) =
                match queryExpr with
                | Source (Constant (value, Array (_, 1)) as expr, t, QueryExprType.Gpu) ->
                    match context.ReductionType with
                    | ReductionType.Map ->
                        let kernelTemplate = sprintf "
                            __kernel void kernelCode(__global %s* ___input___, __global %s* ___result___)
                            {
                                %s
                                int ___id___ = get_global_id(0);
                                %s = ___input___[___id___];
                                %s
                                ___result___[___id___] = %s;
                            }"
                        let sourceTypeStr = typeToStr t
                        let resultType = context.CurrentVarExpr.Type
                        let resultTypeStr = typeToStr resultType
                        let sourceLength = (value :?> Array).Length
                        let resultArray = Array.CreateInstance(resultType, sourceLength) :> obj
                        let varsStr = context.VarExprs 
                                      |> List.map (fun varExpr -> sprintf "%s %s;" (typeToStr varExpr.Type) (varExprToStr varExpr)) 
                                      |> List.reduce (fun first second -> sprintf "%s%s%s" first Environment.NewLine second)
                        let exprsStr = context.Exprs
                                       |> List.map (fun expr -> sprintf "%s;" (exprToStr expr))
                                       |> List.reduce (fun first second -> sprintf "%s%s%s" first Environment.NewLine second)
                        let source = kernelTemplate sourceTypeStr resultTypeStr varsStr (varExprToStr context.CurrentVarExpr) exprsStr (varExprToStr context.AccVarExpr)
                        { Source = source; ReductionType = context.ReductionType; Args = [| (value, t, sourceLength, Marshal.SizeOf(t)); 
                                                                                            (resultArray, resultType, sourceLength, Marshal.SizeOf(resultType)) |] }
                    | _ -> failwithf "Not supported %A" queryExpr 
                | Transform (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                    let exprs' = assign context.CurrentVarExpr bodyExpr :: context.Exprs
                    compile' queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' } 
                | _ -> failwithf "Not supported %A" queryExpr 

            match queryExpr with
            | Transform (_) ->
                let t = queryExpr.Type
                let finalVarExpr = var "___final___" t
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = finalVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = []; AccExpr = empty; CombinerExpr = empty; ReturnExpr = empty; 
                                VarExprs = [finalVarExpr]; Exprs = []; ReductionType = ReductionType.Map  }
                compile' queryExpr context
            | Filter (_) ->
                let context : QueryContext = raise <| new NotImplementedException()
                compile' queryExpr context
            | Sum (_) ->
                let context : QueryContext = raise <| new NotImplementedException()
                compile' queryExpr context
            | Count (_) ->
                let context : QueryContext = raise <| new NotImplementedException()
                compile' queryExpr context
            | _ -> failwithf "Not supported %A" queryExpr 

