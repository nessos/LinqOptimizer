namespace LinqOptimizer.Gpu
    open System
    open LinqOptimizer.Core


    module internal Compiler =
        
        
        let compile (queryExpr : QueryExpr) : string = 
            let rec compile' (queryExpr : QueryExpr) (context : QueryContext) =
                match queryExpr with
                | Source (ExprType (Array (_, 1)) as expr, t, _) ->
                    ""
                | _ -> failwithf "Not supported %A" queryExpr 

            match queryExpr with
            | Transform (_) ->
                let context : QueryContext = raise <| new NotImplementedException()
                compile' queryExpr context
            | Filter (_) ->
                let context : QueryContext = raise <| new NotImplementedException()
                compile' queryExpr context
            | Sum (_) ->
                let context : QueryContext = raise <| new NotImplementedException()
                compile' queryExpr context
            | Count (_) ->
                let context : QueryContext = raise <| new NotImplementedException()
                compile' queryExpr context
            | _ -> failwithf "Not supported %A" queryExpr 

