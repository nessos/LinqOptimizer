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
    // Main Query representation
    and QueryExpr = 
        | Source of Expression * Type
        | Transform of LambdaExpression * QueryExpr * Type
        | Filter of LambdaExpression * QueryExpr * Type
        | NestedQuery of (ParameterExpression * QueryExpr) * QueryExpr * Type
        | NestedQueryTransform of (ParameterExpression * QueryExpr) * LambdaExpression * QueryExpr * Type
        | Aggregate of (obj *  Type) * LambdaExpression * QueryExpr
        | Sum of QueryExpr * Type with

        member self.Type = 
            match self with
            | Source (_, t) -> t
            | Transform (_, _, t) -> t
            | Filter (_, _, t) -> t
            | NestedQuery (_, _, t) -> t 
            | NestedQueryTransform (_, _, _, t) -> t
            | Aggregate ((_, t), _, _) -> t
            | Sum (_, t) -> t
           
     




            


        
        