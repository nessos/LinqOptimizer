namespace LinqOptimizer.Gpu
    open System
    open System.Linq.Expressions
    open LinqOptimizer.Core


    module internal Compiler =
        
        let breakLabel () = labelTarget "break"
        let continueLabel () = labelTarget "continue"
        
       
        let compile (queryExpr : QueryExpr) : string = 
            let typeToStr (t : Type) = 
                match t.Name with
                | "Int32" -> "int"
                | "Single" -> "float"
                | "Double" -> "float"
                | "Byte" -> "byte"
                | _ -> failwithf "Not supported %A" t
            let varExprToStr (varExpr : ParameterExpression) = varExpr.ToString()
            let rec compile' (queryExpr : QueryExpr) (context : QueryContext) =
                match queryExpr with
                | Source (Constant (ExprType (Array (_, 1))) as expr, t, QueryExprType.Gpu) ->
                    match context.ReductionType with
                    | ReductionType.Map ->
                        let kernelTemplate = sprintf "
                            __kernel void kernel(__global %s* ___input___, __global %s* ___result___)
                            {
                                %s
                                int ___id___ = get_global_id(0);
                                %s = ___input___[___id___];
                                %s
                                ___result___[___id___] = %s;
                            }"
                        let sourceType = typeToStr t
                        let resultType = typeToStr context.CurrentVarExpr.Type
                        kernelTemplate "" sourceType resultType (varExprToStr context.CurrentVarExpr) "" (varExprToStr context.AccVarExpr)
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

