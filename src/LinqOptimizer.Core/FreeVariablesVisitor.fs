namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    /// Returns the free variables out of an expression.
    type private FreeVariablesVisitor () =
        inherit ExpressionVisitor() with

            let freeVars  = HashSet<ParameterExpression>()
            let localVars = HashSet<ParameterExpression>()

            let addLocals(vars : seq<ParameterExpression>) =
                vars |> Seq.iter (fun p -> localVars.Add(p) |> ignore)

            member this.Environment with get () = freeVars :> seq<_>

            override this.VisitParameter(expr : ParameterExpression) =
                if not <| localVars.Contains(expr) then
                    freeVars.Add(expr) |> ignore
                expr :> _
                
            override this.VisitLambda(expr : Expression<'T>) =
                addLocals expr.Parameters
                expr.Update(this.Visit(expr.Body), expr.Parameters) :> _

            override this.VisitBlock(expr : BlockExpression) =
                addLocals expr.Variables 
                expr.Update(expr.Variables, this.Visit(expr.Expressions)) :> _

            override this.VisitMember(expr : MemberExpression) =
                // TransparentIdentifier's free variable
                if expr.Member.MemberType = MemberTypes.Property then
                    let pi = expr.Member :?> PropertyInfo
                    if isTransparentIdentifier expr.Expression then
                        let p = Expression.Parameter(expr.Type, pi.Name)
                        freeVars.Add(p) |> ignore
                        p :> _
                    else
                        expr :> _
                else
                    expr :> _

//            override this.VisitMember(expr : MemberExpression) =
//                if expr.Expression :? ConstantExpression then
//                    let obj = (expr.Expression :?> ConstantExpression).Value
//                    
//                    let (value, p) = 
//                        match expr.Member.MemberType with
//                        | MemberTypes.Field ->
//                            let fi = expr.Member :?> FieldInfo
//                            fi.GetValue(obj), Expression.Parameter(expr.Type, sprintf "%s" fi.Name) 
//                        | MemberTypes.Property ->
//                            let pi = expr.Member :?> PropertyInfo
//                            let indexed = pi.GetIndexParameters() |> Seq.cast<obj> |> Seq.toArray
//                            pi.GetValue(obj, indexed), Expression.Parameter(expr.Type, sprintf "%s" pi.Name) 
//                        | _ -> 
//                            failwithf "Internal error : Accessing non Field or Property from MemberExpression %A" expr
//                    freeVars.Add(p) |> ignore
//                    p :> _
//                else
//                    expr.Update(this.Visit expr.Expression) :> _


    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module FreeVariablesVisitor =
        let get(expr : Expression) =
            let fvv = new FreeVariablesVisitor()
            let expr = fvv.Visit(expr)
            fvv.Environment

        let getWithExpr(expr : Expression) =
            let fvv = new FreeVariablesVisitor()
            let expr = fvv.Visit(expr)
            expr, fvv.Environment