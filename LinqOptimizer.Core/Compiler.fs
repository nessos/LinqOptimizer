namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection


    module internal Compiler =
        let listTypeDef = typedefof<List<_>>

        let breakLabel () = label "break"
        let continueLabel () = label "continue"
        let lookup name (varExprs : ParameterExpression list) =
            varExprs |> List.find (fun varExpr -> varExpr.Name = name)  

        type QueryContext = { CurrentVarExpr : Expression; BreakLabel : LabelTarget; ContinueLabel : LabelTarget;
                                InitExprs : Expression list; AccExpr : Expression; ReturnExpr : Expression; 
                                VarExprs : ParameterExpression list; Exprs : Expression list }

        let compileToSequential (queryExpr : QueryExpr) : Expression = 
            let rec compile' (queryExpr : QueryExpr) (context : QueryContext) : Expression =
                match queryExpr with
                | Source (ExprType (Array (_, 1)) as expr, t) ->
                        let indexVarExpr = var "___index___" typeof<int>
                        let arrayVarExpr = var "___array___" expr.Type
                        let arrayAssignExpr = assign arrayVarExpr expr
                        let indexAssignExpr = assign indexVarExpr (constant -1) 
                        let lengthExpr = arrayLength arrayVarExpr 
                        let getItemExpr = arrayIndex arrayVarExpr indexVarExpr
                        let exprs' = assign context.CurrentVarExpr getItemExpr :: context.Exprs
                        let checkBoundExpr = equal indexVarExpr lengthExpr 
                        let brachExpr = ``if`` checkBoundExpr (``break`` context.BreakLabel) (block [] exprs') 
                        let loopExpr = loop (block [] [addAssign indexVarExpr (constant 1); brachExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel 
                        block (arrayVarExpr :: indexVarExpr :: context.VarExprs) [block [] context.InitExprs; arrayAssignExpr; indexAssignExpr; loopExpr; context.ReturnExpr] 
                | Source (ExprType (Named (TypeCheck listTypeDef _, [|_|])) as expr, t) ->
                        let indexVarExpr = var "___index___" typeof<int>
                        let listVarExpr = var "___list___" expr.Type
                        let listAssignExpr = assign listVarExpr expr
                        let indexAssignExpr = assign indexVarExpr (constant -1) 
                        let lengthExpr = call (expr.Type.GetMethod("get_Count")) listVarExpr []
                        let getItemExpr = call (expr.Type.GetMethod("get_Item")) listVarExpr [indexVarExpr]
                        let exprs' = assign context.CurrentVarExpr getItemExpr :: context.Exprs
                        let checkBoundExpr = equal indexVarExpr lengthExpr 
                        let brachExpr = ``if`` checkBoundExpr (``break`` context.BreakLabel) (block [] exprs') 
                        let loopExpr = loop (block [] [addAssign indexVarExpr (constant 1); brachExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel 
                        block (listVarExpr :: indexVarExpr :: context.VarExprs) [block [] context.InitExprs; listAssignExpr; indexAssignExpr; loopExpr; context.ReturnExpr] 
                | Source (expr, t) -> // general case for IEnumerable
                        let enumerableType = typedefof<IEnumerable<_>>.MakeGenericType [| t |]
                        let enumeratorType = typedefof<IEnumerator<_>>.MakeGenericType [| t |]
                        let disposableVarExpr = var "___disposable___" typeof<IDisposable>
                        let enumeratorVarExpr = var "___enumerator___" enumeratorType
                        let enumeratorAssignExpr = assign enumeratorVarExpr (call (enumerableType.GetMethod("GetEnumerator")) expr [])
                        let disposableAssignExpr = assign disposableVarExpr enumeratorVarExpr 
                        let getItemExpr = call (enumeratorType.GetMethod("get_Current")) enumeratorVarExpr []
                        let exprs' = assign context.CurrentVarExpr getItemExpr :: context.Exprs
                        let checkBoundExpr = equal (call (typeof<IEnumerator>.GetMethod("MoveNext")) enumeratorVarExpr []) (constant false)
                        let brachExpr = ``if`` checkBoundExpr (``break`` context.BreakLabel) (block [] exprs') 
                        let loopExpr = tryfinally (loop (block [] [brachExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel) (call (typeof<IDisposable>.GetMethod("Dispose")) disposableVarExpr [])
                        block (enumeratorVarExpr :: disposableVarExpr :: context.VarExprs) [block [] context.InitExprs; enumeratorAssignExpr; disposableAssignExpr; loopExpr; context.ReturnExpr] 
                | Transform (Lambda ([paramExpr], bodyExpr), queryExpr', _) ->
                    let exprs' = assign context.CurrentVarExpr bodyExpr :: context.Exprs
                    compile' queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' }
                | TransformIndexed (Lambda ([paramExpr; indexExpr], bodyExpr), queryExpr', _) ->
                    let exprs' = addAssign indexExpr (constant 1) :: assign context.CurrentVarExpr bodyExpr :: context.Exprs
                    compile' queryExpr' { context with CurrentVarExpr = paramExpr; InitExprs = assign indexExpr (constant -1) :: context.InitExprs; VarExprs = paramExpr :: indexExpr :: context.VarExprs; Exprs = exprs' }
                | Filter (Lambda ([paramExpr], bodyExpr), queryExpr', _) ->
                    let exprs' = ``if`` bodyExpr empty (``continue`` context.ContinueLabel) :: assign context.CurrentVarExpr paramExpr :: context.Exprs
                    compile' queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' }
                | FilterIndexed (Lambda ([paramExpr; indexExpr], bodyExpr), queryExpr', _) ->
                    let exprs' = addAssign indexExpr (constant 1) :: ``if`` bodyExpr empty (``continue`` context.ContinueLabel) :: assign context.CurrentVarExpr paramExpr :: context.Exprs
                    compile' queryExpr' { context with CurrentVarExpr = paramExpr; InitExprs = assign indexExpr (constant -1) :: context.InitExprs; VarExprs = indexExpr :: paramExpr :: context.VarExprs; Exprs = exprs' }
                | Take (countExpr, queryExpr', _) ->
                    let countVarExpr = var "___takeCount___" typeof<int> //special "local" variable for Take
                    let exprs' = addAssign countVarExpr (constant 1) :: ``if`` (greaterThan countVarExpr countExpr) (``break`` context.BreakLabel) empty :: context.Exprs
                    compile' queryExpr' { context with InitExprs = assign countVarExpr (constant 0) :: context.InitExprs ; VarExprs = countVarExpr :: context.VarExprs; Exprs = exprs' }
                | Skip (countExpr, queryExpr', _) ->
                    let countVarExpr = var "___skipCount___" typeof<int> //special "local" variable for Skip
                    let exprs' = addAssign countVarExpr (constant 1) :: ``if`` (lessThanOrEqual countVarExpr countExpr) (``continue`` context.ContinueLabel) empty :: context.Exprs
                    compile' queryExpr' { context with InitExprs = assign countVarExpr (constant 0) :: context.InitExprs ; VarExprs = countVarExpr :: context.VarExprs; Exprs = exprs' }
                | NestedQuery ((paramExpr, nestedQueryExpr), queryExpr', t) ->
                    let context' = { CurrentVarExpr = context.CurrentVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                        InitExprs = [empty]; AccExpr = context.AccExpr; ReturnExpr = empty; 
                                        VarExprs = []; Exprs = context.Exprs }

                    let expr = compile' nestedQueryExpr context'
                    compile' queryExpr' { context with CurrentVarExpr = paramExpr; AccExpr = empty; VarExprs = paramExpr :: context.VarExprs; Exprs = [expr] }
                | NestedQueryTransform ((paramExpr, nestedQueryExpr), Lambda ([valueExpr; colExpr], bodyExpr), queryExpr', t) ->
                    let context' = { CurrentVarExpr = valueExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                        InitExprs = [empty]; AccExpr = context.AccExpr; ReturnExpr = empty; 
                                        VarExprs = []; Exprs = assign colExpr paramExpr :: assign context.CurrentVarExpr bodyExpr :: context.Exprs }

                    let expr = compile' nestedQueryExpr context'
                    compile' queryExpr' { context with CurrentVarExpr = paramExpr; AccExpr = empty; VarExprs = paramExpr :: valueExpr :: colExpr :: context.VarExprs; Exprs = [expr] }
                | GroupBy (Lambda ([paramExpr], bodyExpr) as lambdaExpr, queryExpr', _) ->
                    let listType = listTypeDef.MakeGenericType [| queryExpr'.Type |]
                    let finalVarExpr, accVarExpr  = var "___final___" queryExpr'.Type, var "___acc___" listType
                    let initExpr, accExpr = assign accVarExpr (``new`` listType), call (listType.GetMethod("Add")) accVarExpr [finalVarExpr]
                    let context' = { CurrentVarExpr = finalVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                        InitExprs = [initExpr]; AccExpr = accExpr; ReturnExpr = empty; 
                                        VarExprs = [finalVarExpr]; Exprs = [] }
                    let expr = compile' queryExpr' context'
                    let groupByMethodInfo = typeof<Enumerable>.GetMethods()
                                                |> Array.find (fun methodInfo -> methodInfo.Name = "GroupBy" && methodInfo.GetParameters().Length = 2) // TODO: reflection type checks
                                                |> (fun methodInfo -> methodInfo.MakeGenericMethod [|paramExpr.Type; bodyExpr.Type|])
                    let groupingType = typedefof<IGrouping<_, _>>.MakeGenericType [|paramExpr.Type; bodyExpr.Type|]
                    let groupByCallExpr = call groupByMethodInfo null [accVarExpr; lambdaExpr]
                    let expr' = compile' (Source (groupByCallExpr, groupingType)) context
                    block [accVarExpr] [expr; expr']
                | OrderBy (Lambda ([paramExpr], bodyExpr) as lambdaExpr, order, queryExpr', t) ->
                    let listType = listTypeDef.MakeGenericType [| queryExpr'.Type |]
                    let finalVarExpr, accVarExpr  = var "___final___" queryExpr'.Type, var "___acc___" listType
                    let initExpr, accExpr = assign accVarExpr (``new`` listType), call (listType.GetMethod("Add")) accVarExpr [finalVarExpr]
                    let context' = { CurrentVarExpr = finalVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                        InitExprs = [initExpr]; AccExpr = accExpr; ReturnExpr = empty; 
                                        VarExprs = [finalVarExpr]; Exprs = [] }
                    let expr = compile' queryExpr' context'
                    let methodName = match order with Ascending -> "OrderBy" | Descending -> "OrderByDescending"
                    let orderByMethodInfo = typeof<Enumerable>.GetMethods()
                                                |> Array.find (fun methodInfo -> methodInfo.Name = methodName && methodInfo.GetParameters().Length = 2) // TODO: reflection type checks
                                                |> (fun methodInfo -> methodInfo.MakeGenericMethod [|paramExpr.Type; bodyExpr.Type|])
                    let orderByCallExpr = call orderByMethodInfo null [accVarExpr; lambdaExpr]
                    let expr' = compile' (Source (orderByCallExpr, t)) context
                    block [accVarExpr] [expr; expr']
                | _ -> failwithf "Invalid state %A" queryExpr 


            match queryExpr with
            | Sum (queryExpr', t) ->
                let finalVarExpr = var "___final___" t
                let accVarExpr = var "___acc___" t
                let initExpr = assign accVarExpr (``default`` t)
                let accExpr = addAssign accVarExpr finalVarExpr
                let context = { CurrentVarExpr = finalVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [initExpr]; AccExpr = accExpr; ReturnExpr = accVarExpr; 
                                VarExprs = [finalVarExpr; accVarExpr]; Exprs = [] }
                let expr = compile' queryExpr' context
                expr 
            | Count (queryExpr', t) ->
                let accVarExpr = var "___cnt___" typeof<int>
                let initExpr = assign accVarExpr (constant 0)
                let accExpr = addAssign accVarExpr (constant 1)
                let tmpVarExpr = var "___tmp___" t
                let context = { CurrentVarExpr = tmpVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [initExpr]; AccExpr = accExpr; ReturnExpr = accVarExpr; 
                                VarExprs = [tmpVarExpr; accVarExpr]; Exprs = [] }
                let expr = compile' queryExpr' context
                expr 
            | Aggregate ((seed, t), Lambda ([accVarExpr; varExpr], bodyExpr), queryExpr') ->
                let initExpr = assign accVarExpr (constant seed)
                let accExpr = assign accVarExpr bodyExpr
                let context = { CurrentVarExpr = varExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [initExpr]; AccExpr = accExpr; ReturnExpr = accVarExpr; 
                                VarExprs = [varExpr; accVarExpr]; Exprs = [] }
                let expr = compile' queryExpr' context
                expr
            | ForEach (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                let context = { CurrentVarExpr = paramExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [assign paramExpr (``default`` paramExpr.Type)]; AccExpr = bodyExpr; ReturnExpr = empty; 
                                VarExprs = [paramExpr]; Exprs = [] }
                let expr = compile' queryExpr' context
                expr
            | queryExpr' ->
                let listType = listTypeDef.MakeGenericType [| queryExpr'.Type |]
                let finalVarExpr, accVarExpr  = var "___final___" queryExpr'.Type, var "___acc___" listType
                let initExpr, accExpr = assign accVarExpr (``new`` listType), call (listType.GetMethod("Add")) accVarExpr [finalVarExpr]
                let context = { CurrentVarExpr = finalVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [initExpr]; AccExpr = accExpr; ReturnExpr = accVarExpr; 
                                VarExprs = [finalVarExpr; accVarExpr]; Exprs = [] }
                let expr = compile' queryExpr context
                expr
            | _ -> failwithf "Invalid state %A" queryExpr 

        let compileToParallel (queryExpr : QueryExpr) : Expression =
            raise <| new NotImplementedException() 

        let rec toQueryExpr (expr : Expression) : QueryExpr =
            // TODO: expr type checks
            match expr with
            | MethodCall (_, MethodName "Select" _, [expr'; Lambda ([_], bodyExpr) as f']) -> 
                Transform (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)
            | MethodCall (_, MethodName "Select" _, [expr'; Lambda ([_; _], bodyExpr) as f']) -> 
                TransformIndexed (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)
            | MethodCall (_, MethodName "Where" _, [expr'; Lambda ([paramExpr], _) as f']) -> 
                Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
            | MethodCall (_, MethodName "Where" _, [expr'; Lambda ([paramExpr; indexExpr], _) as f']) -> 
                FilterIndexed (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
            | MethodCall (_, MethodName "Take" _, [expr'; countExpr]) -> 
                let queryExpr = toQueryExpr expr'
                Take (countExpr, queryExpr, queryExpr.Type)
            | MethodCall (_, MethodName "Skip" _, [expr'; countExpr]) -> 
                let queryExpr = toQueryExpr expr'
                Skip (countExpr, queryExpr, queryExpr.Type)
            | MethodCall (_, MethodName "SelectMany" m, [expr'; Lambda ([paramExpr], bodyExpr)]) -> 
                NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr', m.ReturnType.GetGenericArguments().[0])
            | MethodCall (_, MethodName "GroupBy" _, [expr'; Lambda ([paramExpr], bodyExpr) as f']) -> 
                GroupBy (f' :?> LambdaExpression, toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|paramExpr.Type; bodyExpr.Type|])
            | MethodCall (_, MethodName "OrderBy" _, [expr'; Lambda ([paramExpr], bodyExpr) as f']) -> 
                OrderBy (f' :?> LambdaExpression, Order.Ascending, toQueryExpr expr', paramExpr.Type)
            | _ -> 
                if expr.Type.IsArray then
                    Source (expr, expr.Type.GetElementType())
                else
                    Source (expr, expr.Type.GetGenericArguments().[0])


