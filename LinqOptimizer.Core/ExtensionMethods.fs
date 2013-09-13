    
namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection

    // LINQ-C# friendly extension methods 
    [<AutoOpen>]
    [<System.Runtime.CompilerServices.Extension>]
    type ExtensionMethods =
     
        [<System.Runtime.CompilerServices.Extension>]
        static member AsQueryExpr(enumerable : IEnumerable<'T>) = 
            new QueryExpr<IEnumerable<'T>>(Source (constant enumerable, typeof<'T>))

        [<System.Runtime.CompilerServices.Extension>]
        static member Compile<'T>(queryExpr : QueryExpr<'T>) : Func<'T> =
            let expr = Compiler.compile queryExpr.QueryExpr
            let func = Expression.Lambda<Func<'T>>(expr).Compile()
            func
            
        [<System.Runtime.CompilerServices.Extension>]
        static member Run<'T>(queryExpr : QueryExpr<'T>) : 'T =
            ExtensionMethods.Compile(queryExpr).Invoke()

        [<System.Runtime.CompilerServices.Extension>]
        static member Select<'T, 'R>(queryExpr : QueryExpr<IEnumerable<'T>>, selector : Expression<Func<'T, 'R>>) =
            new QueryExpr<IEnumerable<'R>>(Transform (selector, queryExpr.QueryExpr, typeof<'R>))
            
        [<System.Runtime.CompilerServices.Extension>]
        static member Where<'T>(queryExpr : QueryExpr<IEnumerable<'T>>, predicate : Expression<Func<'T, bool>>) =
            new QueryExpr<IEnumerable<'T>>(Filter (predicate, queryExpr.QueryExpr, typeof<'T>))


        [<System.Runtime.CompilerServices.Extension>]
        static member Aggregate(queryExpr : QueryExpr<IEnumerable<'T>>, seed : 'Acc, func : Expression<Func<'Acc, 'T, 'Acc>>) =
            new QueryExpr<'Acc>(Aggregate ((seed :> _, typeof<'Acc>), func, queryExpr.QueryExpr))

        [<System.Runtime.CompilerServices.Extension>]
        static member Sum(queryExpr : QueryExpr<IEnumerable<double>>) =
            new QueryExpr<double>(Sum (queryExpr.QueryExpr, typeof<double>))

        [<System.Runtime.CompilerServices.Extension>]
        static member Sum(queryExpr : QueryExpr<IEnumerable<int>>) =
            new QueryExpr<int>(Sum (queryExpr.QueryExpr, typeof<int>))



        [<System.Runtime.CompilerServices.Extension>]
        static member SelectMany<'T, 'Col, 'R>(queryExpr : QueryExpr<IEnumerable<'T>>, 
                                                collectionSelector : Expression<Func<'T, IEnumerable<'Col>>>, 
                                                resultSelector : Expression<Func<'T, 'Col, 'R>>) : QueryExpr<IEnumerable<'R>> =
            let queryExpr' = 
                match collectionSelector with
                | Lambda ([paramExpr], bodyExpr) ->
                    NestedQueryTransform ((paramExpr, Compiler.toQueryExpr bodyExpr), resultSelector, queryExpr.QueryExpr, typeof<'R>)
                | _ -> failwithf "Invalid state %A" collectionSelector

            new QueryExpr<IEnumerable<'R>>(queryExpr')

        [<System.Runtime.CompilerServices.Extension>]
        static member SelectMany<'T, 'R>(queryExpr : QueryExpr<IEnumerable<'T>>, selector : Expression<Func<'T, IEnumerable<'R>>>) : QueryExpr<IEnumerable<'R>> =
            let queryExpr' = 
                match selector with
                | Lambda ([paramExpr], bodyExpr) ->
                    NestedQuery ((paramExpr, Compiler.toQueryExpr bodyExpr), queryExpr.QueryExpr, typeof<'R>)
                | _ -> failwithf "Invalid state %A" selector

            new QueryExpr<IEnumerable<'R>>(queryExpr')


