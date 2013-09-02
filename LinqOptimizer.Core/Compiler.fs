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

        let compile (queryExpr : QueryExpr) : Expression = 
            let rec compile' (queryExpr : QueryExpr)
                            (initExpr : Expression)
                            (accExpr : Expression)
                            (returnExpr :  Expression)
                            (varExprs : ParameterExpression list) 
                            (exprs : Expression list) : Expression =
                let current = lookup "current" varExprs
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
                        let exprs' = assign current getItemExpr :: exprs
                        let checkBoundExpr = equal indexVarExpr lengthExpr 
                        let brachExpr = ``if`` checkBoundExpr (``break`` breakLabel) (block [] exprs') 
                        let loopExpr = loop (block [] [addAssign indexVarExpr (constant 1); brachExpr; accExpr]) breakLabel continueLabel 
                        block (arrayVarExpr :: indexVarExpr :: varExprs) [initExpr; arrayAssignExpr; indexAssignExpr; loopExpr; returnExpr] :> _

                | Transform (Lambda ([paramExpr], body), queryExpr') ->
                        let exprs' = assign paramExpr current :: assign current body :: exprs
                        compile' queryExpr' initExpr accExpr returnExpr (paramExpr :: varExprs) exprs'
                | Filter (Lambda ([paramExpr], body), queryExpr') ->
                    let exprs' = assign paramExpr current :: body :: exprs
                    compile' queryExpr' initExpr accExpr returnExpr (paramExpr :: varExprs) exprs'
                | _ -> failwithf "Invalid state %A" queryExpr 

            match queryExpr with
            | Sum (queryExpr', t) ->
                let finalVarExpr = var "current" t
                let accVarExpr = var "___acc___" t
                let initExpr = assign accVarExpr (``default`` t)
                let accExpr = addAssign accVarExpr finalVarExpr
                let expr = compile' queryExpr' initExpr accExpr accVarExpr [accVarExpr; finalVarExpr] []
                expr
            | _ -> failwithf "Invalid state %A" queryExpr 

