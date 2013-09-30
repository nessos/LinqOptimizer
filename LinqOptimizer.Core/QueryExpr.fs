namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection

    // Typed Wrapper for QueryExpr 
    type QueryExpr<'T>(queryExpr : QueryExpr) =
        member self.QueryExpr = queryExpr 
    and ParallelQueryExpr<'T>(queryExpr : QueryExpr) =
        member self.QueryExpr = queryExpr 
    // C# friendly QueryExpr<unit>
    and QueryExprVoid(queryExpr : QueryExpr) =
        member self.QueryExpr = queryExpr 
    and ParallelQueryExprVoid(queryExpr : QueryExpr) =
        member self.QueryExpr = queryExpr 
    // Main Query representation
    and Order = Ascending | Descending
    and QueryExpr = 
        | Source of Expression * Type
        | Transform of LambdaExpression * QueryExpr * Type
        | TransformIndexed of LambdaExpression * QueryExpr * Type
        | Filter of LambdaExpression * QueryExpr * Type
        | FilterIndexed of LambdaExpression * QueryExpr * Type
        | NestedQuery of (ParameterExpression * QueryExpr) * QueryExpr * Type
        | NestedQueryTransform of (ParameterExpression * QueryExpr) * LambdaExpression * QueryExpr * Type
        | Aggregate of (obj *  Type) * LambdaExpression * QueryExpr
        | Sum of QueryExpr * Type 
        | Count of QueryExpr * Type
        | Take of Expression * QueryExpr * Type
        | Skip of Expression * QueryExpr * Type
        | ForEach of LambdaExpression * QueryExpr 
        | GroupBy of LambdaExpression * QueryExpr * Type
        | OrderBy of LambdaExpression * Order * QueryExpr * Type
        with

        member self.Type = 
            match self with
            | Source (_, t) -> t
            | Transform (_, _, t) -> t
            | TransformIndexed (_, _, t) -> t
            | Filter (_, _, t) -> t
            | FilterIndexed (_, _, t) -> t
            | NestedQuery (_, _, t) -> t 
            | NestedQueryTransform (_, _, _, t) -> t
            | Aggregate ((_, t), _, _) -> t
            | Sum (_, t) -> t
            | Count (_, t) -> t
            | Take (_, _, t) -> t
            | Skip (_, _, t) -> t
            | ForEach (_, _) -> typeof<Void>
            | GroupBy (_, _, t) -> t
            | OrderBy (_, _, _, t) -> t
           
     




            


        
        