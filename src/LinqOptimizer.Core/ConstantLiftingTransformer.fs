namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    // Lift constants and member access into parameters
    // due to the live-object limitation.
    type private ConstantLiftingVisitor () =
        inherit ExpressionVisitor() with
            let mutable x = 0
            let getName () = 
                x <- x + 1
                sprintf "___param%d___" x

            let x = Dictionary<ParameterExpression, obj>()

            member this.Environment with get () = x

            override this.VisitConstant(expr : ConstantExpression) =
                if not <| isPrimitive expr && expr.Value <> null then
                    let p = Expression.Parameter(expr.Type, getName()) 
                    this.Environment.Add(p, expr.Value)
                    p :> _
                else 
                    expr :> _

            override this.VisitMember(expr : MemberExpression) =
                if expr.Expression :? ConstantExpression then
                    
                    let obj = (expr.Expression :?> ConstantExpression).Value
                    
                    let (value, p) = 
                        match expr.Member.MemberType with
                        | MemberTypes.Field ->
                            let fi = expr.Member :?> FieldInfo
                            fi.GetValue(obj), Expression.Parameter(expr.Type, sprintf "___param%s___" fi.Name) 
                        | MemberTypes.Property ->
                            let pi = expr.Member :?> PropertyInfo
                            let indexed = pi.GetIndexParameters() |> Seq.cast<obj> |> Seq.toArray
                            pi.GetValue(obj, indexed), Expression.Parameter(expr.Type, sprintf "___param%s___" pi.Name) 
                        | _ -> 
                            failwithf "Internal error : Accessing non Field or Property from MemberExpression %A" expr
                                                
                    this.Environment.Add(p, value)
                    p :> _
                else
                    expr.Update(this.Visit expr.Expression) :> _

    module ConstantLiftingTransformer =
        let apply(expr : Expression) =
            let clv = new ConstantLiftingVisitor()
            let expr = clv.Visit(expr)
            expr, clv.Environment.Keys.ToArray(), clv.Environment.Values.ToArray()