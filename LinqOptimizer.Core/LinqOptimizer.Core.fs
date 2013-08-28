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

        let rec compile (queryExpr : QueryExpr)
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
                    let indexExpr = var "___index___" typeof<int>
                    let indexAssignExpr = assign indexExpr (constant -1) 
                    let lengthExpr = arrayLength (constant array) 
                    let getItemExpr = arrayIndex (constant array) indexExpr
                    let exprs' = assign current getItemExpr :: exprs
                    let checkBoundExpr = equal indexExpr lengthExpr 
                    let brachExpr = ``if`` checkBoundExpr (``break`` breakLabel) (block [] exprs') 
                    let loopExpr = loop (block [] [addAssign indexExpr (constant 1); brachExpr; accExpr]) breakLabel continueLabel 
                    block (indexExpr :: varExprs) [initExpr; indexAssignExpr; loopExpr; returnExpr] :> _

            | Transform (Lambda ([paramExpr], body), queryExpr') ->
                    let exprs' = assign paramExpr current :: assign current body :: exprs
                    compile queryExpr' initExpr accExpr returnExpr (paramExpr :: varExprs) exprs'
            | Filter (Lambda ([paramExpr], body), queryExpr') ->
                let exprs' = assign paramExpr current :: body :: exprs
                compile queryExpr' initExpr accExpr returnExpr (paramExpr :: varExprs) exprs'
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
        static member Compile<'T>(queryExpr : QueryExpr<'T>) : Func<'T> =
            let finalVarExpr = var "current" typeof<int>
            let accVarExpr = var "___acc___" typeof<int>
            let initExpr = assign accVarExpr (constant 0)
            let accExpr = addAssign accVarExpr finalVarExpr
            let expr = compile queryExpr.QueryExpr initExpr accExpr accVarExpr [accVarExpr; finalVarExpr] []
            let func = Expression.Lambda<Func<int>>(expr).Compile()
            let result = func.Invoke()
            raise <| new NotImplementedException()


        [<System.Runtime.CompilerServices.Extension>]
        static member Run<'T>(queryExpr : QueryExpr<'T>) : 'T =
            ExtensionMethods.Compile(queryExpr).Invoke()
            


        
        