namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions

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
        let block (varExprs : seq<ParameterExpression>) (exprs : seq<Expression>) = 
            Expression.Block(varExprs, exprs) :> Expression
        let assign leftExpr rightExpr = Expression.Assign(leftExpr, rightExpr) :> Expression
        let constant (value : 'T) = Expression.Constant(value, typeof<'T>) :> Expression

        let (|Lambda|_|) (expr : Expression) = 
            if expr :? LambdaExpression then 
                let lambdaExpr = expr :?> LambdaExpression
                Some (Seq.toList lambdaExpr.Parameters, lambdaExpr.Body)
            else None

    [<AutoOpen>]
    module internal Compiler = 
        let current = Expression.Parameter(typeof<int>, "current")
        let rec compile (queryExpr : QueryExpr) 
            : ParameterExpression list -> Expression list -> Expression =
            match queryExpr with
            | Source (enumerable, t) ->
                (fun varExprs exprs ->
                    let exprs' = assign current (constant 10) :: exprs
                    block (current :: varExprs) exprs')
            | Transform (Lambda ([paramExpr], body), queryExpr') ->
                (fun varExprs exprs ->
                    let f = compile queryExpr'
                    let exprs' = (assign paramExpr current) :: body :: exprs
                    f (paramExpr :: varExprs) exprs')
            | Filter (lambdaExpr, queryExpr') ->
                raise <| new NotImplementedException()
            | _ -> failwithf "Invalid state %A" queryExpr 

    // LINQ-C# friendly extension methods 
    [<AutoOpen>]
    [<System.Runtime.CompilerServices.Extension>]
    module ExtensionMethods =
     
        [<System.Runtime.CompilerServices.Extension>]
        let AsQueryExpr(enumerable : IEnumerable<'T>) = 
            new QueryExpr<IEnumerable<'T>>(Source (enumerable, typeof<'T>))

        [<System.Runtime.CompilerServices.Extension>]
        let Select<'T, 'R>(queryExpr : QueryExpr<IEnumerable<'T>>, 
                            f : Expression<Func<'T, 'R>>) =
            new QueryExpr<IEnumerable<'R>>(Transform (f, queryExpr.QueryExpr))
            
        [<System.Runtime.CompilerServices.Extension>]
        let Where<'T>(queryExpr : QueryExpr<IEnumerable<'T>>, 
                            f : Expression<Func<'T, bool>>) =
            new QueryExpr<IEnumerable<'T>>(Filter (f, queryExpr.QueryExpr))

        [<System.Runtime.CompilerServices.Extension>]
        let Compile<'T>(queryExpr : QueryExpr<'T>) : Func<'T> =
            let expr = compile queryExpr.QueryExpr [] []
            let func = Expression.Lambda<Func<int>>(expr).Compile()
            let result = func.Invoke()
            raise <| new NotImplementedException()   

        [<System.Runtime.CompilerServices.Extension>]
        let Run<'T>(queryExpr : QueryExpr<'T>) : 'T =
            (Compile queryExpr).Invoke() 

        
        