namespace Nessos.LinqOptimizer.Tests
  
module PrecompileHelpers =

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open Nessos.LinqOptimizer.Base
    open Nessos.LinqOptimizer.Core
    open Nessos.LinqOptimizer.FSharp
    open Microsoft.FSharp.Linq
    open Microsoft.FSharp.Linq.RuntimeHelpers
    open Microsoft.FSharp.Quotations

    let private queryType = typeof<Query>
    let private ofSeq = queryType.GetMethod("ofSeq")
    let private length = queryType.GetMethod("length")
    let private iter = queryType.GetMethod("iter")

    let private pQueryType = typeof<PQuery>
    let private pOfSeq = pQueryType.GetMethod("ofSeq")
    let private pLength = pQueryType.GetMethod("length")

    let ``fun x -> Query.length(Query.ofSeq x))`` =
        let x = Expression.Parameter(typeof<seq<int>>, "x")
        let ofSeq = Expression.Call(ofSeq.MakeGenericMethod [| typeof<int> |], x)
        let length = Expression.Call(length.MakeGenericMethod [| typeof<int> |], ofSeq) 
        Expression.Lambda<Func<seq<int>, Nessos.LinqOptimizer.Base.IQueryExpr<int>>>(length, x) 


    let ``fun x -> PQuery.length (PQuery.ofSeq x)`` = 
        let x = Expression.Parameter(typeof<seq<int>>, "x")
        let ofSeq = Expression.Call(pOfSeq.MakeGenericMethod [| typeof<int> |], x)
        let length = Expression.Call(pLength.MakeGenericMethod [| typeof<int> |], ofSeq) 
        Expression.Lambda<Func<seq<int>, Nessos.LinqOptimizer.Base.IParallelQueryExpr<int>>>(length, x) 

//    let ``fun x -> Query.iter (fun m -> a.Add(m)) (Query.ofSeq x)`` =
//        let a = Var("a", typeof<ResizeArray<int>>)
//        let m = Var("m", typeof<int>)             
//        let x = Var("x", typeof<seq<int>>)    
//        let add = typeof<ResizeArray<int>>.GetMethod("Add") //.MakeGenericMethod [| typeof<int> |]
//        let ofSeq = Expr.Call(ofSeq.MakeGenericMethod [| typeof<int> |], [Expr.Var x])
//        let action = 
//            let action = Expr.Lambda(m, Expr.Call(Expr.Var a, add, [Expr.Var m]))
//            <@ LeafExpressionConverter.QuotationToLambdaExpression %%action @>
//
//        let iter = iter.MakeGenericMethod [| typeof<int> |]
//        let iter1 = Expr.Call(iter, [action])
//        
//        ()