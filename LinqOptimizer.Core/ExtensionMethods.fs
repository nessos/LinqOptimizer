    
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
            new QueryExpr<IEnumerable<'T>>(Source (enumerable, typeof<'T>))

        [<System.Runtime.CompilerServices.Extension>]
        static member Select<'T, 'R>(queryExpr : QueryExpr<IEnumerable<'T>>, f : Expression<Func<'T, 'R>>) =
            new QueryExpr<IEnumerable<'R>>(Transform (f, queryExpr.QueryExpr))
            
        [<System.Runtime.CompilerServices.Extension>]
        static member Where<'T>(queryExpr : QueryExpr<IEnumerable<'T>>, f : Expression<Func<'T, bool>>) =
            new QueryExpr<IEnumerable<'T>>(Filter (f, queryExpr.QueryExpr))


        [<System.Runtime.CompilerServices.Extension>]
        static member Aggregate(queryExpr : QueryExpr<IEnumerable<'T>>, seed : 'Acc, f : Expression<Func<'Acc, 'T, 'Acc>>) =
            new QueryExpr<'Acc>(Aggregate ((seed :> _, typeof<'Acc>), f, queryExpr.QueryExpr))

        [<System.Runtime.CompilerServices.Extension>]
        static member Sum(queryExpr : QueryExpr<IEnumerable<double>>) =
            new QueryExpr<double>(Sum (queryExpr.QueryExpr, typeof<double>))

        [<System.Runtime.CompilerServices.Extension>]
        static member Sum(queryExpr : QueryExpr<IEnumerable<int>>) =
            new QueryExpr<int>(Sum (queryExpr.QueryExpr, typeof<int>))

        [<System.Runtime.CompilerServices.Extension>]
        static member Compile<'T>(queryExpr : QueryExpr<'T>) : Func<'T> =
            let expr = Compiler.compile queryExpr.QueryExpr
            let func = Expression.Lambda<Func<'T>>(expr).Compile()
            func
            
        [<System.Runtime.CompilerServices.Extension>]
        static member Run<'T>(queryExpr : QueryExpr<'T>) : 'T =
            ExtensionMethods.Compile(queryExpr).Invoke()


