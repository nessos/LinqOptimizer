namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    module private FSharpExpressionOptimizerHelpers =

        let (|PipedMethodCall1|_|) (expr : Expression) =
            match expr with
            | MethodCall (_, MethodName "op_PipeRight" _, [expr'; MethodCall(Lambda([m], MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda([s], MethodCall(_, mi, [m'; s']) )]) ) , MethodName "Invoke" _, [ f' ]) ]) when m :> Expression = m' && s :> Expression = s' -> 
                Some(expr', mi, f')
            | _ -> None

        let (|PipedMethodCall0|_|) (expr : Expression) =
            match expr with
            | MethodCall (_, MethodName "op_PipeRight" _, [expr'; MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda([s], MethodCall(_, mi, [s']) )]) ]) when s :> Expression = s' -> 
                Some(expr', mi)
            | _ -> None        

        // F# call patterns
        // TODO: expr type checks
        let rec toQueryExpr (expr : Expression) : QueryExpr =
            match expr with
            | MethodCall (_, MethodName "Map" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([_], bodyExpr) as f']) ; expr']) -> 
                Transform (f' :?> LambdaExpression, toQueryExpr expr')
            
            | MethodCall (_, MethodName "Filter" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], _) as f']) ; expr']) -> 
                Filter (f' :?> LambdaExpression, toQueryExpr expr')
            
            | MethodCall (_, MethodName "Where" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], _) as f']) ; expr']) -> 
                Filter (f' :?> LambdaExpression, toQueryExpr expr')
            
            | MethodCall (_, MethodName "Take" _, [countExpr; expr' ]) when countExpr.Type = typeof<int> -> 
                Take (countExpr, toQueryExpr expr')
            
            | MethodCall (_, MethodName "Skip" _, [countExpr; expr' ]) when countExpr.Type = typeof<int> -> 
                Skip (countExpr, toQueryExpr expr')

            | MethodCall (_, (MethodName "Collect" [|_; _|] as m), [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], bodyExpr) as f']) ; expr']) -> 
                NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr')

            | MethodCall (_, MethodName "Sort" _, [expr']) -> 
                let query' = toQueryExpr expr'
                let v = var "___x___" query'.Type
                let f = Expression.Lambda(v,v)
                OrderBy ([f, Order.Ascending], query')

            | MethodCall (_, MethodName "SortBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], bodyExpr) as f']) ; expr']) -> 
                OrderBy ([f' :?> LambdaExpression, Order.Ascending], toQueryExpr expr')

            | MethodCall (_, MethodName "Length" _, [expr']) -> 
                Count (toQueryExpr expr')

            | MethodCall (_, MethodName "GroupBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], bodyExpr) as f']) ; expr']) -> 
                let query' = GroupBy (f' :?> LambdaExpression, toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|bodyExpr.Type; paramExpr.Type |])
                
                let grp = var "___grp___" query'.Type
                
                // IEnumerable<'Body>
                let seqTy = typedefof<IEnumerable<_>>.MakeGenericType [| bodyExpr.Type|]
                
                // 'Param * IEnumerable<'Body>
                let tupleTy = typedefof<Tuple<_, _>>.MakeGenericType [|paramExpr.Type; seqTy |]
                
                let ci = tupleTy.GetConstructor([|paramExpr.Type; seqTy|])
                let body = Expression.New(ci, [ Expression.Property(grp, "Key") :> Expression; Expression.Convert(grp, seqTy) :> Expression ]) :> Expression
                let project = Expression.Lambda(body,[grp])
                Transform(project, query')

            //
            // Pipe (|>) optimizations
            //

            | PipedMethodCall1(expr', MethodName "Map" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                Transform (f' :?> LambdaExpression, toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "Filter" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                Filter (f' :?> LambdaExpression, toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "Where" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                Filter (f' :?> LambdaExpression, toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "Take" _, countExpr) ->
                Take (countExpr, toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "Skip" _, countExpr) ->
                Skip (countExpr, toQueryExpr expr')

            | PipedMethodCall1(expr', (MethodName "Collect" [|_; _|] as m), (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr')

            | PipedMethodCall0(expr', MethodName "Sort" _) ->
                let query' = toQueryExpr expr'
                let v = var "x" query'.Type
                let f = Expression.Lambda(v,v)
                OrderBy ([f, Order.Ascending], query')

            | PipedMethodCall1(expr', MethodName "SortBy" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                OrderBy ([f' :?> LambdaExpression, Order.Ascending], toQueryExpr expr')

            | PipedMethodCall0(expr', MethodName "Length" _) ->
                Count (toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "GroupBy" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                let query' = GroupBy (f' :?> LambdaExpression, toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|bodyExpr.Type; paramExpr.Type |])
                
                let grp = var "___grp___" query'.Type
                
                // IEnumerable<'Body>
                let seqTy = typedefof<IEnumerable<_>>.MakeGenericType [| bodyExpr.Type|]
                
                // 'Param * IEnumerable<'Body>
                let tupleTy = typedefof<Tuple<_, _>>.MakeGenericType [|paramExpr.Type; seqTy |]
                
                let ci = tupleTy.GetConstructor([|paramExpr.Type; seqTy|])
                let body = Expression.New(ci, [ Expression.Property(grp, "Key") :> Expression; Expression.Convert(grp, seqTy) :> Expression ]) :> Expression
                let project = Expression.Lambda(body,[grp])
                Transform(project, query')

            | NotNull expr -> 
                if expr.Type.IsArray then
                    Source (expr, expr.Type.GetElementType(), QueryExprType.Sequential)
                elif expr.Type.IsGenericType && expr.Type.GetGenericTypeDefinition() = typedefof<IEnumerable<_>> then
                    Source(expr, expr.Type.GetGenericArguments().[0], QueryExprType.Sequential)
                elif expr.Type.IsGenericType then
                    Source (expr, expr.Type.GetInterface("IEnumerable`1").GetGenericArguments().[0], QueryExprType.Sequential)
                else
                    failwithf "Not supported source %A" expr.Type
            | _ ->
                invalidArg "expr" "Cannot extract QueryExpr from null Expression"

        and private transformer (expr : Expression) : Expression option =
            match expr with
            | MethodCall (_, MethodName "Map" _,            [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda _ ]) ; _])
            | MethodCall (_, MethodName "Filter" _,         [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda _ ]) ; _]) 
            | MethodCall (_, MethodName "Where" _,          [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda _ ]) ; _]) 
            | MethodCall (_, MethodName "Take" _,           [_; _ ]) 
            | MethodCall (_, MethodName "Skip" _,           [_; _ ])
            | MethodCall (_, MethodName "Collect" [|_; _|], [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda _ ]) ; _]) 
            | MethodCall (_, MethodName "Sort" _,           [_])
            | MethodCall (_, MethodName "SortBy" _,         [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda _ ]) ; _])
            | MethodCall (_, MethodName "Length" _,         [_]) 
            | MethodCall (_, MethodName "GroupBy" _,        [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda _ ]) ; _]) ->
                let query = toQueryExpr expr
                let expr = (Compiler.compileToSequential query optimize)
                Some expr
            // Using all the match cases at once, causes F# compiler to produce invalid CLR code! <3
            | _ ->
            match expr with
            //
            | PipedMethodCall1(_, MethodName "Map" _,           (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda _ ]))) 
            | PipedMethodCall1(_, MethodName "Filter" _,        (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda _ ])))
            | PipedMethodCall1(_, MethodName "Where" _,         (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda _])))
            | PipedMethodCall1(_, MethodName "Take" _,          _) 
            | PipedMethodCall1(_, MethodName "Skip" _,          _) 
            | PipedMethodCall1(_, MethodName "Collect" [|_; _|],(MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda _ ])))
            | PipedMethodCall0(_, MethodName "Sort" _) 
            | PipedMethodCall1(_, MethodName "SortBy" _,        (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda _ ]))) 
            | PipedMethodCall0(_, MethodName "Length" _) 
            | PipedMethodCall1(_, MethodName "GroupBy" _,       (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda _ ]))) ->
                let query = toQueryExpr expr
                let expr = (Compiler.compileToSequential query optimize)
                Some expr
            | _ ->
                None

        and private opt = ExpressionTransformer.transform transformer
        
        and optimize (expr : Expression) : Expression = opt expr

    type FSharpExpressionOptimizer =
        static member Optimize(expr : Expression) : Expression =
            FSharpExpressionOptimizerHelpers.optimize(expr)
        static member ToQueryExpr(expr : Expression) : QueryExpr =
            FSharpExpressionOptimizerHelpers.toQueryExpr(expr)