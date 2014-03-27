namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent
    open System.Runtime.CompilerServices

    // Transform anonymous type access and transparent identifiers
    // to local variables.
    type private AnonymousTypeEraserVisitor () =
        inherit ExpressionVisitor() with



            let generated = sprintf "___%s___"

            let mappings = Dictionary<ParameterExpression, ParameterExpression>()
            let existing = List<ParameterExpression>()
            let redundants = List<ParameterExpression>()

            override this.VisitMember(expr : MemberExpression) =
                match expr.Member.MemberType with
                | MemberTypes.Property when isAnonymousType expr.Member.DeclaringType ->
                    let p = mappings.Values.SingleOrDefault(fun p -> p.Name = generated expr.Member.Name) //Expression.Parameter(expr.Type, expr.Member.Name)
                    if p = null then 
                        let e = existing.SingleOrDefault(fun p -> p.Name = expr.Member.Name) 
                        if e = null then expr.Update(this.Visit expr.Expression) :> _ else e  :> _
                    else 
                        p :> _
                | _ -> 
                    //expr :> _
                    expr.Update(this.Visit expr.Expression) :> _

            override this.VisitBinary(expr : BinaryExpression) =
                match expr with
                | AnonymousTypeAssign(left, right) ->
                    let first = right.Arguments.First() :?> ParameterExpression
                    if not <| isTransparentIdentifier first then existing.Add(first)

                    let right' = this.Visit(right.Arguments.Last())
                    let left' = Expression.Parameter(right'.Type, generated(right.Members.Last().Name))
                    mappings.Add(left, left')
                    Expression.Assign(left', right') :> _
                | TransparentIdentifierIdentityAssignment(ti1, ti2) ->
                    redundants.Add(ti1)
                    redundants.Add(ti2)
                    Expression.Empty() :> _
                | _ -> 
                   expr.Update(this.Visit(expr.Left), null, this.Visit(expr.Right)) :> _

            override this.VisitBlock(expr : BlockExpression) =
                let exprs = this.Visit(expr.Expressions)
                let vars  = expr.Variables 
                            |> Seq.map (fun p -> if mappings.ContainsKey(p) then mappings.[p] else p)
                            |> Seq.filter (fun p -> not(Seq.exists ((=) p) redundants))
                Expression.Block(vars, exprs ) :> _

    module AnonymousTypeEraser =
        let apply(expr : Expression) =
            let ate = new AnonymousTypeEraserVisitor()
            ate.Visit(expr)