namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Text
    open Nessos.LinqOptimizer.Core.Utils

//    // Typed Wrapper for QueryExpr 
//    type QueryExpr<'T>(queryExpr : QueryExpr) =
//        member self.QueryExpr = queryExpr 
//    type ParallelQueryExpr<'T>(queryExpr : QueryExpr) =
//        member self.QueryExpr = queryExpr 
    // C# friendly QueryExpr<unit>
//    and QueryExprVoid(queryExpr : QueryExpr) =
//        member self.QueryExpr = queryExpr 
//    and ParallelQueryExprVoid(queryExpr : QueryExpr) =
//        member self.QueryExpr = queryExpr 
    // Main Query representation
    type QueryExprType = Sequential | Parallel | Gpu
    type Order = Ascending | Descending
    type ReductionType = Map | Filter | Count | Sum | Aggregate | ToList | ToArray | Iter | NestedQueryTransform 
    /// The type representing an query expression.
    and QueryExpr = 
        | Source of Expression * Type * QueryExprType
        | Generate of Expression * LambdaExpression * LambdaExpression * LambdaExpression
        | Transform of LambdaExpression * QueryExpr 
        | TransformIndexed of LambdaExpression * QueryExpr 
        | Filter of LambdaExpression * QueryExpr 
        | FilterIndexed of LambdaExpression * QueryExpr 
        | NestedQuery of (ParameterExpression * QueryExpr) * QueryExpr 
        | NestedQueryTransform of (ParameterExpression * QueryExpr) * LambdaExpression * QueryExpr 
        | Aggregate of Expression * LambdaExpression * QueryExpr
        | Sum of QueryExpr 
        | Count of QueryExpr 
        | Take of Expression * QueryExpr
        | TakeWhile of LambdaExpression * QueryExpr
        | SkipWhile of LambdaExpression * QueryExpr
        | Skip of Expression * QueryExpr 
        | ForEach of LambdaExpression * QueryExpr 
        | GroupBy of LambdaExpression * QueryExpr * Type
        | OrderBy of (LambdaExpression * Order) list * QueryExpr 
        | ToList of QueryExpr
        | ToArray of QueryExpr
        | RangeGenerator of Expression * Expression
        | RepeatGenerator of Expression * Expression
        | ZipWith of Expression * Expression * LambdaExpression
        with

        member self.Type = 
            match self with
            | Source (_, t, _) -> t
            | Generate(_,_,_, selector) -> selector.Body.Type
            | Transform (lambda, _) -> lambda.Body.Type
            | TransformIndexed (lambda, _) -> lambda.Body.Type
            | Filter (_, q) -> q.Type
            | FilterIndexed (_, q) -> q.Type
            | NestedQuery ((_, q), _) -> q.Type
            | NestedQueryTransform ((_, _), resultSelector, _) -> resultSelector.Body.Type
            | Aggregate (seed, _, _) -> seed.Type
            | Sum (q) -> q.Type
            | Count (q) -> q.Type
            | Take (_, q) -> q.Type
            | TakeWhile(_,q) -> q.Type
            | SkipWhile(_,q) -> q.Type
            | Skip (_, q) -> q.Type
            | ForEach (_, _) -> typeof<Void>
            | GroupBy (_, _, t) -> t
            | OrderBy (_, q) -> q.Type
            | ToList q -> q.Type
            | ToArray q -> q.Type
            | RangeGenerator _ -> typeof<int>
            | RepeatGenerator (objExpr,_) -> objExpr.Type
            | ZipWith (_,_,f) -> f.ReturnType

        override self.ToString() = 
            let str (expr : Expression ) = expr.ToString()
            let rec toString (query : QueryExpr) : string = 
                match query with
                | Source (expr, t, queryExprType) -> 
                    match queryExprType with
                    | Sequential -> sprintf' "Source (%s, %s, Sequential)" <| str expr <| t.ToString()
                    | Parallel -> sprintf' "Source (%s, %s, Parallel)" <| str expr <| t.ToString()
                    | Gpu -> sprintf' "Source (%s, %s, Gpu)" <| str expr <| t.ToString()
                | Generate(expr1, expr2, expr3, expr4) -> 
                    sprintf' "Generate (%s, %s, %s, %s)" <| str expr1 <| str expr2 <| str expr3 <| str expr4
                | Transform (lambda, query) -> 
                    sprintf' "Transform [%s](%s, %s)" <| lambda.Body.Type.ToString() <| str lambda <| toString query
                | TransformIndexed (lambda, query) -> 
                    sprintf' "TransformIndexed [%s](%s, %s)" <| lambda.Body.Type.ToString() <| str lambda <| toString query
                | Filter (lambda, query) -> 
                    sprintf' "Filter (%s, %s)" <| str lambda <| toString query
                | FilterIndexed (lambda, query) -> 
                    sprintf' "FilterIndexed (%s, %s)" <| str lambda <| toString query
                | NestedQuery ((param, query), query') -> 
                    sprintf' "NestedQuery [%s](%s, %s, %s)" <| query.Type.ToString() <| str param <| toString query <| toString query'
                | NestedQueryTransform ((param, query), resultSelector, query') -> 
                    sprintf' "NestedQueryTransform [%s](%s, %s, %s, %s)" <| query.Type.ToString() <| str param <| toString query <| str resultSelector <| toString query'
                | Aggregate (seed, acc, query) -> 
                    sprintf' "Aggregate [%s](%s, %s, %s)" <| acc.Body.Type.ToString() <| str seed <| str acc <| toString query
                | Sum (query) -> 
                    sprintf' "Sum (%s, %s)" <| toString query <| query.Type.ToString()
                | Count (query) -> 
                    sprintf' "Count (%s)" <| toString query
                | Take (takeCount, query) -> 
                    sprintf' "Take (%s, %s)" <| str takeCount <| toString query
                | TakeWhile(lambda, query) -> 
                    sprintf' "TakeWhile (%s, %s)" <| str lambda <| toString query
                | SkipWhile(lambda, query) -> 
                    sprintf' "SkipWhile (%s, %s)" <| str lambda <| toString query
                | Skip (skipCount, query) -> 
                    sprintf' "Skip (%s, %s)" <| str skipCount <| toString query
                | ForEach (lambda, query) -> 
                    sprintf' "ForEach (%s, %s)" <| str lambda <| toString query
                | GroupBy (lambda, query, _) -> 
                    sprintf' "GroupBy (%s, %s)" <| str lambda <| toString query
                | OrderBy (lambdaOrderPairs, query) ->
                    List.foldBack
                        (fun (lambda, order) s -> 
                            match order with
                            | Ascending -> sprintf' "OrderBy (%s, Asc, %s)" <| str lambda <| s
                            | Descending -> sprintf' "OrderBy (%s, Desc, %s)" <| str lambda <| s)
                        lambdaOrderPairs (toString query)
                | ToList query -> 
                    sprintf' "ToList [%s](%s)" <| query.Type.ToString() <| toString query
                | ToArray query -> 
                    sprintf' "ToArray [%s](%s)" <| query.Type.ToString() <| toString query
                | RangeGenerator (expr1, expr2) -> 
                    sprintf' "RangeGenerator (%s, %s)" <| str expr1 <| str expr2
                | RepeatGenerator (expr1, expr2) -> 
                    sprintf' "RepeatGenerator (%s, %s)" <| str expr1 <| str expr2
                | ZipWith(l,r,f) ->
                    sprintf' "ZipWith (%s, %s, %s)" <| str l <| str r <| str f

            toString self 
            
        static member AddOrderBy(keySelector : LambdaExpression, order : Order, queryExpr : QueryExpr) = 
            match queryExpr with
            | OrderBy (keySelectorOrderPairs, queryExpr') ->
                OrderBy ((keySelector, order) :: keySelectorOrderPairs, queryExpr')
            | _ -> OrderBy ([keySelector, order], queryExpr)
            
        