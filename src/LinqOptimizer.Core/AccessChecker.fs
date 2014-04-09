namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    type private AccessCheckerVisitor () =
        inherit ExpressionVisitor() with

            let rec isPublic (ty : Type) =
                if ty = null || ty = typeof<obj> then true
                else ty.IsPublic && isPublic ty.DeclaringType

            let invalidAccesses = List<Expression * string option>()

            member this.InvalidAccesses with get () = invalidAccesses

            override this.VisitNew(expr : NewExpression) =
                if not(isPublic expr.Type) then
                    invalidAccesses.Add(expr :> _, Some expr.Type.FullName)
                expr.Update(this.Visit expr.Arguments) :> _
                
            override this.VisitMember(expr : MemberExpression) =
                match expr.Member with
                | :? PropertyInfo as pi -> 
                    if pi.GetGetMethod() = null then invalidAccesses.Add(expr :> _, Some expr.Member.DeclaringType.FullName)
                | :? FieldInfo as fi ->
                    if not fi.IsPublic || not (isPublic fi.DeclaringType) then invalidAccesses.Add(expr :> _, Some expr.Member.DeclaringType.FullName)
                | _ -> ()

                expr.Update(this.Visit expr.Expression) :> _

            override this.VisitMethodCall(expr : MethodCallExpression) =
                if not expr.Method.IsPublic || not (isPublic expr.Method.DeclaringType) then
                    invalidAccesses.Add(expr :> _, Some expr.Method.DeclaringType.FullName)
                expr.Update(this.Visit expr.Object, this.Visit expr.Arguments) :> _

            // cast
            override this.VisitUnary(expr : UnaryExpression) =
                if expr.NodeType = ExpressionType.Convert && not (isPublic expr.Type) then invalidAccesses.Add(expr :> _, Some(expr.Type.FullName.ToString()))
                expr.Update(this.Visit expr.Operand) :> _

    module AccessChecker =
        let check(expr : Expression) : seq<_> option =
            let ac = new AccessCheckerVisitor()
            ac.Visit(expr) |> ignore
            if ac.InvalidAccesses.Count > 0 then Some(ac.InvalidAccesses :> _) else None
