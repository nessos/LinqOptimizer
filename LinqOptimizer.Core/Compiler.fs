namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection


    module internal Compiler =
        let lookup name (varExprs : ParameterExpression list) =
            varExprs |> List.find (fun varExpr -> varExpr.Name = name)
        let getMethod (instance : obj) (methodName : string) =
            if instance = null then raise <| new ArgumentNullException("instance")
            instance.GetType().GetMethod(methodName)  

        type CompilerContext = { InitExpr : Expression; AccExpr : Expression; ReturnExpr : Expression 
                                 VarExprs : ParameterExpression list; Exprs : Expression list}

        let compile (queryExpr : QueryExpr) : Expression = 
            let rec compile' (queryExpr : QueryExpr) (context : CompilerContext) : Expression =
                let current = lookup "current" context.VarExprs
                match queryExpr with
                | Source (:? Array as array, t) ->
                        let breakLabel = label "break"
                        let continueLabel = label "continue"
                        let indexVarExpr = var "___index___" typeof<int>
                        let arrayVarExpr = var "___array___" (array.GetType())
                        let arrayAssignExpr = assign arrayVarExpr (constant array)
                        let indexAssignExpr = assign indexVarExpr (constant -1) 
                        let lengthExpr = arrayLength arrayVarExpr 
                        let getItemExpr = arrayIndex arrayVarExpr indexVarExpr
                        let exprs' = assign current getItemExpr :: context.Exprs
                        let checkBoundExpr = equal indexVarExpr lengthExpr 
                        let brachExpr = ``if`` checkBoundExpr (``break`` breakLabel) (block [] exprs') 
                        let loopExpr = loop (block [] [addAssign indexVarExpr (constant 1); brachExpr; context.AccExpr]) breakLabel continueLabel 
                        block (arrayVarExpr :: indexVarExpr :: context.VarExprs) [context.InitExpr; arrayAssignExpr; indexAssignExpr; loopExpr; context.ReturnExpr] :> _

                | Transform (Lambda ([paramExpr], body), queryExpr') ->
                        let exprs' = assign paramExpr current :: assign current body :: context.Exprs
                        compile' queryExpr' { context with VarExprs = (paramExpr :: context.VarExprs); Exprs = exprs' }
                | Filter (Lambda ([paramExpr], body), queryExpr') ->
                    let exprs' = assign paramExpr current :: body :: context.Exprs
                    compile' queryExpr' { context with VarExprs = (paramExpr :: context.VarExprs); Exprs = exprs' }
                | _ -> failwithf "Invalid state %A" queryExpr 

            match queryExpr with
            | Sum (queryExpr', t) ->
                let finalVarExpr = var "current" t
                let accVarExpr = var "___acc___" t
                let initExpr = assign accVarExpr (``default`` t)
                let accExpr = addAssign accVarExpr finalVarExpr
                let context = { InitExpr = initExpr; AccExpr = accExpr; ReturnExpr = accVarExpr; VarExprs = [accVarExpr; finalVarExpr]; Exprs = [] }
                let expr = compile' queryExpr' context
                expr
            | _ -> failwithf "Invalid state %A" queryExpr 

