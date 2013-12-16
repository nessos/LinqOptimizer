    
namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Reflection.Emit

    module CompiledThunks = 
        let cache = Concurrent.ConcurrentDictionary<string, Func<obj>>()

    type CoreHelpers =
        

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
                RepeatGenerator(Expression.Convert(constant element, ty) , constant count)
            | _ -> 
                Source (constant enumerable, ty)

        static member private CompileToMethod(query : QueryExpr, compile : QueryExpr -> Expression) : Func<'T> =
            let expr = compile query
            
            let eraser = AnonymousTypeEraser()
            let expr = eraser.Visit(expr)
            
            let csv = ConstantLiftingTransformer()
            let expr' = csv.Visit(expr)
            let objs, pms = csv.Environment.Values.ToArray(), csv.Environment.Keys

            let func = Expression.Lambda(expr', pms)
            let source = func.ToString()
//            if CompiledThunks.cache.ContainsKey(source) then
//                Func<'T>(fun () -> CompiledThunks.cache.[source].Invoke() :?> 'T)
//            else
            let func = CoreHelpers.WrapInvocation(Session.Compile(func), objs)
                //let b = CompiledThunks.cache.TryAdd(source, func)
            Func<'T>(fun () -> func.Invoke() :?> 'T)

        static member private Compile(query : QueryExpr, compile : QueryExpr -> Expression) : Func<'T> =
            let expr = compile query
            let source = expr.ToString()
//            if CompiledThunks.cache.ContainsKey(source) then
//                Func<'T>(fun () -> CompiledThunks.cache.[source].Invoke() :?> 'T)
//            else
            let func = Expression.Lambda<Func<obj>>(expr).Compile()
                //CompiledThunks.cache.TryAdd(source, func) |> ignore
            Func<'T>(fun () -> func.Invoke() :?> 'T)

        static member private WrapInvocation<'T>(mi : MethodInfo, args : obj []) : Func<obj> =
            Func<obj>(
                fun () -> 
                    try mi.Invoke(null, args) 
                    with :? TargetInvocationException as ex -> 
                        if ex.InnerException :? MemberAccessException 
                        then raise <| Exception(
                                            "Attempting to access non public member or type from dynamic assembly. Consider making your type/member public or use the appropriate Run method.",
                                            ex.InnerException)
                        else raise ex.InnerException )

        static member Compile<'T>(queryExpr : QueryExpr, optimize : Func<Expression,Expression>) : Func<'T> =
            CoreHelpers.Compile<'T>(queryExpr, optimize, false)

        static member Compile(queryExpr : QueryExpr, optimize : Func<Expression,Expression>) : Action =
            CoreHelpers.Compile(queryExpr, optimize, false)

        static member CompileToParallel<'T>(queryExpr : QueryExpr,  optimize : Func<Expression,Expression>) : Func<'T> =
            CoreHelpers.CompileToParallel<'T>(queryExpr, optimize, false)




        static member Compile<'T>(queryExpr : QueryExpr, optimize : Func<Expression,Expression>, allowNonPublicMemberAccess : bool) : Func<'T> =
            if allowNonPublicMemberAccess then
                CoreHelpers.Compile(queryExpr, fun expr -> Compiler.compileToSequential expr optimize.Invoke)
            else
                CoreHelpers.CompileToMethod(queryExpr, fun expr -> Compiler.compileToSequential expr optimize.Invoke )
                

        static member Compile(queryExpr : QueryExpr, optimize : Func<Expression,Expression>,  allowNonPublicMemberAccess : bool) : Action =
            if allowNonPublicMemberAccess then
                let func = CoreHelpers.Compile(queryExpr, fun expr -> Compiler.compileToSequential expr optimize.Invoke)
                Action(fun () -> func.Invoke())
            else
                let func = CoreHelpers.CompileToMethod(queryExpr, fun expr -> Compiler.compileToSequential expr optimize.Invoke )
                Action(fun () -> func.Invoke())

        static member CompileToParallel<'T>(queryExpr : QueryExpr,  optimize : Func<Expression,Expression>, allowNonPublicMemberAccess : bool ) : Func<'T> =
            if allowNonPublicMemberAccess then
                CoreHelpers.Compile(queryExpr, fun expr -> Compiler.compileToParallel expr optimize.Invoke)
            else
                CoreHelpers.CompileToMethod(queryExpr,  fun expr -> Compiler.compileToParallel expr optimize.Invoke )
                


