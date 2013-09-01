namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection

    // Typed Wrapper for QueryExpr 
    type QueryExpr<'T>(queryExpr : QueryExpr) =
        member self.QueryExpr = queryExpr 
    // Main Query representation
    and QueryExpr = 
        | Source of IEnumerable * Type
        | Transform of LambdaExpression * QueryExpr 
        | Filter of LambdaExpression * QueryExpr 
        | Sum of QueryExpr * Type
           
     
    [<AutoOpen>]
    module internal ExpressionHelpers =
        // F# friendly Expression functions 
        let empty = Expression.Empty()
        let block (varExprs : seq<ParameterExpression>) (exprs : seq<Expression>) = 
            Expression.Block(varExprs, exprs) 
        let assign leftExpr rightExpr = 
            Expression.Assign(leftExpr, rightExpr) :> Expression
        let addAssign leftExpr rightExpr = 
            Expression.AddAssign (leftExpr, rightExpr) :> Expression

        let var name (t : Type) = Expression.Parameter(t, name)
        let label (name : string) = Expression.Label(name) 
        let constant (value : obj) = Expression.Constant(value)
         
        let call (methodInfo : MethodInfo) (instance : Expression) 
                    (args : seq<Expression>) =
            Expression.Call(instance, methodInfo, args)
             
        let ``if`` boolExpr thenExpr elseExpr = 
            Expression.IfThenElse(boolExpr, thenExpr, elseExpr) 
        let loop bodyExpr breakLabel continueLabel = 
            Expression.Loop(bodyExpr, breakLabel, continueLabel)
        let ``break`` (label : LabelTarget) = 
            Expression.Break(label)
        

        let equal leftExpr rightExpr = Expression.Equal(leftExpr, rightExpr)
        let notEqual leftExpr rightExpr = Expression.NotEqual(leftExpr, rightExpr)

        let arrayIndex (arrayExpr : Expression) (indexExpr : Expression) = 
            Expression.ArrayIndex(arrayExpr, indexExpr)
        let arrayLength arrayExpr = Expression.ArrayLength(arrayExpr)

        // Expression Active Patterns
        let (|Lambda|_|) (expr : Expression) = 
            if expr :? LambdaExpression then 
                let lambdaExpr = expr :?> LambdaExpression
                Some (Seq.toList lambdaExpr.Parameters, lambdaExpr.Body)
            else None

    [<AutoOpen>]
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
                let finalVarExpr = var "current" typeof<double>
                let accVarExpr = var "___acc___" typeof<double>
                let initExpr = assign accVarExpr (constant Unchecked.defaultof<double>)
                let accExpr = addAssign accVarExpr finalVarExpr
                let expr = compile' queryExpr' initExpr accExpr accVarExpr [accVarExpr; finalVarExpr] []
                expr
            | _ -> failwithf "Invalid state %A" queryExpr 

    // LINQ-C# friendly extension methods 
    [<AutoOpen>]
    [<System.Runtime.CompilerServices.Extension>]
    type ExtensionMethods =
     
        [<System.Runtime.CompilerServices.Extension>]
        static member AsQueryExpr(enumerable : IEnumerable<'T>) = 
            new QueryExpr<IEnumerable<'T>>(Source (enumerable, typeof<'T>))

        [<System.Runtime.CompilerServices.Extension>]
        static member Select<'T, 'R>(queryExpr : QueryExpr<IEnumerable<'T>>, f : Expression<Func<'T, 'R>>) =
            new QueryExpr<IEnumerable<'R>>(Transform (f, queryExpr.QueryExpr))
            
        [<System.Runtime.CompilerServices.Extension>]
        static member Where<'T>(queryExpr : QueryExpr<IEnumerable<'T>>, f : Expression<Func<'T, bool>>) =
            new QueryExpr<IEnumerable<'T>>(Filter (f, queryExpr.QueryExpr))

        [<System.Runtime.CompilerServices.Extension>]
        static member Sum(queryExpr : QueryExpr<IEnumerable<double>>) =
            new QueryExpr<double>(Sum (queryExpr.QueryExpr, typeof<double>))

        [<System.Runtime.CompilerServices.Extension>]
        static member Compile<'T>(queryExpr : QueryExpr<'T>) : Func<'T> =
            let expr = compile queryExpr.QueryExpr
            let func = Expression.Lambda<Func<'T>>(expr).Compile()
            func
            
        [<System.Runtime.CompilerServices.Extension>]
        static member Run<'T>(queryExpr : QueryExpr<'T>) : 'T =
            ExtensionMethods.Compile(queryExpr).Invoke()
            


        
        