namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    module internal ExpressionTransformer =

        type private ExpressionTransformer (transformer : Expression -> Expression option) =

            inherit ExpressionVisitor() with
                override this.VisitBinary(expr : BinaryExpression) =
                    let l = this.Visit expr.Left
                    let r = this.Visit expr.Right
                    let e = Expression.MakeBinary(expr.NodeType, l,r)
                    defaultArg (transformer e) (e :> _)
            
                override this.VisitBlock(expr : BlockExpression) =
                    let exprs' = expr.Expressions |> Seq.map this.Visit
                    let e = Expression.Block(exprs')
                    defaultArg (transformer e) (e :> _)

                override this.VisitCatchBlock(expr : CatchBlock) =
                    let var    = this.Visit expr.Variable :?> ParameterExpression
                    let body   = this.Visit expr.Body
                    let filter = this.Visit expr.Filter
                    let e      = Expression.MakeCatchBlock(expr.Test, var, body, filter)
                    e

                override this.VisitConditional(expr : ConditionalExpression) =
                    let ifTrue = this.Visit expr.IfTrue
                    let ifFalse = this.Visit expr.IfFalse
                    let test = this.Visit expr.Test
                    let e = Expression.Condition(test, ifTrue, ifFalse, expr.Type)
                    defaultArg (transformer e) (e :> _)

                override this.VisitConstant(expr : ConstantExpression) =
                    let e = expr
                    defaultArg (transformer e) (e :> _)
        
        let transform (transformer : Expression -> Expression option) =
            let t = new ExpressionTransformer(transformer)
            fun (expr : Expression) -> t.Visit expr
