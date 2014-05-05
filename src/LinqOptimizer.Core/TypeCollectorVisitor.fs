namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    type private TypeCollectorVisitor () =
        inherit ExpressionVisitor() with

            let types = List<Type>()

            member this.Types with get () = types

            override this.VisitNew(expr : NewExpression) =
                types.Add(expr.Type)
                expr.Update(this.Visit expr.Arguments) :> _

            override this.VisitParameter(expr : ParameterExpression) =
                types.Add(expr.Type)
                expr :> _
                
            override this.VisitMember(expr : MemberExpression) =
                match expr.Member with
                | :? PropertyInfo as pi -> 
                    if pi.GetGetMethod() = null then types.Add(expr.Member.DeclaringType)
                | :? FieldInfo as fi ->
                    types.Add(expr.Member.DeclaringType)
                | _ -> ()

                expr.Update(this.Visit expr.Expression) :> _

            override this.VisitMethodCall(expr : MethodCallExpression) =
                types.Add(expr.Method.DeclaringType)
                expr.Update(this.Visit expr.Object, this.Visit expr.Arguments) :> _

            // cast
            override this.VisitUnary(expr : UnaryExpression) =
                types.Add(expr.Type)
                expr.Update(this.Visit expr.Operand) :> _

    module TypeCollector =
        let getTypes(exprs : seq<Expression>) : seq<Type>  =
            exprs 
            |> Seq.collect (fun expr -> 
                let tg = new TypeCollectorVisitor()
                tg.Visit(expr) |> ignore
                tg.Types)
