namespace LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Collections.Concurrent

    module internal ExpressionTransformer =

        type private ExpressionTransformer (transformer : Expression -> Expression option) as self =

            inherit ExpressionVisitor() with
                override this.VisitBinary(expr : BinaryExpression) =
                    let l = this.Visit expr.Left
                    let r = this.Visit expr.Right
                    let e = Expression.MakeBinary(expr.NodeType, l,r)
                    defaultArg (transformer e) (e :> _)
            
                override this.VisitBlock(expr : BlockExpression) =
                    let exprs' =  this.Visit expr.Expressions 
                    let e = Expression.Block(exprs')
                    defaultArg (transformer e) (e :> _)

                // transform
                override this.VisitCatchBlock(expr : CatchBlock) =
                    let var    = this.Visit expr.Variable :?> ParameterExpression
                    let body   = this.Visit expr.Body
                    let filter = this.Visit expr.Filter
                    Expression.MakeCatchBlock(expr.Test, var, body, filter)

                override this.VisitConditional(expr : ConditionalExpression) =
                    let ifTrue = this.Visit expr.IfTrue
                    let ifFalse = this.Visit expr.IfFalse
                    let test = this.Visit expr.Test
                    let e = Expression.Condition(test, ifTrue, ifFalse, expr.Type)
                    defaultArg (transformer e) (e :> _)

                override this.VisitConstant(expr : ConstantExpression) =
                    defaultArg (transformer expr) (expr :> _)

                override this.VisitDebugInfo(expr : DebugInfoExpression) =
                    defaultArg (transformer expr) (expr :> _)

                override this.VisitDefault(expr : DefaultExpression) =
                    defaultArg (transformer expr) (expr :> _)
                    
                override this.VisitDynamic(expr : DynamicExpression) =
                    failwith "Not implemented"

                override this.VisitElementInit(expr : ElementInit) =
                    expr

                override this.VisitExtension(expr : Expression) =
                    defaultArg (transformer expr) expr

                override this.VisitGoto(expr : GotoExpression) =
                    let value = this.Visit expr.Value
                    let e = Expression.MakeGoto(expr.Kind, expr.Target, value, expr.Type)
                    defaultArg (transformer e) (e :> _)

                override this.VisitIndex(expr : IndexExpression) =
                    let o       = this.Visit expr.Object
                    let args    = this.Visit expr.Arguments
                    let e       = expr.Update(o, args) 
                    defaultArg (transformer e) (e :> _)

                override this.VisitInvocation(expr : InvocationExpression) =
                    let expr' = this.Visit expr.Expression
                    let args  = this.Visit expr.Arguments
                    let e = expr.Update(expr', args)
                    defaultArg (transformer e) (e :> _)

                override this.VisitLabel(expr : LabelExpression) =
                    let value = this.Visit(expr.DefaultValue)
                    let e = expr.Update(expr.Target, value)
                    defaultArg (transformer e) (e :> _)

                override this.VisitLabelTarget(expr : LabelTarget) =
                    expr

                override this.VisitLambda<'T>(expr : Expression<'T>) =
                    let body = this.Visit expr.Body
                    let par =  expr.Parameters |> Seq.map this.VisitParameter |> Seq.cast<ParameterExpression>
                    let e = expr.Update(body, par) :> Expression
                    defaultArg (transformer e) e

                // inits
                override this.VisitListInit(expr : ListInitExpression) =
                    let newExpr = this.Visit expr.NewExpression :?> NewExpression
                    let inits = expr.Initializers
                    let e = expr.Update(newExpr, inits)
                    defaultArg (transformer e) (e :> _)

                override this.VisitLoop(expr : LoopExpression) =
                    let body = this.Visit expr.Body
                    let e = expr.Update(expr.BreakLabel, expr.ContinueLabel, body)
                    defaultArg (transformer e) (e :> _)

                override this.VisitMember(expr : MemberExpression) =
                    let expr' = this.Visit expr.Expression
                    let e = expr.Update(expr')
                    defaultArg (transformer e) (e :> _)

                // transform
                override this.VisitMemberAssignment(expr : MemberAssignment) =
                    let expr' = this.Visit expr.Expression
                    let e = expr.Update(expr') 
                    e

                override this.VisitMemberBinding(expr : MemberBinding) =
                    expr

                override this.VisitMemberInit(expr : MemberInitExpression) =
                    let newExpr = this.Visit expr.NewExpression :?> NewExpression
                    let e = expr.Update(newExpr, expr.Bindings)
                    defaultArg (transformer e) (e :> _)

                override this.VisitMemberListBinding(expr : MemberListBinding) =
                    expr

                override this.VisitMemberMemberBinding(expr : MemberMemberBinding) =
                    expr

                override this.VisitMethodCall(expr : MethodCallExpression) =
                    let o = this.Visit expr.Object
                    let args = this.Visit expr.Arguments
                    let e = expr.Update(o, args)
                    defaultArg (transformer e) (e :> _)

                override this.VisitNew(expr : NewExpression) =
                    let args = this.Visit expr.Arguments
                    let e = expr.Update(args)
                    defaultArg (transformer e) (e :> _)

                override this.VisitNewArray(expr : NewArrayExpression) =
                    let exprs = this.Visit expr.Expressions
                    let e = expr.Update(exprs)
                    defaultArg (transformer e) (e :> _)

                override this.VisitParameter(expr : ParameterExpression) =
                    defaultArg (transformer expr) (expr :> _)

                override this.VisitRuntimeVariables(expr : RuntimeVariablesExpression) =
                    let vars = expr.Variables |> Seq.map this.VisitParameter |> Seq.cast<ParameterExpression>
                    let e = expr.Update(vars)
                    defaultArg (transformer e) (e :> _)

                // cases
                override this.VisitSwitch(expr : SwitchExpression) =
                    let value = this.Visit expr.SwitchValue
                    let cases = expr.Cases
                    let defaultBody = this.Visit expr.DefaultBody
                    let e = expr.Update(value, cases, defaultBody)
                    defaultArg (transformer e) (e :> _)

                override this.VisitSwitchCase(expr : SwitchCase) =
                    let tests = this.Visit expr.TestValues
                    let body = this.Visit expr.Body
                    expr.Update(tests, body)

                // handlers
                override this.VisitTry(expr : TryExpression) =
                    let body = this.Visit expr.Body
                    let handlers = expr.Handlers            
                    let finallyExpr = this.Visit expr.Finally
                    let fault = this.Visit expr.Fault
                    let e = expr.Update(body, handlers, finallyExpr, fault)
                    defaultArg (transformer e) (e :> _)
        
                override this.VisitTypeBinary(expr : TypeBinaryExpression) =
                    let expr' = this.Visit expr.Expression
                    let e = expr.Update(expr')
                    defaultArg (transformer e) (e :> _)

                override this.VisitUnary(expr : UnaryExpression) =
                    let expr' = this.Visit expr.Operand
                    let e = expr.Update(expr')
                    defaultArg (transformer e) (e :> _)


        let transform (transformer : Expression -> Expression option) =
            let t = new ExpressionTransformer(transformer)
            fun (expr : Expression) -> t.Visit expr
