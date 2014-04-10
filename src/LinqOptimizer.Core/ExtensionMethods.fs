    
namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Reflection.Emit


    module CompiledThunks = 
        let cache = Concurrent.ConcurrentDictionary<string, Delegate>()

    type CoreHelpers =
        

        static member AsQueryExpr(enumerable : IEnumerable, ty : Type) : QueryExpr = 
            // Hack removed
            //// Hack to optimize Enumerable.Range and Enumerable.Repeat calls
            //// TODO : check Mono generated types
            //let t = enumerable.GetType()
            //match t.FullName with
            //| s when s.StartsWith "System.Linq.Enumerable+<RangeIterator>"  ->
            //    let start = t.GetFields().First(fun f -> f.Name.EndsWith "__start").GetValue(enumerable)
            //    let count = t.GetFields().First(fun f -> f.Name.EndsWith "__count").GetValue(enumerable)
            //    RangeGenerator(constant start , constant count)
            //| s when s.StartsWith "System.Linq.Enumerable+<RepeatIterator>"  ->
            //    let element = t.GetFields().First(fun f -> f.Name.EndsWith "__element").GetValue(enumerable)
            //    let count   = t.GetFields().First(fun f -> f.Name.EndsWith "__count").GetValue(enumerable)
            //    RepeatGenerator(Expression.Convert(constant element, ty) , constant count)
            //| _ -> 
                Source (constant enumerable, ty, QueryExprType.Sequential)

        static member private CompileToMethod(query : QueryExpr, compile : QueryExpr -> Expression) : Func<'T> =
            let source = query.ToString()
            let expr = compile query
            let expr = TupleElimination.apply(expr)
            let expr = AnonymousTypeEraser.apply(expr)
            let expr, pms, objs = ConstantLiftingTransformer.apply(expr)
            
            if CompiledThunks.cache.ContainsKey(source) then
                let func = CompiledThunks.cache.[source] :?> Func<obj[], obj>
                Func<'T>(fun () -> func.Invoke(objs) :?> 'T)
            else
                let lambda = Expression.Lambda(expr, pms)
                let methodInfo = Session.Compile(lambda)
                let iaccs = AccessChecker.check lambda
                let func = CoreHelpers.WrapInvocation(methodInfo, iaccs) 
                CompiledThunks.cache.TryAdd(source, func) |> ignore
                Func<'T>(fun () -> func.Invoke(objs) :?> 'T)

        static member private Compile(query : QueryExpr, compile : QueryExpr -> Expression) : Func<'T> =
            let source = sprintf "allowNonPublicMemberAccess query (%s)" <| query.ToString()
            let expr = compile query
            let expr = TupleElimination.apply(expr)
            let expr = AnonymousTypeEraser.apply(expr)
            let expr, pms, objs = ConstantLiftingTransformer.apply(expr)

            if CompiledThunks.cache.ContainsKey(source) then
                let func = CompiledThunks.cache.[source]
                Func<'T>(fun () -> func.DynamicInvoke(objs) :?> 'T)
            else
                let lambda = Expression.Lambda(expr, pms)
                let func = lambda.Compile()
                CompiledThunks.cache.TryAdd(source, func) |> ignore
                Func<'T>(fun () -> func.DynamicInvoke(objs) :?> 'T)

        static member private WrapInvocation<'T>(mi : MethodInfo, iaccs : seq<Expression * (string option)> option) : Func<obj [], obj> =
            Func<obj[], obj>(
                fun (args : obj[]) -> 
                    try mi.Invoke(null, args) 
                    with :? TargetInvocationException as ex -> 
                        if ex.InnerException :? MemberAccessException then 
                            let msg =
                                "Attempting to access non public member or type from dynamic assembly. Consider making your type/member public or use the appropriate Run method.\n" +
                                match iaccs with
                                | None -> String.Empty
                                | Some iaccs ->
                                    "Possible invalid accesses :\n" +
                                        (iaccs 
                                        |> Seq.map (fun (expr, msg) -> 
                                                let msg = match msg with None -> String.Empty | Some msg -> msg
                                                sprintf "At expression : %A, %s" expr msg)
                                        |> String.concat "\n")
                            raise <| Exception(msg, ex.InnerException)
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
             

        static member CompileTemplateVariadic<'R>(parameters : ParameterExpression [], template : QueryExpr, optimize : Func<Expression,Expression>, allowNonPublicMemberAccess : bool) =
            let func = 
                if allowNonPublicMemberAccess then
                    CoreHelpers.Compile(template, 
                        fun query -> 
                            let expr = Compiler.compileToSequential query optimize.Invoke
                            let lam = lambda parameters expr
                            lam :> Expression) 
                else
                    CoreHelpers.CompileToMethod(template, 
                        fun query -> 
                            let expr = Compiler.compileToSequential query optimize.Invoke
                            let lam = lambda parameters expr
                            lam :> _) 
            let template = func.Invoke()
            template

        static member CompileActionTemplateVariadic(parameters : ParameterExpression [], template : QueryExpr, optimize : Func<Expression,Expression>, allowNonPublicMemberAccess : bool) : obj =
            let func = 
                if allowNonPublicMemberAccess then
                    CoreHelpers.Compile(template, 
                        fun query -> 
                            let expr = Compiler.compileToSequential query optimize.Invoke
                            let lam = lambda parameters expr
                            lam :> Expression) 
                else
                    CoreHelpers.CompileToMethod(template, 
                        fun query -> 
                            let expr = Compiler.compileToSequential query optimize.Invoke
                            let lam = lambda parameters expr
                            lam :> _) 
            let template = func.Invoke()
            template

        static member CompileTemplateToParallelVariadic<'R>(parameters : ParameterExpression [], template : QueryExpr, optimize : Func<Expression,Expression>, allowNonPublicMemberAccess : bool) =
            let func = 
                if allowNonPublicMemberAccess then
                    CoreHelpers.Compile(template, 
                        fun query -> 
                            let expr = Compiler.compileToParallel query optimize.Invoke
                            let lam = lambda parameters expr
                            lam :> Expression) 
                else
                    CoreHelpers.CompileToMethod(template, 
                        fun query -> 
                            let expr = Compiler.compileToParallel query optimize.Invoke
                            let lam = lambda parameters expr
                            lam :> _) 
            let template = func.Invoke()
            template