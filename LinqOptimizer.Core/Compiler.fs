namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection


    module internal Compiler =
        let breakLabel n = label "break"
        let continueLabel n = label "continue"
        let lookup name (varExprs : ParameterExpression list) =
            varExprs |> List.find (fun varExpr -> varExpr.Name = name)  

        type QueryContext = { BreakLabel : LabelTarget; ContinueLabel : LabelTarget;
                                InitExpr : Expression; AccExpr : Expression; ReturnExpr : Expression; 
                                VarExprs : ParameterExpression list; Exprs : Expression list }

        let compile (queryExpr : QueryExpr) : Expression = 
            let rec compile' (queryExpr : QueryExpr) (context : QueryContext) : Expression =
                let current = List.head context.VarExprs
                match queryExpr with
                | Source expr ->
                        let indexVarExpr = var "___index___" typeof<int>
                        let arrayVarExpr = var "___array___" expr.Type
                        let arrayAssignExpr = assign arrayVarExpr expr
                        let indexAssignExpr = assign indexVarExpr (constant -1) 
                        let lengthExpr = arrayLength arrayVarExpr 
                        let getItemExpr = arrayIndex arrayVarExpr indexVarExpr
                        let exprs' = assign current getItemExpr :: context.Exprs
                        let checkBoundExpr = equal indexVarExpr lengthExpr 
                        let brachExpr = ``if`` checkBoundExpr (``break`` context.BreakLabel) (block [] exprs') 
                        let loopExpr = loop (block [] [addAssign indexVarExpr (constant 1); brachExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel 
                        block (arrayVarExpr :: indexVarExpr :: context.VarExprs) [context.InitExpr; arrayAssignExpr; indexAssignExpr; loopExpr; context.ReturnExpr] :> _

                | Transform (Lambda ([paramExpr], bodyExpr), queryExpr', _) ->
                    let exprs' = assign current bodyExpr :: context.Exprs
                    compile' queryExpr' { context with VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' }
                | Filter (Lambda ([paramExpr], bodyExpr), queryExpr', _) ->
                    let exprs' = ``if`` bodyExpr empty (``continue`` context.ContinueLabel) :: assign current paramExpr :: context.Exprs
                    compile' queryExpr' { context with VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' }
                | NestedQuery ((paramExpr, nestedQueryExpr), queryExpr', t) ->
                    let context' = { BreakLabel = breakLabel 0; ContinueLabel = continueLabel 0; 
                                        InitExpr = empty; AccExpr = context.AccExpr; ReturnExpr = empty; 
                                        VarExprs = [current]; Exprs = context.Exprs }

                    let expr = compile' nestedQueryExpr context'
                    compile' queryExpr' { context with AccExpr = empty; VarExprs = paramExpr :: context.VarExprs; Exprs = [expr] }

                | NestedQueryTransform ((paramExpr, nestedQueryExpr), Lambda ([valueExpr; colExpr], bodyExpr), queryExpr', t) ->
                    let context' = { BreakLabel = breakLabel 0; ContinueLabel = continueLabel 0; 
                                        InitExpr = empty; AccExpr = context.AccExpr; ReturnExpr = empty; 
                                        VarExprs = [valueExpr]; Exprs = assign colExpr paramExpr :: assign current bodyExpr :: context.Exprs }

                    let expr = compile' nestedQueryExpr context'
                    compile' queryExpr' { context with AccExpr = empty; VarExprs = paramExpr :: valueExpr :: colExpr :: context.VarExprs; Exprs = [expr] }
                | _ -> failwithf "Invalid state %A" queryExpr 


            match queryExpr with
            | Sum (queryExpr', t) ->
                let finalVarExpr = var "___final___" t
                let accVarExpr = var "___acc___" t
                let initExpr = assign accVarExpr (``default`` t)
                let accExpr = addAssign accVarExpr finalVarExpr
                let context = { BreakLabel = breakLabel 0; ContinueLabel = continueLabel 0; 
                                InitExpr = initExpr; AccExpr = accExpr; ReturnExpr = accVarExpr; 
                                VarExprs = [finalVarExpr; accVarExpr]; Exprs = [] }
                let expr = compile' queryExpr' context
                expr
            | Aggregate ((seed, t), Lambda ([accVarExpr; varExpr], bodyExpr), queryExpr') ->
                let initExpr = assign accVarExpr (constant seed)
                let accExpr = assign accVarExpr bodyExpr
                let context = { BreakLabel = breakLabel 0; ContinueLabel = continueLabel 0; 
                                InitExpr = initExpr; AccExpr = accExpr; ReturnExpr = accVarExpr; 
                                VarExprs = [varExpr; accVarExpr]; Exprs = [] }
                let expr = compile' queryExpr' context
                expr
            | Transform (_, _, t) | Filter (_, _, t) ->
                let listType = typedefof<List<_>>.MakeGenericType [| t |]
                let finalVarExpr = var "___final___" t
                let accVarExpr = var "___acc___" listType
                let initExpr = assign accVarExpr (``new`` listType)
                let accExpr =  call (listType.GetMethod("Add")) accVarExpr [finalVarExpr]
                let context = { BreakLabel = breakLabel 0; ContinueLabel = continueLabel 0; 
                                InitExpr = initExpr; AccExpr = accExpr; ReturnExpr = accVarExpr; 
                                VarExprs = [finalVarExpr; accVarExpr]; Exprs = [] }
                let expr = compile' queryExpr context
                expr
            | _ -> failwithf "Invalid state %A" queryExpr 



        let rec toQueryExpr (expr : Expression) : QueryExpr =
                match expr with
                | MethodCall (_, MethodName "Select" _, [expr'; Lambda ([_], bodyExpr) as f']) -> 
                    Transform (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)
                | MethodCall (_, MethodName "Where" _, [expr'; Lambda ([paramExpr], _) as f']) -> 
                    Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
                | MethodCall (_, MethodName "SelectMany" m, [expr'; Lambda ([paramExpr], bodyExpr)]) -> 
                    NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr', m.ReturnType.GetGenericArguments().[0])
                | _ -> Source expr

