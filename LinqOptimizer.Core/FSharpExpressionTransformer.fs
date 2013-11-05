namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    module FSharpExpressionTransformer =

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
                Transform (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)
            
            | MethodCall (_, MethodName "Filter" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], _) as f']) ; expr']) -> 
                Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
            
            | MethodCall (_, MethodName "Where" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], _) as f']) ; expr']) -> 
                Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)
            
            | MethodCall (_, MethodName "Take" _, [countExpr; expr' ]) when countExpr.Type = typeof<int> -> 
                let queryExpr = toQueryExpr expr'
                Take (countExpr, queryExpr, queryExpr.Type)
            
            | MethodCall (_, MethodName "Skip" _, [countExpr; expr' ]) when countExpr.Type = typeof<int> -> 
                let queryExpr = toQueryExpr expr'
                Skip (countExpr, queryExpr, queryExpr.Type)

            | MethodCall (_, (MethodName "Collect" [|_; _|] as m), [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], bodyExpr) as f']) ; expr']) -> 
                NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr', m.ReturnType.GetGenericArguments().[0])

            | MethodCall (_, MethodName "Sort" _, [expr']) -> 
                let query' = toQueryExpr expr'
                let v = var "___x___" query'.Type
                let f = Expression.Lambda(v,v)
                OrderBy ([f, Order.Ascending], query', query'.Type)

            | MethodCall (_, MethodName "SortBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], bodyExpr) as f']) ; expr']) -> 
                OrderBy ([f' :?> LambdaExpression, Order.Ascending], toQueryExpr expr', paramExpr.Type)

            | MethodCall (_, MethodName "Length" _, [expr']) -> 
                let query' = toQueryExpr expr'
                Count (query', query'.Type)

            | MethodCall (_, MethodName "GroupBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda ([paramExpr], bodyExpr) as f']) ; expr']) -> 
                let query' = GroupBy (f' :?> LambdaExpression, toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|paramExpr.Type; bodyExpr.Type|])
                
                let grp = var "___grp___" query'.Type
                
                // IEnumerable<'Body>
                let seqTy = typedefof<IEnumerable<_>>.MakeGenericType [| bodyExpr.Type|]
                
                // 'Param * IEnumerable<'Body>
                let tupleTy = typedefof<Tuple<_, _>>.MakeGenericType [|paramExpr.Type; seqTy |]
                
                let ci = tupleTy.GetConstructor([|paramExpr.Type; seqTy|])
                let body = Expression.New(ci, [ Expression.Property(grp, "Key") :> Expression; Expression.Convert(grp, seqTy) :> Expression ]) :> Expression
                let project = Expression.Lambda(body,[grp])
                Transform(project, query', tupleTy)

            //
            // Pipe (|>) optimizations
            //

            | PipedMethodCall1(expr', MethodName "Map" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                Transform (f' :?> LambdaExpression, toQueryExpr expr', bodyExpr.Type)

            | PipedMethodCall1(expr', MethodName "Filter" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)

            | PipedMethodCall1(expr', MethodName "Where" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                Filter (f' :?> LambdaExpression, toQueryExpr expr', paramExpr.Type)

            | PipedMethodCall1(expr', MethodName "Take" _, countExpr) ->
                let queryExpr = toQueryExpr expr'
                Take (countExpr, queryExpr, queryExpr.Type)

            | PipedMethodCall1(expr', MethodName "Skip" _, countExpr) ->
                let queryExpr = toQueryExpr expr'
                Skip (countExpr, queryExpr, queryExpr.Type)

            | PipedMethodCall1(expr', (MethodName "Collect" [|_; _|] as m), (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr', m.ReturnType.GetGenericArguments().[0])

            | PipedMethodCall0(expr', MethodName "Sort" _) ->
                let query' = toQueryExpr expr'
                let v = var "x" query'.Type
                let f = Expression.Lambda(v,v)
                OrderBy ([f, Order.Ascending], query', query'.Type)

            | PipedMethodCall1(expr', MethodName "SortBy" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                OrderBy ([f' :?> LambdaExpression, Order.Ascending], toQueryExpr expr', paramExpr.Type)

            | PipedMethodCall0(expr', MethodName "Length" _) ->
                let query' = toQueryExpr expr'
                Count (query', query'.Type)

            | PipedMethodCall1(expr', MethodName "GroupBy" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ Lambda ([paramExpr],bodyExpr) as f' ]))) ->
                let query' = GroupBy (f' :?> LambdaExpression, toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|paramExpr.Type; bodyExpr.Type|])
                
                let grp = var "___grp___" query'.Type
                
                // IEnumerable<'Body>
                let seqTy = typedefof<IEnumerable<_>>.MakeGenericType [| bodyExpr.Type|]
                
                // 'Param * IEnumerable<'Body>
                let tupleTy = typedefof<Tuple<_, _>>.MakeGenericType [|paramExpr.Type; seqTy |]
                
                let ci = tupleTy.GetConstructor([|paramExpr.Type; seqTy|])
                let body = Expression.New(ci, [ Expression.Property(grp, "Key") :> Expression; Expression.Convert(grp, seqTy) :> Expression ]) :> Expression
                let project = Expression.Lambda(body,[grp])
                Transform(project, query', tupleTy)

            | _ -> 
                if expr.Type.IsArray then
                    Source (expr, expr.Type.GetElementType())
                else
                    Source (expr, expr.Type.GetGenericArguments().[0])


//            | MethodCall (_, MethodName "ToArray" _, [expr']) -> 
//                ToArray(toQueryExpr expr')
