namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    module internal Compiler =
        let arrayTypeDef = typedefof<_[]>
        let interfaceListTypeDef = typedefof<IList<_>>
        let partitionerTypeDef = typedefof<Partitioner<_>>

        type QueryContext = { CurrentVarExpr : ParameterExpression; AccVarExpr : ParameterExpression; 
                                BreakLabel : LabelTarget; ContinueLabel : LabelTarget;
                                InitExprs : Expression list; AccExpr : Expression; CombinerExpr : Expression; ReturnExpr : Expression; 
                                VarExprs : ParameterExpression list; Exprs : Expression list; ReductionType : ReductionType }

        let breakLabel = 
            let x = ref -1
            fun () -> 
                incr x
                labelTarget (sprintf "break%d" !x)
        let continueLabel =
            let x = ref -1
            fun () -> 
                incr x
                labelTarget (sprintf "continue%d" !x)

        let lookup name (varExprs : ParameterExpression list) =
            varExprs |> List.find (fun varExpr -> varExpr.Name = name)  

        let collectorType = typedefof<ArrayCollector<_>>
        let toListContext (queryExpr : QueryExpr) =
            let listType = collectorType.MakeGenericType [| queryExpr.Type |]
            let finalVarExpr, accVarExpr  = var "___final___" queryExpr.Type, var "___acc___" listType
            let initExpr, accExpr = assign accVarExpr (``new`` listType), call (listType.GetMethod("Add")) accVarExpr [finalVarExpr]
            let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = accVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                            InitExprs = [initExpr]; AccExpr = accExpr; CombinerExpr = empty; ReturnExpr = accVarExpr; 
                            VarExprs = [finalVarExpr; accVarExpr]; Exprs = []; ReductionType = ReductionType.ToList }
            context
        
        type KeyValueCollectRecord = { KeyVarArrayExpr : ParameterExpression; 
                                       ValueVarArrayExpr : ParameterExpression;
                                       LoopExpr : Expression; KeyType : Type  }
        let collectKeyValueArrays (listExpr : Expression) (lambdaExprs : LambdaExpression list) (orders : Order list) : KeyValueCollectRecord = 
                let valueType, keyType = 
                    match lambdaExprs with
                    | [Lambda ([paramExpr], bodyExpr)] -> paramExpr.Type, bodyExpr.Type
                    | [Lambda ([paramExpr2], bodyExpr2); Lambda ([paramExpr1], bodyExpr1)] -> 
                        paramExpr1.Type, typedefof<Keys<int, int>>.MakeGenericType [|bodyExpr1.Type; bodyExpr2.Type|]
                    | Lambda ([paramExprn], bodyExprn) :: Lambda ([paramExprnm1], bodyExprnm1) :: rest ->
                        let accKeyType = typedefof<Keys<int, int>>.MakeGenericType [|bodyExprnm1.Type; bodyExprn.Type|]
                        let keyType = List.fold (fun keyType (lambda : LambdaExpression) -> typedefof<Keys<int, int>>.MakeGenericType [| lambda.Body.Type; keyType |]) 
                                                accKeyType rest
                        paramExprn.Type, keyType
                    | _ -> failwithf "Invalid state, keys %A" lambdaExprs

                let loopBreak = breakLabel()
                let loopContinue = continueLabel()
                let listVarExpr = var "___listVar___" listExpr.Type
                let indexVarExpr = var "___index___" typeof<int>
                let listAssignExpr = assign listVarExpr listExpr
                let indexAssignExpr = assign indexVarExpr (constant -1) 
                
                let collectorType = collectorType.MakeGenericType [| valueType |]
                let velueArrayExpr = call (collectorType.GetMethod "ToArray") listVarExpr []
                
                let keyVarArrayExpr = var "___keys___" (keyType.MakeArrayType())
                let valueVarArrayExpr = var "___values___" (valueType.MakeArrayType())
                                
                let lengthExpr = Expression.ArrayLength(valueVarArrayExpr)
                let getItemExpr = Expression.ArrayAccess(valueVarArrayExpr, indexVarExpr)


                let initKeyArrayExpr = assign keyVarArrayExpr (arrayNew keyType lengthExpr)
                let initValueArrayExpr =  assign valueVarArrayExpr velueArrayExpr

                let accessKeyArrayExpr = arrayAccess keyVarArrayExpr indexVarExpr 
                let accessValueArrayExpr = arrayAccess valueVarArrayExpr indexVarExpr 
  
                // collect keys and values assignments
                let exprs' = 
                    match lambdaExprs, orders with 
                    | [Lambda ([paramExpr], bodyExpr)], _ -> [assign paramExpr getItemExpr; assign accessKeyArrayExpr bodyExpr]
                    | [Lambda ([paramExpr2], bodyExpr2); Lambda ([paramExpr1], bodyExpr1)], [o2; o1] ->
                        let keysExpr = Expression.New(keyType.GetConstructors().[0], [bodyExpr1; bodyExpr2; constant o1; constant o2])
                        [assign paramExpr1 getItemExpr; assign paramExpr2 getItemExpr; 
                            assign accessKeyArrayExpr keysExpr]
                    | Lambda ([paramExprn], bodyExprn) :: Lambda ([paramExprnm1], bodyExprnm1) :: restLambdas, on :: onm1 :: restOrders ->
                        let accKeysExpr = Expression.New((typedefof<Keys<int, int>>.MakeGenericType [|bodyExprnm1.Type; bodyExprn.Type|]).GetConstructors().[0], [bodyExprnm1; bodyExprn; constant onm1; constant on])
                        let keysExpr = List.fold (fun (keysExpr : NewExpression) (lambda : LambdaExpression, order : Order) -> 
                                                    Expression.New((typedefof<Keys<int, int>>.MakeGenericType [| lambda.Body.Type; keysExpr.Type |]).GetConstructors().[0], 
                                                                        [lambda.Body; keysExpr :> Expression; constant order; constant Order.Ascending]))
                                                 accKeysExpr (List.zip restLambdas restOrders)
                        
                        let assignExprs = List.fold (fun assignExprs (lambda : LambdaExpression) -> assign lambda.Parameters.[0] getItemExpr :: assignExprs) 
                                                    [assign paramExprn getItemExpr; assign paramExprnm1 getItemExpr] restLambdas
                        assignExprs @ [assign accessKeyArrayExpr keysExpr ]                    
                    | _ -> failwithf "Invalid state, keys %A" lambdaExprs

                let checkBoundExpr = equal indexVarExpr lengthExpr 
                let brachExpr = ifThenElse checkBoundExpr (``break`` loopBreak) (block [] exprs') 
                let loopExpr = block ((lambdaExprs |> List.map (fun lambdaExpr -> lambdaExpr.Parameters.[0])) @ [listVarExpr; indexVarExpr]) 
                                    [listAssignExpr; initValueArrayExpr; initKeyArrayExpr; indexAssignExpr; 
                                                    loop (block [] [addAssign indexVarExpr (constant 1); brachExpr]) loopBreak loopContinue]
                { KeyVarArrayExpr = keyVarArrayExpr; ValueVarArrayExpr = valueVarArrayExpr; LoopExpr = loopExpr; KeyType = keyType }
            
        let rec private compileToSeqPipeline (queryExpr : QueryExpr) (context : QueryContext) (optimize : Expression -> Expression) : Expression =
            match queryExpr with
            | Source (ExprType (Array (_, 1)) as expr, t, _) ->
                    let indexVarExpr = var "___index___" typeof<int>
                    let arrayVarExpr = var "___array___" expr.Type
                    let arrayAssignExpr = assign arrayVarExpr expr
                    let indexAssignExpr = assign indexVarExpr (constant -1) 
                    let lengthExpr = arrayLength arrayVarExpr 
                    let getItemExpr = arrayIndex arrayVarExpr indexVarExpr
                    let exprs' = assign context.CurrentVarExpr getItemExpr :: context.Exprs
                    let checkBoundExpr = equal indexVarExpr lengthExpr 
                    let brachExpr = ifThenElse checkBoundExpr (``break`` context.BreakLabel) (block [] exprs') 
                    let loopExpr = loop (block [] [addAssign indexVarExpr (constant 1); brachExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel 
                    block (arrayVarExpr :: indexVarExpr :: context.VarExprs) [block [] context.InitExprs; arrayAssignExpr; indexAssignExpr; loopExpr; context.ReturnExpr] 
            | Source (ExprType (Named (TypeCheck collectorType _, [|_|])) as expr, t, _) ->
                    let indexVarExpr = var "___index___" typeof<int>
                    let listVarExpr = var "___list___" expr.Type
                    let listAssignExpr = assign listVarExpr expr
                    let indexAssignExpr = assign indexVarExpr (constant -1) 
                    let lengthExpr = call (expr.Type.GetMethod("get_Count")) listVarExpr []
                    let getItemExpr = call (expr.Type.GetMethod("get_Item")) listVarExpr [indexVarExpr]
                    let exprs' = assign context.CurrentVarExpr getItemExpr :: context.Exprs
                    let checkBoundExpr = equal indexVarExpr lengthExpr 
                    let brachExpr = ifThenElse checkBoundExpr (``break`` context.BreakLabel) (block [] exprs') 
                    let loopExpr = loop (block [] [addAssign indexVarExpr (constant 1); brachExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel 
                    block (listVarExpr :: indexVarExpr :: context.VarExprs) [block [] context.InitExprs; listAssignExpr; indexAssignExpr; loopExpr; context.ReturnExpr] 
            | Source (expr, t, _) -> // general case for IEnumerable
                    let enumerableType = typedefof<IEnumerable<_>>.MakeGenericType [| t |]
                    let enumeratorType = typedefof<IEnumerator<_>>.MakeGenericType [| t |]
                    let disposableVarExpr = var "___disposable___" typeof<IDisposable>
                    let enumeratorVarExpr = var "___enumerator___" enumeratorType
                    let enumeratorAssignExpr = assign enumeratorVarExpr (call (enumerableType.GetMethod("GetEnumerator")) expr [])
                    let disposableAssignExpr = assign disposableVarExpr enumeratorVarExpr 
                    let getItemExpr = call (enumeratorType.GetMethod("get_Current")) enumeratorVarExpr []
                    let exprs' = assign context.CurrentVarExpr getItemExpr :: context.Exprs
                    let checkBoundExpr = equal (call (typeof<IEnumerator>.GetMethod("MoveNext")) enumeratorVarExpr []) (constant false)
                    let brachExpr = ifThenElse checkBoundExpr (``break`` context.BreakLabel) (block [] exprs') 
                    let loopExpr = tryfinally (loop (block [] [brachExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel) (call (typeof<IDisposable>.GetMethod("Dispose")) disposableVarExpr [])
                    block (enumeratorVarExpr :: disposableVarExpr :: context.VarExprs) [block [] context.InitExprs; enumeratorAssignExpr; disposableAssignExpr; loopExpr; context.ReturnExpr] 
            | RangeGenerator(start, countExpr) ->
                    // count < 0 || (int64 start + int64 count) - 1L > int64 Int32.MaxValue
                    let (start, countExpr) = (optimize start, optimize countExpr)
                    let countVarExpr = var "___count___" typeof<int>
                    let countVarInitExpr = assign countVarExpr countExpr
                    let countCheckExpr = 
                        let left = Expression.LessThan(countVarExpr, constant 0)
                        let right = Expression.GreaterThan(
                                        Expression.Subtract(
                                            Expression.Add(cast start typeof<int64>, cast countVarExpr typeof<int64>),
                                            constant 1L),
                                        constant (int64 Int32.MaxValue))
                        Expression.Or(left, right)
                    let exceptionExpr = 
                        let exc = Expression.New(typeof<ArgumentOutOfRangeException>.GetConstructor([|typeof<string>|]), constant "count")
                        Expression.Block(Expression.Throw(exc), 
                            constant Unchecked.defaultof<IEnumerable<int>>)

                    let startExpr = Expression.Subtract(start , constant 1)
                    let endExpr = Expression.Add(start, countVarExpr)
                    let currVarExpr = var "___curr___" typeof<int>
                    let currVarInitExpr = assign currVarExpr startExpr
                    let checkExpr = equal currVarExpr endExpr
                    let incCurrExpr = addAssign currVarExpr (constant 1)
                    let exprs' = assign context.CurrentVarExpr currVarExpr :: context.Exprs
                    let branchExpr = ifThenElse checkExpr (``break`` context.BreakLabel) (block [] exprs')
                    let loopExpr = 
                        ifThenElse countCheckExpr exceptionExpr 
                            (loop (block [] [incCurrExpr; branchExpr ; context.AccExpr]) context.BreakLabel context.ContinueLabel)
                    block (countVarExpr :: currVarExpr :: context.VarExprs) [block [] context.InitExprs; countVarInitExpr; currVarInitExpr; loopExpr; context.ReturnExpr ]
            | RepeatGenerator(elementExpr, countExpr) ->
                    let countExpr = optimize countExpr
                    let t = elementExpr.Type
                    let countVarExpr = var "___count___" typeof<int>
                    let countVarInitExpr = assign countVarExpr countExpr
                    let countCheckExpr = Expression.LessThan(countVarExpr, constant 0)
                    let exceptionExpr = 
                        let exc = Expression.New(typeof<ArgumentOutOfRangeException>.GetConstructor([|typeof<string>|]), constant "count")
                        Expression.Block(Expression.Throw(exc), 
                            constant null)

                    let endExpr = countExpr
                    let indexVarExpr = var "___index___" typeof<int>
                    let indexVarInitExpr = assign indexVarExpr countExpr
                    let elemVarExpr = var "___elem___" t
                    let elemVarInitExpr = assign elemVarExpr elementExpr
                    let checkExpr = lessThan indexVarExpr (constant 0)
                    let incCurrExpr = subAssign indexVarExpr (constant 1) 
                    let exprs' = assign context.CurrentVarExpr elemVarExpr :: context.Exprs
                    let branchExpr = ifThenElse checkExpr (``break`` context.BreakLabel) (block [] exprs')
                    let loopExpr = 
                        ifThenElse countCheckExpr  exceptionExpr 
                            (loop (block [] [incCurrExpr; branchExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel)
                    block (countVarExpr :: indexVarExpr :: elemVarExpr :: context.VarExprs) [block [] context.InitExprs; countVarInitExpr; elemVarInitExpr; indexVarInitExpr; loopExpr; context.ReturnExpr ]
            

            | ZipWith((ExprType (Array (_, 1)) as expr1),(ExprType (Array (_, 1)) as expr2), (Lambda ([param1Expr; param2Expr], bodyExpr) as func)) ->
                    let bodyExpr = optimize bodyExpr
                    let indexVarExpr = var "___index___" typeof<int>
                    let indexAssignExpr = assign indexVarExpr (constant -1) 
                         
                    let array1VarExpr = var "___array1___" expr1.Type
                    let array1AssignExpr = assign array1VarExpr expr1
                    let length1Expr = arrayLength array1VarExpr 
                    let getItem1Expr = arrayIndex array1VarExpr indexVarExpr

                    let array2VarExpr = var "___array2___" expr2.Type
                    let array2AssignExpr = assign array2VarExpr expr2
                    let length2Expr = arrayLength array2VarExpr 
                    let getItem2Expr = arrayIndex array2VarExpr indexVarExpr

                    let param1AssignExpr = assign param1Expr getItem1Expr
                    let param2AssignExpr = assign param2Expr getItem2Expr
                    let getItemExpr = bodyExpr
                        
                    let exprs' = param1AssignExpr ::  param2AssignExpr :: assign context.CurrentVarExpr getItemExpr :: context.Exprs
                    let checkBound1Expr = equal indexVarExpr length1Expr 
                    let checkBound2Expr = equal indexVarExpr length2Expr
                    let checkBoundExpr = Expression.Or(checkBound1Expr, checkBound2Expr)
                    let branchExpr = ifThenElse checkBoundExpr (``break`` context.BreakLabel) (block [] exprs')

                    let loopExpr = loop (block [] [addAssign indexVarExpr (constant 1); branchExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel
                    let vars =   param1Expr :: param2Expr ::array1VarExpr :: array2VarExpr :: indexVarExpr :: context.VarExprs
                    block vars
                        [block [] context.InitExprs; array1AssignExpr; array2AssignExpr; indexAssignExpr; loopExpr; context.ReturnExpr]
            | ZipWith((ExprType (Named (TypeCheck collectorType _, [|_|])) as expr1),(ExprType (Named (TypeCheck collectorType _, [|_|])) as expr2), (Lambda ([param1Expr; param2Expr], bodyExpr) as func)) ->
                    let bodyExpr = optimize bodyExpr
                    let indexVarExpr = var "___index___" typeof<int>
                    let indexAssignExpr = assign indexVarExpr (constant -1) 
                        
                    let list1VarExpr = var "___list1___" expr1.Type
                    let list1AssignExpr = assign list1VarExpr expr1
                    let length1Expr = call (expr1.Type.GetMethod("get_Count")) list1VarExpr []
                    let getItem1Expr = call (expr1.Type.GetMethod("get_Item")) list1VarExpr [indexVarExpr]

                    let list2VarExpr = var "___list2___" expr2.Type
                    let list2AssignExpr = assign list2VarExpr expr2
                    let length2Expr = call (expr2.Type.GetMethod("get_Count")) list2VarExpr []
                    let getItem2Expr = call (expr2.Type.GetMethod("get_Item")) list2VarExpr [indexVarExpr]
                                                
                    let param1AssignExpr = assign param1Expr getItem1Expr
                    let param2AssignExpr = assign param2Expr getItem2Expr
                    let getItemExpr = bodyExpr
                        
                    let exprs' = param1AssignExpr ::  param2AssignExpr :: assign context.CurrentVarExpr getItemExpr :: context.Exprs
                    let checkBound1Expr = equal indexVarExpr length1Expr 
                    let checkBound2Expr = equal indexVarExpr length2Expr
                    let checkBoundExpr = Expression.Or(checkBound1Expr, checkBound2Expr)
                    let branchExpr = ifThenElse checkBoundExpr (``break`` context.BreakLabel) (block [] exprs')

                    let loopExpr = loop (block [] [addAssign indexVarExpr (constant 1); branchExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel
                    let vars = param1Expr :: param2Expr ::list1VarExpr :: list2VarExpr :: indexVarExpr :: context.VarExprs
                    block vars
                        [block [] context.InitExprs; list1AssignExpr; list2AssignExpr; indexAssignExpr; loopExpr; context.ReturnExpr]
            | ZipWith((expr1),(expr2), (Lambda ([param1Expr; param2Expr], bodyExpr) as func) ) ->
                    let bodyExpr = optimize bodyExpr
                    let t1, t2 = getIEnumerableType expr1.Type , getIEnumerableType expr2.Type
                    let enumerable1Type = typedefof<IEnumerable<_>>.MakeGenericType [| t1 |]
                    let enumerator1Type = typedefof<IEnumerator<_>>.MakeGenericType [| t1 |]
                    let disposable1VarExpr = var "___disposable1___" typeof<IDisposable>
                    let enumerator1VarExpr = var "___enumerator1___" enumerator1Type
                    let enumerator1AssignExpr = assign enumerator1VarExpr (call (enumerable1Type.GetMethod("GetEnumerator")) expr1 [])
                    let disposable1AssignExpr = assign disposable1VarExpr enumerator1VarExpr 
                    let getItem1Expr = call (enumerator1Type.GetMethod("get_Current")) enumerator1VarExpr []

                    let enumerable2Type = typedefof<IEnumerable<_>>.MakeGenericType [| t2 |]
                    let enumerator2Type = typedefof<IEnumerator<_>>.MakeGenericType [| t2 |]
                    let disposable2VarExpr = var "___disposable2___" typeof<IDisposable>
                    let enumerator2VarExpr = var "___enumerator2___" enumerator2Type
                    let enumerator2AssignExpr = assign enumerator2VarExpr (call (enumerable2Type.GetMethod("GetEnumerator")) expr2 [])
                    let disposable2AssignExpr = assign disposable2VarExpr enumerator2VarExpr 
                    let getItem2Expr = call (enumerator2Type.GetMethod("get_Current")) enumerator2VarExpr []

                    let param1AssignExpr = assign param1Expr getItem1Expr
                    let param2AssignExpr = assign param2Expr getItem2Expr
                    let getItemExpr = bodyExpr

                    let exprs' = param1AssignExpr ::  param2AssignExpr :: assign context.CurrentVarExpr getItemExpr :: context.Exprs

                    let checkBound1Expr = equal (call (typeof<IEnumerator>.GetMethod("MoveNext")) enumerator1VarExpr []) (constant false)
                    let checkBound2Expr = equal (call (typeof<IEnumerator>.GetMethod("MoveNext")) enumerator2VarExpr []) (constant false)
                    let checkBoundExpr = Expression.Or(checkBound1Expr, checkBound2Expr)
                    let branchExpr = ifThenElse checkBoundExpr (``break`` context.BreakLabel) (block [] exprs')
                    let disposeCallExpr = block [] [ (call (typeof<IDisposable>.GetMethod("Dispose")) disposable1VarExpr []);
                                                        (call (typeof<IDisposable>.GetMethod("Dispose")) disposable2VarExpr []) ]
                    let loopExpr = tryfinally (loop (block [] [branchExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel) disposeCallExpr
                    let vars =  
                        param1Expr :: param2Expr :: enumerator1VarExpr :: disposable1VarExpr :: 
                        enumerator2VarExpr :: disposable2VarExpr :: context.VarExprs
                    block vars 
                        [block [] context.InitExprs; enumerator1AssignExpr; disposable1AssignExpr; enumerator2AssignExpr; disposable2AssignExpr; loopExpr; context.ReturnExpr]

            
            | Transform (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                let bodyExpr = optimize bodyExpr
                let exprs' = assign context.CurrentVarExpr bodyExpr :: context.Exprs
                compileToSeqPipeline queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' } optimize
            | TransformIndexed (Lambda ([paramExpr; indexExpr], bodyExpr), queryExpr') ->
                let bodyExpr = optimize bodyExpr
                let exprs' = addAssign indexExpr (constant 1) :: assign context.CurrentVarExpr bodyExpr :: context.Exprs
                compileToSeqPipeline queryExpr' { context with CurrentVarExpr = paramExpr; InitExprs = assign indexExpr (constant -1) :: context.InitExprs; VarExprs = paramExpr :: indexExpr :: context.VarExprs; Exprs = exprs' } optimize
            
            | Generate(startExpr, Lambda ([paramCondExpr], bodyCondExpr), Lambda ([paramStateExpr], bodyStateExpr), Lambda ([paramResultExpr], bodyResultExpr)) ->

                let (startExpr, bodyCondExpr, bodyStateExpr, bodyResultExpr) = (optimize startExpr, optimize bodyCondExpr, optimize bodyStateExpr, optimize bodyResultExpr)
                
                let stateVarInitExpr = assign paramStateExpr startExpr
                let nextStateExpr = assign paramStateExpr bodyStateExpr

                let exprs' = assign paramResultExpr paramStateExpr :: assign context.CurrentVarExpr bodyResultExpr :: context.Exprs
                let branchExpr = ifThenElse bodyCondExpr (block [] exprs') (``break`` context.BreakLabel) 
                let loopExpr = 
                    (loop (block [] [ (assign paramCondExpr paramStateExpr); branchExpr ; nextStateExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel)
                
                block (paramCondExpr :: paramStateExpr :: paramResultExpr :: context.VarExprs) [block [] context.InitExprs; stateVarInitExpr; loopExpr; context.ReturnExpr ]

            | Filter (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                let bodyExpr = optimize bodyExpr
                let exprs' = ifThenElse bodyExpr empty (``continue`` context.ContinueLabel) :: assign context.CurrentVarExpr paramExpr :: context.Exprs
                compileToSeqPipeline queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' } optimize
            | FilterIndexed (Lambda ([paramExpr; indexExpr], bodyExpr), queryExpr') ->
                let bodyExpr = optimize bodyExpr
                let exprs' = addAssign indexExpr (constant 1) :: ifThenElse bodyExpr empty (``continue`` context.ContinueLabel) :: assign context.CurrentVarExpr paramExpr :: context.Exprs
                compileToSeqPipeline queryExpr' { context with CurrentVarExpr = paramExpr; InitExprs = assign indexExpr (constant -1) :: context.InitExprs; VarExprs = indexExpr :: paramExpr :: context.VarExprs; Exprs = exprs' } optimize
            | Take (countExpr, queryExpr') ->
                let countExpr = optimize countExpr
                let countVarExpr = var "___takeCount___" typeof<int> //special "local" variable for Take
                let exprs' = addAssign countVarExpr (constant 1) :: ifThenElse (greaterThan countVarExpr countExpr) (``break`` context.BreakLabel) empty :: context.Exprs
                compileToSeqPipeline queryExpr' { context with InitExprs = assign countVarExpr (constant 0) :: context.InitExprs ; VarExprs = countVarExpr :: context.VarExprs; Exprs = exprs' } optimize
            | TakeWhile (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                let bodyExpr = optimize bodyExpr
                let exprs' = ifThenElse (bodyExpr) empty (``break`` context.BreakLabel) :: assign context.CurrentVarExpr paramExpr :: context.Exprs
                compileToSeqPipeline queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' } optimize
            | SkipWhile (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                let bodyExpr = optimize bodyExpr
                let skipWhileVar = var "___skipFlag___" typeof<bool>
                let exprs' = ifThenElse (Expression.And(skipWhileVar, bodyExpr)) (``continue`` context.ContinueLabel) (assign skipWhileVar (constant false)) :: assign context.CurrentVarExpr paramExpr :: context.Exprs
                compileToSeqPipeline queryExpr' { context with InitExprs = assign skipWhileVar (constant true) :: context.InitExprs; CurrentVarExpr = paramExpr; VarExprs = skipWhileVar :: paramExpr :: context.VarExprs; Exprs = exprs' } optimize
            | Skip (countExpr, queryExpr') ->
                let countExpr = optimize countExpr
                let countVarExpr = var "___skipCount___" typeof<int> //special "local" variable for Skip
                let exprs' = addAssign countVarExpr (constant 1) :: ifThenElse (lessThanOrEqual countVarExpr countExpr) (``continue`` context.ContinueLabel) empty :: context.Exprs
                compileToSeqPipeline queryExpr' { context with InitExprs = assign countVarExpr (constant 0) :: context.InitExprs ; VarExprs = countVarExpr :: context.VarExprs; Exprs = exprs' } optimize
            | NestedQuery ((paramExpr, nestedQueryExpr), queryExpr') ->
                let context' = { CurrentVarExpr = context.CurrentVarExpr; AccVarExpr = context.AccVarExpr; BreakLabel = context.BreakLabel; ContinueLabel = context.ContinueLabel; 
                                    InitExprs = [empty]; AccExpr = context.AccExpr; CombinerExpr = empty; ReturnExpr = empty; 
                                    VarExprs = []; Exprs = context.Exprs; ReductionType = context.ReductionType }

                let expr = compileToSeqPipeline nestedQueryExpr context' optimize
                compileToSeqPipeline queryExpr' { context with CurrentVarExpr = paramExpr; AccExpr = empty; VarExprs = paramExpr :: context.VarExprs; Exprs = [expr]; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); } optimize
            | NestedQueryTransform ((paramExpr, nestedQueryExpr), Lambda ([valueExpr; colExpr], bodyExpr), queryExpr') ->
                let bodyExpr = optimize bodyExpr
                let context' = { CurrentVarExpr = colExpr; AccVarExpr = context.AccVarExpr; BreakLabel = context.BreakLabel; ContinueLabel = context.ContinueLabel; 
                                    InitExprs = [empty]; AccExpr = context.AccExpr; CombinerExpr = empty; ReturnExpr = empty; 
                                    VarExprs = []; Exprs = assign valueExpr paramExpr :: assign context.CurrentVarExpr bodyExpr :: context.Exprs;
                                    ReductionType = context.ReductionType  }

                let expr = compileToSeqPipeline nestedQueryExpr context' optimize
                compileToSeqPipeline queryExpr' { context with
                                                     CurrentVarExpr = paramExpr; AccExpr = empty; VarExprs = paramExpr :: valueExpr :: colExpr :: context.VarExprs; 
                                                     Exprs = [expr]; BreakLabel = breakLabel (); ContinueLabel = continueLabel ();  } optimize
            | GroupBy (Lambda ([paramExpr], bodyExpr), queryExpr', _) ->
                let bodyExpr = optimize bodyExpr
                let listType = collectorType.MakeGenericType [| queryExpr'.Type |]
                let finalVarExpr, accVarExpr  = var "___final___" queryExpr'.Type, var "___acc___" listType
                let initExpr, accExpr = assign accVarExpr (``new`` listType), call (listType.GetMethod("Add")) accVarExpr [finalVarExpr]
                let context' = { CurrentVarExpr = finalVarExpr; AccVarExpr = accVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                    InitExprs = [initExpr]; AccExpr = accExpr; CombinerExpr = empty; ReturnExpr = empty; 
                                    VarExprs = [finalVarExpr]; Exprs = []; ReductionType = ReductionType.ToList }
                let expr = compileToSeqPipeline queryExpr' context' optimize
                // Generate loop to extract keys
                let { KeyVarArrayExpr = keyVarArrayExpr; ValueVarArrayExpr = valueVarArrayExpr;
                      LoopExpr = loopExpr } = collectKeyValueArrays accVarExpr [lambda [|paramExpr|] bodyExpr] []
                // generare grouping
                let groupByMethodInfo = typeof<Grouping>.GetMethods()
                                            |> Array.find (fun methodInfo -> 
                                                            match methodInfo with
                                                            | MethodName "GroupBy" [|_; _|] -> true
                                                            | _ -> false) // TODO: reflection type checks
                                            |> (fun methodInfo -> methodInfo.MakeGenericMethod [|bodyExpr.Type; paramExpr.Type|])
                let groupingType = typedefof<IGrouping<_, _>>.MakeGenericType [|bodyExpr.Type; paramExpr.Type|]
                let groupByCallExpr = call groupByMethodInfo null [keyVarArrayExpr; valueVarArrayExpr]
                let expr' = compileToSeqPipeline (Source (groupByCallExpr, groupingType, QueryExprType.Sequential)) context optimize
                block [accVarExpr; keyVarArrayExpr; valueVarArrayExpr] [expr; loopExpr; expr']
            | OrderBy (keySelectorOrderPairs, queryExpr') ->
                let keySelectorOrderPairs' = keySelectorOrderPairs |> List.map (fun (lambdaExpr, order) -> (lambda (lambdaExpr.Parameters.ToArray()) (optimize lambdaExpr.Body)), order)
                let listType = collectorType.MakeGenericType [| queryExpr'.Type |]
                let finalVarExpr, accVarExpr  = var "___final___" queryExpr'.Type, var "___acc___" listType
                let initExpr, accExpr = assign accVarExpr (``new`` listType), call (listType.GetMethod("Add")) accVarExpr [finalVarExpr]
                let context' = { CurrentVarExpr = finalVarExpr; AccVarExpr = accVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                    InitExprs = [initExpr]; AccExpr = accExpr; CombinerExpr = empty; ReturnExpr = empty; 
                                    VarExprs = [finalVarExpr]; Exprs = []; ReductionType = ReductionType.ToList  }
                let expr = compileToSeqPipeline queryExpr' context' optimize
                // Generate loop to extract keys
                let { KeyVarArrayExpr = keyVarArrayExpr; ValueVarArrayExpr = valueVarArrayExpr;
                      LoopExpr = loopExpr; KeyType = keyType } = collectKeyValueArrays accVarExpr (keySelectorOrderPairs' |> List.map fst) (keySelectorOrderPairs' |> List.map snd)
                // generate sort 
                let sortMethodInfo = typeof<Sort>.GetMethods()
                                            |> Array.find (fun methodInfo -> 
                                                            match methodInfo with
                                                            | MethodName "SequentialSort" [|_; _; _|] -> true
                                                            | _ -> false) // TODO: reflection type checks
                                            |> (fun methodInfo -> methodInfo.MakeGenericMethod [|keyType; queryExpr'.Type|])
                let sortCallExpr = call sortMethodInfo null [keyVarArrayExpr; valueVarArrayExpr; constant (keySelectorOrderPairs' |> List.map snd |> List.toArray)]
                let expr' = compileToSeqPipeline (Source (valueVarArrayExpr, queryExpr'.Type, QueryExprType.Sequential)) context optimize
                block [accVarExpr; keyVarArrayExpr; valueVarArrayExpr] [expr; loopExpr; sortCallExpr; expr']
            | _ -> failwithf "Invalid state %A" queryExpr 

        let rec compileToSequential (queryExpr : QueryExpr) (optimize : Expression -> Expression) : Expression = 
            match queryExpr with
            | Sum (queryExpr') ->
                let t = queryExpr'.Type
                let finalVarExpr = var "___final___" t
                let accVarExpr = var "___acc___" t
                let initExpr = assign accVarExpr (``default`` t)
                let accExpr = addAssign accVarExpr finalVarExpr
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = accVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [initExpr]; AccExpr = accExpr; CombinerExpr = empty; ReturnExpr = accVarExpr; 
                                VarExprs = [finalVarExpr; accVarExpr]; Exprs = []; ReductionType = ReductionType.Sum }
                let expr = compileToSeqPipeline queryExpr' context optimize
                expr 
            | Count (queryExpr') ->
                let t = queryExpr'.Type
                let accVarExpr = var "___cnt___" typeof<int>
                let initExpr = assign accVarExpr (constant 0)
                let accExpr = addAssign accVarExpr (constant 1)
                let tmpVarExpr = var "___tmp___" t
                let context = { CurrentVarExpr = tmpVarExpr; AccVarExpr = accVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [initExpr]; AccExpr = accExpr; CombinerExpr = empty; ReturnExpr = accVarExpr; 
                                VarExprs = [tmpVarExpr; accVarExpr]; Exprs = []; ReductionType = ReductionType.Count  }
                let expr = compileToSeqPipeline queryExpr' context optimize
                expr 
            | Aggregate (seed, Lambda ([accVarExpr; varExpr], bodyExpr), queryExpr') ->
                let seed = optimize seed
                let bodyExpr = optimize bodyExpr
                let initExpr = assign accVarExpr seed
                let accExpr = assign accVarExpr bodyExpr
                let context = { CurrentVarExpr = varExpr; AccVarExpr = accVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [initExpr]; AccExpr = accExpr; CombinerExpr = empty; ReturnExpr = accVarExpr; 
                                VarExprs = [varExpr; accVarExpr]; Exprs = []; ReductionType = ReductionType.Aggregate  }
                let expr = compileToSeqPipeline queryExpr' context optimize
                expr
            | ForEach (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                let bodyExpr = optimize bodyExpr
                let context = { CurrentVarExpr = paramExpr; AccVarExpr = var "___empty___" typeof<unit>; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [assign paramExpr (``default`` paramExpr.Type)]; 
                                AccExpr = bodyExpr; CombinerExpr = empty; ReturnExpr = empty; 
                                VarExprs = [paramExpr]; Exprs = []; ReductionType = ReductionType.Iter }
                let expr = compileToSeqPipeline queryExpr' context optimize
                expr
            | ToArray queryExpr' ->
                let collectorType = collectorType.MakeGenericType [| queryExpr'.Type |]
                let context = toListContext queryExpr'
                let expr = compileToSeqPipeline queryExpr' context optimize
                call (collectorType.GetMethod "ToArray") expr []
            | ToList queryExpr' ->
                let collectorType = collectorType.MakeGenericType [| queryExpr'.Type |]
                let context = toListContext queryExpr'
                let expr = compileToSeqPipeline queryExpr' context optimize
                call (collectorType.GetMethod "ToList") expr []
            | queryExpr' ->
                let collectorType = collectorType.MakeGenericType [| queryExpr'.Type |]
                let context = toListContext queryExpr'
                let expr = compileToSeqPipeline queryExpr context optimize
                expr

        let rec compileToParallel (queryExpr : QueryExpr) (optimize : Expression -> Expression) : Expression =
            let toParallelListContext (queryExpr : QueryExpr) = 
                let listType = collectorType.MakeGenericType [| queryExpr.Type |]
                let finalVarExpr, accVarExpr  = var "___final___" queryExpr.Type, var "___acc___" listType
                let initExpr, accExpr = lambda [||] (``new`` listType), block [] [call (listType.GetMethod("Add")) accVarExpr [finalVarExpr]; accVarExpr]
                let leftVarExpr, rightVarExpr = var "___left___" listType, var "___right___" listType
                let combinerExpr = lambda [|leftVarExpr; rightVarExpr|] (block [] [call (listType.GetMethod("AddRange")) leftVarExpr [rightVarExpr]; leftVarExpr])
                let returnExpr = lambda [|accVarExpr|] accVarExpr
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = accVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                    InitExprs = [initExpr]; AccExpr = accExpr; CombinerExpr = combinerExpr; ReturnExpr = returnExpr; 
                                    VarExprs = [finalVarExpr]; Exprs = []; ReductionType = ReductionType.ToList  }
                context



            let rec compile queryExpr context =
                match queryExpr with
                | Source (ExprType (Array (_, 1)) as expr, t, _) | Source (ExprType (Named (TypeCheck collectorType _, [|_|])) as expr, t, _) ->
                    let methods = typeof<ParallelismHelpers>.GetMethods()
                    let aggregateMethodInfo = methods
                                                |> Array.find (fun methodInfo -> 
                                                                match methodInfo with
                                                                | MethodName "ReduceCombine" [|ParamType (Array (_, 1)); _; _; _; _|] -> true
                                                                | _ -> false) // TODO: reflection type checks
                                                |> (fun methodInfo -> methodInfo.MakeGenericMethod [|context.CurrentVarExpr.Type; context.AccVarExpr.Type; context.AccVarExpr.Type|])
                    // loop Expr
                    let indexVarExpr = var "___index___" typeof<int>
                    let arrayVarExpr = var "___array___" expr.Type
                    let lengthVarExpr = var "___length___" typeof<int>
                    let arrayAssignExpr = assign arrayVarExpr expr
                    let getItemExpr = arrayIndex arrayVarExpr indexVarExpr
                    let exprs' = assign context.CurrentVarExpr getItemExpr :: context.Exprs
                    let checkBoundExpr = greaterThan indexVarExpr lengthVarExpr 
                    let brachExpr = ifThenElse checkBoundExpr (``break`` context.BreakLabel) (block [] exprs') 
                    let loopExpr = loop (block [] [(addAssign indexVarExpr (constant 1)); brachExpr; context.AccExpr]) context.BreakLabel context.ContinueLabel :> Expression
                    let accExpr = lambda [|arrayVarExpr; indexVarExpr; lengthVarExpr; context.AccVarExpr|] 
                                            (block context.VarExprs
                                            (loopExpr :: [context.AccVarExpr]))
                    call aggregateMethodInfo null [expr; List.head context.InitExprs; accExpr; context.CombinerExpr; context.ReturnExpr]
                | Source (expr, t, _) ->
                    let aggregateMethodInfo = typeof<ParallelismHelpers>.GetMethods()
                                                |> Array.find (fun methodInfo -> 
                                                                match methodInfo with
                                                                | MethodName "ReduceCombine" [| ParamType (Named (TypeCheck partitionerTypeDef _, [|_|])); _; _; _; _|] -> true
                                                                | _ -> false) // TODO: reflection type checks
                                                |> (fun methodInfo -> methodInfo.MakeGenericMethod [|context.CurrentVarExpr.Type; context.AccVarExpr.Type; context.AccVarExpr.Type|])
                    let partitionerCreateMethodInfo = typeof<Partitioner>.GetMethods()
                                                        |> Array.find (fun methodInfo -> 
                                                                        match methodInfo with
                                                                        | MethodName "Create" [|_|] -> true
                                                                        | _ -> false) // TODO: reflection type checks
                                                        |> (fun methodInfo -> methodInfo.MakeGenericMethod [|context.CurrentVarExpr.Type|])
                    let accExpr = lambda [|context.AccVarExpr; context.CurrentVarExpr|] 
                                            (block (context.VarExprs |> List.filter (fun var -> not (var = context.CurrentVarExpr))) 
                                            (context.Exprs @ [context.AccExpr; label context.ContinueLabel; context.AccVarExpr]))
                    let partitionerCallExpr = call partitionerCreateMethodInfo  null [expr]
                    call aggregateMethodInfo null [partitionerCallExpr; List.head context.InitExprs; accExpr; context.CombinerExpr; context.ReturnExpr]
                | Transform (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                    let bodyExpr = optimize bodyExpr
                    let exprs' = assign context.CurrentVarExpr bodyExpr :: context.Exprs
                    compile queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' }
                | Filter (Lambda ([paramExpr], bodyExpr), queryExpr') ->
                    let bodyExpr = optimize bodyExpr
                    let exprs' = ifThenElse bodyExpr empty (goto context.ContinueLabel) :: assign context.CurrentVarExpr paramExpr :: context.Exprs
                    compile queryExpr' { context with CurrentVarExpr = paramExpr; VarExprs = paramExpr :: context.VarExprs; Exprs = exprs' }
                | NestedQuery ((paramExpr, nestedQueryExpr), queryExpr') ->
                    let context' = { CurrentVarExpr = context.CurrentVarExpr; AccVarExpr = context.AccVarExpr; BreakLabel = context.BreakLabel; ContinueLabel = context.ContinueLabel; 
                                        InitExprs = [empty]; AccExpr = context.AccExpr; CombinerExpr = empty; ReturnExpr = empty; 
                                        VarExprs = []; Exprs = context.Exprs; ReductionType = context.ReductionType  }

                    let expr = compileToSeqPipeline nestedQueryExpr context' optimize
                    compile queryExpr' { context with CurrentVarExpr = paramExpr; AccExpr = empty; VarExprs = paramExpr :: context.VarExprs; Exprs = [expr]; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); }
                | NestedQueryTransform ((paramExpr, nestedQueryExpr), Lambda ([valueExpr; colExpr], bodyExpr), queryExpr') ->
                    let bodyExpr = optimize bodyExpr
                    let context' = { CurrentVarExpr = colExpr; AccVarExpr = context.AccVarExpr; BreakLabel = context.BreakLabel; ContinueLabel = context.ContinueLabel;  
                                        InitExprs = [empty]; AccExpr = context.AccExpr; CombinerExpr = empty; ReturnExpr = empty; 
                                        VarExprs = []; Exprs = assign valueExpr paramExpr :: assign context.CurrentVarExpr bodyExpr :: context.Exprs;
                                        ReductionType = context.ReductionType  }

                    let expr = compileToSeqPipeline nestedQueryExpr context' optimize
                    compile queryExpr' { context with CurrentVarExpr = paramExpr; 
                                                        AccExpr = empty; VarExprs = paramExpr :: valueExpr :: colExpr :: context.VarExprs; 
                                                        Exprs = [expr]; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); }
                | GroupBy (Lambda ([paramExpr], bodyExpr) as lambdaExpr, queryExpr', _) ->
                    let bodyExpr = optimize bodyExpr
                    let context' = toParallelListContext queryExpr'
                    let expr = compile queryExpr' context'
                    let { KeyVarArrayExpr = keyVarArrayExpr; ValueVarArrayExpr = valueVarArrayExpr;
                          LoopExpr = loopExpr } = collectKeyValueArrays expr [lambda [|paramExpr|] bodyExpr] []
                    let groupByMethodInfo = typeof<Grouping>.GetMethods()
                                                |> Array.find (fun methodInfo -> 
                                                                match methodInfo with
                                                                | MethodName "ParallelGroupBy" [|_; _|] -> true
                                                                | _ -> false) // TODO: reflection type checks
                                                |> (fun methodInfo -> methodInfo.MakeGenericMethod [|bodyExpr.Type; paramExpr.Type |])
                    let groupingType = typedefof<IGrouping<_, _>>.MakeGenericType [| bodyExpr.Type; paramExpr.Type|]
                    let groupByCallExpr = call groupByMethodInfo null [keyVarArrayExpr; valueVarArrayExpr]
                    let expr' = compile (Source (groupByCallExpr, groupingType, QueryExprType.Parallel)) context
                    block [keyVarArrayExpr; valueVarArrayExpr] [loopExpr; expr']
                | OrderBy (keySelectorOrderPairs, queryExpr') ->
                    let keySelectorOrderPairs' = keySelectorOrderPairs |> List.map (fun (lambdaExpr, order) -> (lambda (lambdaExpr.Parameters.ToArray()) (optimize lambdaExpr.Body)), order)
                    let context' = toParallelListContext queryExpr'
                    let expr = compile queryExpr' context'
                    let { KeyVarArrayExpr = keyVarArrayExpr; ValueVarArrayExpr = valueVarArrayExpr;
                          LoopExpr = loopExpr; KeyType = keyType } = collectKeyValueArrays expr (keySelectorOrderPairs' |> List.map fst) (keySelectorOrderPairs' |> List.map snd)
                    // generate sort 
                    let sortMethodInfo = typeof<Sort>.GetMethods()
                                                |> Array.find (fun methodInfo -> 
                                                                match methodInfo with
                                                                | MethodName "ParallelSort" [|_; _; _|] -> true
                                                                | _ -> false) // TODO: reflection type checks
                                                |> (fun methodInfo -> methodInfo.MakeGenericMethod [|keyType; queryExpr'.Type|])
                    let sortCallExpr = call sortMethodInfo null [keyVarArrayExpr; valueVarArrayExpr; constant (keySelectorOrderPairs' |> List.map snd |> List.toArray)]
                    let expr' = compile (Source (valueVarArrayExpr, queryExpr'.Type, QueryExprType.Parallel)) context
                    block [keyVarArrayExpr; valueVarArrayExpr] [loopExpr; sortCallExpr; expr']
                | _ -> failwithf "Invalid state %A" queryExpr 
            match queryExpr with
            | Sum (queryExpr') ->
                let t = queryExpr'.Type
                let finalVarExpr = var "___final___" t
                let accVarExpr = var "___acc___" t
                let initExpr = lambda [||] (``default`` t)
                let accExpr = addAssign accVarExpr finalVarExpr
                let leftVarExpr, rightVarExpr = var "___left___" t, var "___right___" t
                let combinerExpr = lambda [|leftVarExpr; rightVarExpr|] (block [] [addAssign leftVarExpr rightVarExpr; leftVarExpr])
                let returnExpr = lambda [|accVarExpr|] accVarExpr
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = accVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [initExpr]; AccExpr = accExpr; CombinerExpr = combinerExpr; ReturnExpr = returnExpr; 
                                VarExprs = [finalVarExpr]; Exprs = []; ReductionType = ReductionType.Sum  }
                let expr = compile queryExpr' context
                expr 
            | Count (queryExpr') ->
                let t = queryExpr'.Type
                let finalVarExpr = var "___final___" t
                let accVarExpr = var "___acc___" typeof<int>
                let initExpr = lambda [||] (``default`` typeof<int>)
                let accExpr = addAssign accVarExpr (constant 1)
                let leftVarExpr, rightVarExpr = var "___left___" typeof<int>, var "___right___" typeof<int>
                let combinerExpr = lambda [|leftVarExpr; rightVarExpr|] (block [] [addAssign leftVarExpr rightVarExpr; leftVarExpr])
                let returnExpr = lambda [|accVarExpr|] accVarExpr
                let context = { CurrentVarExpr = finalVarExpr; AccVarExpr = accVarExpr; BreakLabel = breakLabel (); ContinueLabel = continueLabel (); 
                                InitExprs = [initExpr]; AccExpr = accExpr; CombinerExpr = combinerExpr; ReturnExpr = returnExpr; 
                                VarExprs = [finalVarExpr]; Exprs = []; ReductionType = ReductionType.Count  }
                let expr = compile queryExpr' context
                expr 
            | ToArray queryExpr' ->
                let collectorType = collectorType.MakeGenericType [| queryExpr'.Type |]
                let context = toParallelListContext queryExpr'
                let expr = compile queryExpr' context
                call (collectorType.GetMethod "ToArray") expr []
            | ToList queryExpr' ->
                let collectorType = collectorType.MakeGenericType [| queryExpr'.Type |]
                let context = toParallelListContext queryExpr'
                let expr = compile queryExpr' context
                call (collectorType.GetMethod "ToList") expr []
            | queryExpr' ->
                let collectorType = collectorType.MakeGenericType [| queryExpr'.Type |]
                let context = toParallelListContext queryExpr'
                let expr = compile queryExpr context
                expr
