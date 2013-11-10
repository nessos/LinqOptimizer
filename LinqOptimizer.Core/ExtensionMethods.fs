    
namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Reflection.Emit


    type CoreExts =
        static member AsQueryExpr(enumerable : IEnumerable, ty : Type) : QueryExpr = 
            // Hack to optimize Enumerable.Range and Enumerable.Repeat calls
            // TODO : check Mono generated types
            let t = enumerable.GetType()
            match t.FullName with
            | s when s.StartsWith "System.Linq.Enumerable+<RangeIterator>"  ->
                let start = t.GetFields().First(fun f -> f.Name.EndsWith "__start").GetValue(enumerable)
                let count = t.GetFields().First(fun f -> f.Name.EndsWith "__count").GetValue(enumerable)
                RangeGenerator(constant start , constant count)
            | s when s.StartsWith "System.Linq.Enumerable+<RepeatIterator>"  ->
                let element = t.GetFields().First(fun f -> f.Name.EndsWith "__element").GetValue(enumerable)
                let count   = t.GetFields().First(fun f -> f.Name.EndsWith "__count").GetValue(enumerable)
                RepeatGenerator(constant element, ty ,constant count)
            | _ -> 
                Source (constant enumerable, ty)

        static member private Compile(query : QueryExpr, compile : QueryExpr -> Expression) : MethodInfo * obj [] =
            let expr = compile query
            let csv = ConstantLiftingTransformer()
            let expr' = csv.Visit(expr)
            let objs, pms = csv.Environment.Values.ToArray(), csv.Environment.Keys

            let func = Expression.Lambda(expr', pms)
            Session.Compile(func), objs

        static member private WrapInvocation<'T>(mi : MethodInfo, args : obj []) =
            fun () -> 
                try mi.Invoke(null, args) :?> 'T
                with :? TargetInvocationException as ex -> raise ex.InnerException 

        static member Compile<'T>(queryExpr : QueryExpr) : Func<'T> =
            let mi, objs = CoreExts.Compile(queryExpr, Compiler.compileToSequential)
            Func<'T>(CoreExts.WrapInvocation(mi, objs))

        static member Compile(queryExpr : QueryExpr) : Action =
            let mi, objs = CoreExts.Compile(queryExpr, Compiler.compileToSequential)
            Action(CoreExts.WrapInvocation(mi, objs))

        static member CompileToParallel<'T>(queryExpr : QueryExpr) : Func<'T> =
            let mi, objs = CoreExts.Compile(queryExpr, Compiler.compileToParallel)
            Func<'T>(CoreExts.WrapInvocation(mi, objs))

        static member SelectManyCSharp<'T, 'Col, 'R>(queryExpr : QueryExpr, 
                                                     collectionSelector : Expression<Func<'T, IEnumerable<'Col>>>, 
                                                     resultSelector : Expression<Func<'T, 'Col, 'R>>) : QueryExpr =
            let selector = CSharpExpressionOptimizer.Optimize collectionSelector
            match collectionSelector with
            | Lambda ([paramExpr], bodyExpr) ->
                NestedQueryTransform ((paramExpr, CSharpExpressionOptimizer.ToQueryExpr bodyExpr), CSharpExpressionOptimizer.Optimize resultSelector :?> _, queryExpr, typeof<'R>)
            | _ -> failwithf "Invalid state %A" collectionSelector

        static member SelectManyCSharp<'T, 'R>(queryExpr : QueryExpr, selector : Expression<Func<'T, IEnumerable<'R>>>) : QueryExpr = 
            let selector = CSharpExpressionOptimizer.Optimize selector
            match selector with
            | Lambda ([paramExpr], bodyExpr) ->
                NestedQuery ((paramExpr, CSharpExpressionOptimizer.ToQueryExpr bodyExpr), queryExpr, typeof<'R>)
            | _ -> failwithf "Invalid state %A" selector
