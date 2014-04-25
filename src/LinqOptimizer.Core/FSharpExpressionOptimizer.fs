namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    module private FSharpExpressionOptimizerHelpers =

        let private (|PipedMethodCall1|_|) (expr : Expression) =
            match expr with
            | MethodCall (_, MethodName "op_PipeRight" _, [expr'; MethodCall(Lambda([m], MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda([s], MethodCall(_, mi, [m'; s']) )]) ) , MethodName "Invoke" _, [ f' ]) ]) when m :> Expression = m' && s :> Expression = s' -> 
                Some(expr', mi, f')
            | _ -> None

        let private (|PipedMethodCall0|_|) (expr : Expression) =
            match expr with
            | MethodCall (_, MethodName "op_PipeRight" _, [expr'; MethodCall(_, MethodName "ToFSharpFunc" _, [Lambda([s], MethodCall(_, mi, [s']) )]) ]) when s :> Expression = s' -> 
                Some(expr', mi)
            | _ -> None   
            
        let private sourceOfExpr (expr : Expression) sourceType : QueryExpr =
                if expr.Type.IsArray then
                    Source (expr, expr.Type.GetElementType(), sourceType)
                elif expr.Type.IsGenericType && expr.Type.GetGenericTypeDefinition() = typedefof<IEnumerable<_>> then
                    Source(expr, expr.Type.GetGenericArguments().[0], sourceType)
                elif expr.Type.IsGenericType then
                    Source (expr, expr.Type.GetInterface("IEnumerable`1").GetGenericArguments().[0], sourceType)
                else
                    failwithf "Not supported source %A" expr

        // F# call patterns
        // TODO: expr type checks
        let rec toQueryExpr (expr : Expression) : QueryExpr =
            match expr with
            | MethodCall (_, MethodName "Map" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([_], bodyExpr, f')]) ; expr'])
            | MethodCall (_, MethodName "map" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([_], bodyExpr, f')]) ; expr']) -> 
                Transform (f' , toQueryExpr expr')
            
            | MethodCall (_, MethodName "Filter" _ , [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], _, f')]) ; expr']) 
            | MethodCall (_, MethodName "filter" _ , [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], _, f')]) ; expr']) -> 
                Filter (f', toQueryExpr expr')
            
            | MethodCall (_, MethodName "Where" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], _, f')]) ; expr']) 
            | MethodCall (_, MethodName "where" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], _, f')]) ; expr']) -> 
                Filter (f', toQueryExpr expr')
            
            | MethodCall (_, MethodName "Take" _, [countExpr; expr' ])
            | MethodCall (_, MethodName "take" _, [countExpr; expr' ]) when countExpr.Type = typeof<int> ->
                Take (countExpr, toQueryExpr expr')
            
            | MethodCall (_, MethodName "Skip" _, [countExpr; expr' ]) 
            | MethodCall (_, MethodName "skip" _, [countExpr; expr' ]) when countExpr.Type = typeof<int> -> 
                Skip (countExpr, toQueryExpr expr')

            | MethodCall (_, (MethodName "Collect" [|_; _|] as m), [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], bodyExpr, f')]) ; expr']) 
            | MethodCall (_, (MethodName "collect" [|_; _|] as m), [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], bodyExpr, f')]) ; expr']) -> 
                NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr')

            | MethodCall (_, MethodName "Sort" _, [expr'])
            | MethodCall (_, MethodName "sort" _, [expr']) -> 
                let query' = toQueryExpr expr'
                let v = var "___x___" query'.Type
                let f = Expression.Lambda(v,v)
                OrderBy ([f, Order.Ascending], query')

            | MethodCall (_, MethodName "SortBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], bodyExpr, f')]) ; expr']) 
            | MethodCall (_, MethodName "sortBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], bodyExpr, f')]) ; expr']) ->
                OrderBy ([f', Order.Ascending], toQueryExpr expr')

            | MethodCall (_, MethodName "Length" _, [expr']) 
            | MethodCall (_, MethodName "length" _, [expr']) -> 
                Count (toQueryExpr expr')

            | MethodCall (_, MethodName "sum" _, [expr']) 
            | MethodCall (_, MethodName "Sum" _, [expr']) -> 
                Sum (toQueryExpr expr')

            | MethodCall (_, MethodName "Range" _, [startExpr; countExpr]) ->
                RangeGenerator(startExpr, countExpr) 

            | MethodCall (_, MethodName "Repeat" _, [objExpr; countExpr]) ->
                RepeatGenerator(objExpr, countExpr) 

            | MethodCall (_, MethodName "GroupBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], bodyExpr, f')]) ; expr']) 
            | MethodCall (_, MethodName "groupBy" _, [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote ([paramExpr], bodyExpr, f')]) ; expr']) -> 
                let query' = GroupBy (f', toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|bodyExpr.Type; paramExpr.Type |])
                
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

            | PipedMethodCall1(expr', MethodName "Map" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ])))
            | PipedMethodCall1(expr', MethodName "map" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ]))) ->
                Transform (f', toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "Filter" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ]))) 
            | PipedMethodCall1(expr', MethodName "filter" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ]))) ->
                Filter (f', toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "Where" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ])))
            | PipedMethodCall1(expr', MethodName "where" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ]))) ->
                Filter (f', toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "Take" _, countExpr) 
            | PipedMethodCall1(expr', MethodName "take" _, countExpr) ->
                Take (countExpr, toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "Skip" _, countExpr) 
            | PipedMethodCall1(expr', MethodName "skip" _, countExpr) ->
                Skip (countExpr, toQueryExpr expr')

            | PipedMethodCall1(expr', (MethodName "Collect" [|_; _|] as m), (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ])))
            | PipedMethodCall1(expr', (MethodName "collect" [|_; _|] as m), (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ]))) ->
                NestedQuery ((paramExpr, toQueryExpr bodyExpr), toQueryExpr expr')

            | PipedMethodCall0(expr', MethodName "Sort" _) 
            | PipedMethodCall0(expr', MethodName "sort" _) ->
                let query' = toQueryExpr expr'
                let v = var "x" query'.Type
                let f = Expression.Lambda(v,v)
                OrderBy ([f, Order.Ascending], query')

            | PipedMethodCall1(expr', MethodName "SortBy" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ])))
            | PipedMethodCall1(expr', MethodName "sortBy" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ]))) ->
                OrderBy ([f', Order.Ascending], toQueryExpr expr')

            | PipedMethodCall0(expr', MethodName "Length" _) 
            | PipedMethodCall0(expr', MethodName "length" _) ->
                Count (toQueryExpr expr')

            | PipedMethodCall0(expr', MethodName "Sum" _) 
            | PipedMethodCall0(expr', MethodName "sum" _) ->
                Sum (toQueryExpr expr')

            | PipedMethodCall1(expr', MethodName "GroupBy" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ])))
            | PipedMethodCall1(expr', MethodName "groupBy" _, (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote ([paramExpr],bodyExpr, f') ]))) ->
                let query' = GroupBy (f', toQueryExpr expr', typedefof<IGrouping<_, _>>.MakeGenericType [|bodyExpr.Type; paramExpr.Type |])
                
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
            // Source
            //

            | MethodCall (_, (MethodName "ofSeq" _ as mi), [ expr' ]) when mi.DeclaringType.FullName = "Nessos.LinqOptimizer.FSharp.Query" -> 
                sourceOfExpr expr' QueryExprType.Sequential

            | MethodCall (_, (MethodName "ofSeq" _ as mi), [ expr' ]) when mi.DeclaringType.FullName = "Nessos.LinqOptimizer.FSharp.PQuery" -> 
                sourceOfExpr expr' QueryExprType.Parallel

            | PipedMethodCall0(expr', (MethodName "ofSeq" _ as mi)) when mi.DeclaringType.FullName = "Nessos.LinqOptimizer.FSharp.Query" ->
                sourceOfExpr expr' QueryExprType.Sequential

            | PipedMethodCall0(expr', (MethodName "ofSeq" _ as mi)) when mi.DeclaringType.FullName = "Nessos.LinqOptimizer.FSharp.PQuery" ->
                sourceOfExpr expr' QueryExprType.Sequential

            | NotNull expr' -> 
                sourceOfExpr expr' QueryExprType.Sequential

            | _ ->
                invalidArg "expr" "Cannot extract QueryExpr from null Expression"

        and private transformer (expr : Expression) : Expression option =
            match expr with
            | MethodCall (_, MethodName "Map" _,            [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote _ ]) ; _])
            | MethodCall (_, MethodName "Filter" _,         [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote _ ]) ; _]) 
            | MethodCall (_, MethodName "Where" _,          [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote _ ]) ; _]) 
            | MethodCall (_, MethodName "Take" _,           [_; _ ]) 
            | MethodCall (_, MethodName "Skip" _,           [_; _ ])
            | MethodCall (_, MethodName "Collect" [|_; _|], [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote _ ]) ; _]) 
            | MethodCall (_, MethodName "Sort" _,           [_])
            | MethodCall (_, MethodName "SortBy" _,         [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote _ ]) ; _])
            | MethodCall (_, MethodName "Length" _,         [_]) 
            | MethodCall (_, MethodName "Sum" _,            [_]) 
            | MethodCall (_, MethodName "GroupBy" _,        [ MethodCall(_, MethodName "ToFSharpFunc" _, [LambdaOrQuote _ ]) ; _]) ->
                let query = toQueryExpr expr
                let expr = (Compiler.compileToSequential query optimize)
                Some expr
            // Using all the match cases at once, causes F# compiler to produce invalid CLR code! <3
            | _ ->
            match expr with
            //
            | PipedMethodCall1(_, MethodName "Map" _,           (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote _ ]))) 
            | PipedMethodCall1(_, MethodName "Filter" _,        (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote _ ])))
            | PipedMethodCall1(_, MethodName "Where" _,         (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote _])))
            | PipedMethodCall1(_, MethodName "Take" _,          _) 
            | PipedMethodCall1(_, MethodName "Skip" _,          _) 
            | PipedMethodCall1(_, MethodName "Collect" [|_; _|],(MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote _ ])))
            | PipedMethodCall0(_, MethodName "Sort" _) 
            | PipedMethodCall1(_, MethodName "SortBy" _,        (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote _ ]))) 
            | PipedMethodCall0(_, MethodName "Length" _) 
            | PipedMethodCall0(_, MethodName "Sum" _) 
            | PipedMethodCall1(_, MethodName "GroupBy" _,       (MethodCall(_, MethodName "ToFSharpFunc" _, [ LambdaOrQuote _ ]))) ->
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