namespace Nessos.LinqOptimizer.FSharp

    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection

    open Microsoft.FSharp.Linq
    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Quotations.Patterns
    open Microsoft.FSharp.Quotations.DerivedPatterns
       
    open Nessos.LinqOptimizer.Base

    type private QueryBuilder () =
        
        member __.Quote (expr : Expr<'T>) = expr
        member __.Source(source:IEnumerable<'T>) : IQueryExpr<IEnumerable<'T>> = raise <| NotImplementedException()

        [<CustomOperation("select",AllowIntoPattern=true)>] 
        member __.Select(source : QueryExpr<IEnumerable<'T>>, [<ProjectionParameter>] projection:('T -> 'R)) : QueryExpr<IEnumerable<'R>> = raise <| NotImplementedException()

        member __.For(source:QueryExpr<IEnumerable<'T>>, body:('T -> QueryExpr<'R>)) : QueryExpr<'R> = raise <| NotImplementedException()
        
        [<CustomOperation("where")>]
        member __.Where(source:QueryExpr<'T>,[<ProjectionParameter>] selector: 'T -> bool) : QueryExpr<'T> = raise <| NotImplementedException()

        member __.Yield (x : 'T) : QueryExpr<IEnumerable<'T>> = raise <| NotImplementedException()

        member __.Zero () : QueryExpr<_> = raise <| NotImplementedException()

        member __.Run (expr : Expr<'T>) = expr

    module private QueryBuilderCompiler =
        
        let toExpression (expr : Expr) : Expression =
            Linq.RuntimeHelpers.LeafExpressionConverter.QuotationToExpression(expr)

        let evalExpr<'T> (expr : Expr) =
            Linq.RuntimeHelpers.LeafExpressionConverter.EvaluateQuotation(expr) :?> 'T

//        let rec transform (expr : Expr<QueryExpr<IEnumerable<'T>>>) : Expr<QueryExpr<IEnumerable<'T>>> =
//            match expr with
//            | Call (o ,m, [seqExpr]) when m.Name = "Source" ->
//                let source = evalExpr<IEnumerable<'T>> seqExpr 
//                <@ ExtensionMethods.AsQueryExpr(source) @>
//                Expr.Call()
//            | Call (o ,m, [source; func]) when m.Name = "Select" ->
//                let source = source :?> Expr<QueryExpr<IEnumerable<'T>>>
//                let func = func :?> Expr<FSharpFunc<>>
//
//                let func   = toExpression func 
//                let source = source :?> Expr<QueryExpr>
//                let source = toExpression (transform source)
//                <@ Query.map func source @>
//            | _ -> 
//                expr
//
//    [<AutoOpen>]
//    module Extensions =
//        let query = new QueryBuilder ()
//
//        let x  = 
//            (query { for x in 1..10 do select x })
//        
//        QueryBuilderCompiler.transform x
//
//        match x with
//        | Call (_,m, [source; func]) when m.Name = "For" ->
//            func.Type :> obj
//        | Call (o ,m, [source; func]) when m.Name = "Select" -> 
//            (source.Type.ToString(), func.Type.ToString()) :> obj 
//
//        |> ignore
//        
//  Call (Some (Value (MBI_0024+QueryBuilder)), For,
//      [),
//       Lambda (_arg1,
//               Let (x, _arg1,
//                    Call (Some (Value (MBI_0024+QueryBuilder)), Yield, [x])))])
//
//   Lambda (x, x)
//
//"MBI_0010+QueryExpr`1[System.Collections.Generic.IEnumerable`1[System.Int32]]",
//"Microsoft.FSharp.Core.FSharpFunc`2[System.Int32,System.Int32]"
//
//Call (Some (Value (MBI_0024+QueryBuilder)), Source, [Call (None, op_Range, [Value (1), Value (10)])]
//
//
//let x = 
//    Linq.RuntimeHelpers.LeafExpressionConverter.EvaluateQuotation <@@ (fun x -> x + 1) @@> :?> (int -> int)
//
//
//    module F =
//        open System.Linq
//
//        let x = Enumerable.Repeat(1,10)
//        let ty = x.GetType()
//        ty.FullName = "System.Linq.Enumerable+<RangeIterator>d__b8"
//        ty.GetFields() |> Array.iter (fun m -> printfn "%A" m.Name)        
//        ty.GetField("<>3__start").GetValue(x)
//        ty.GetField("<>3__count").GetValue(x)
//
