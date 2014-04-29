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


    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module FreeVariablesVisitor =
        let get(expr : Expression) =
            let fvv = new FreeVariablesVisitor()
            let expr = fvv.Visit(expr)
            fvv.Environment