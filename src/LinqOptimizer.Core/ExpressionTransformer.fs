namespace Nessos.LinqOptimizer.Core
    
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
                override this.Visit (expr : Expression) =
                    match transformer expr with
                    | None -> base.Visit(expr)
                    | Some expr -> expr

        let transform (transformer : Expression -> Expression option) =
            let t = new ExpressionTransformer(transformer)
            fun (expr : Expression) -> t.Visit expr


//            inherit ExpressionVisitor() with
//                member private this.Transformer = transformer
//
//                override this.VisitBinary(expr : BinaryExpression) =
//                    match transformer expr with
//                    | None ->
//                        let l = this.Visit expr.Left
//                        let r = this.Visit expr.Right
//                        Expression.MakeBinary(expr.NodeType, l, r) :> _
//                    | Some expr ->
//                        expr
//            
//                override this.VisitBlock(expr : BlockExpression) =
//                    let exprs' = this.Visit expr.Expressions 
//                    let e = Expression.Block(exprs')
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitCatchBlock(expr : CatchBlock) =
//                    ExpressionTransformer.VisitCatchBlockWrapper(this, expr)
//
//                override this.VisitConditional(expr : ConditionalExpression) =
//                    let ifTrue = this.Visit expr.IfTrue
//                    let ifFalse = this.Visit expr.IfFalse
//                    let test = this.Visit expr.Test
//                    let e = Expression.Condition(test, ifTrue, ifFalse, expr.Type)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitConstant(expr : ConstantExpression) =
//                    defaultArg (transformer expr) (expr :> _)
//
//                override this.VisitDebugInfo(expr : DebugInfoExpression) =
//                    defaultArg (transformer expr) (expr :> _)
//
//                override this.VisitDefault(expr : DefaultExpression) =
//                    defaultArg (transformer expr) (expr :> _)
//                    
//                override this.VisitDynamic(expr : DynamicExpression) =
//                    let args = this.Visit expr.Arguments
//                    let e    = expr.Update(args)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitElementInit(expr : ElementInit) =
//                    ExpressionTransformer.VisitElementInitWrapper(this, expr)
//
//                override this.VisitExtension(expr : Expression) =
//                    defaultArg (transformer expr) expr
//
//                override this.VisitGoto(expr : GotoExpression) =
//                    let value = this.Visit expr.Value
//                    let e = expr.Update(expr.Target, value)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitIndex(expr : IndexExpression) =
//                    let o       = this.Visit expr.Object
//                    let args    = this.Visit expr.Arguments
//                    let e       = expr.Update(o, args) 
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitInvocation(expr : InvocationExpression) =
//                    let expr'   = this.Visit expr.Expression
//                    let args    = this.Visit expr.Arguments
//                    let e       = expr.Update(expr', args)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitLabel(expr : LabelExpression) =
//                    let value = this.Visit(expr.DefaultValue)
//                    let e = expr.Update(expr.Target, value)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitLabelTarget(expr : LabelTarget) =
//                    expr
//
//                override this.VisitLambda<'T>(expr : Expression<'T>) =
//                    let body = this.Visit expr.Body
//                    let par =  expr.Parameters |> Seq.map (fun e -> ExpressionTransformer.VisitParameterWrapper(this,e)) |> Seq.cast<ParameterExpression>
//                    let e = expr.Update(body, par)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitListInit(expr : ListInitExpression) =
//                    let newExpr = this.Visit expr.NewExpression :?> NewExpression
//                    let inits = expr.Initializers |> Seq.map (fun ei -> ExpressionTransformer.VisitElementInitWrapper(this, ei))
//                    let e = expr.Update(newExpr, inits)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitLoop(expr : LoopExpression) =
//                    let body = this.Visit expr.Body
//                    let e = expr.Update(expr.BreakLabel, expr.ContinueLabel, body)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitMember(expr : MemberExpression) =
//                    let expr' = this.Visit expr.Expression
//                    let e = expr.Update(expr')
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitMemberAssignment(expr : MemberAssignment) =
//                    let expr' = this.Visit expr.Expression
//                    expr.Update(expr')
//
//                override this.VisitMemberBinding(expr : MemberBinding) =
//                    ExpressionTransformer.VisitMemberBindingWrapper(this, expr)
//
//                override this.VisitMemberInit(expr : MemberInitExpression) =
//                    let newExpr = this.Visit expr.NewExpression :?> NewExpression
//                    let bindings = expr.Bindings |> Seq.map (fun mb -> ExpressionTransformer.VisitMemberBindingWrapper(this, mb))
//                    let e = expr.Update(newExpr, bindings)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitMemberListBinding(expr : MemberListBinding) =
//                    let inits = expr.Initializers |> Seq.map (fun ei -> ExpressionTransformer.VisitElementInitWrapper(this,ei))
//                    expr.Update(inits)
//
//                override this.VisitMemberMemberBinding(expr : MemberMemberBinding) =
//                    let binds = expr.Bindings |> Seq.map (fun mb -> ExpressionTransformer.VisitMemberBindingWrapper(this, mb))
//                    expr.Update(binds)
//
//                override this.VisitMethodCall(expr : MethodCallExpression) =
//                    match transformer expr with
//                    | None ->
//                        let o = this.Visit expr.Object
//                        let args = this.Visit expr.Arguments
//                        expr.Update(o, args) :> _
//                    | Some expr ->
//                        this.Visit expr
//
//                override this.VisitNew(expr : NewExpression) =
//                    let args = this.Visit expr.Arguments
//                    let e = expr.Update(args)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitNewArray(expr : NewArrayExpression) =
//                    let exprs = this.Visit expr.Expressions
//                    let e = expr.Update(exprs)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitParameter(expr : ParameterExpression) =
//                    ExpressionTransformer.VisitParameterWrapper(this,expr)
//                    
//                override this.VisitRuntimeVariables(expr : RuntimeVariablesExpression) =
//                    let vars = expr.Variables |> Seq.map (fun e -> ExpressionTransformer.VisitParameterWrapper(this,e)) |> Seq.cast<ParameterExpression>
//                    let e = expr.Update(vars)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitSwitch(expr : SwitchExpression) =
//                    let value = this.Visit expr.SwitchValue
//                    let cases = expr.Cases |> Seq.map (fun sc -> ExpressionTransformer.VisitSwitchCaseWrapper(this, sc))
//                    let defaultBody = this.Visit expr.DefaultBody
//                    let e = expr.Update(value, cases, defaultBody)
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitSwitchCase(expr : SwitchCase) =
//                    ExpressionTransformer.VisitSwitchCaseWrapper(this, expr)
//
//                override this.VisitTry(expr : TryExpression) =
//                    let body = this.Visit expr.Body
//                    let handlers = expr.Handlers |> Seq.map (fun handler -> ExpressionTransformer.VisitCatchBlockWrapper(this, handler))          
//                    let finallyExpr = this.Visit expr.Finally
//                    let fault = this.Visit expr.Fault
//                    let e = expr.Update(body, handlers, finallyExpr, fault)
//                    defaultArg (transformer e) (e :> _)
//        
//                override this.VisitTypeBinary(expr : TypeBinaryExpression) =
//                    let expr' = this.Visit expr.Expression
//                    let e = expr.Update(expr')
//                    defaultArg (transformer e) (e :> _)
//
//                override this.VisitUnary(expr : UnaryExpression) =
//                    let expr' = this.Visit expr.Operand
//                    let e = expr.Update(expr')
//                    defaultArg (transformer e) (e :> _)
//
//
//                static member private VisitParameterWrapper(visitor : ExpressionTransformer, expr : ParameterExpression) =
//                    defaultArg (visitor.Transformer (expr :> _)) (expr :> _)
//
//                static member private VisitElementInitWrapper(visitor : ExpressionVisitor, expr : ElementInit) =
//                    let args = visitor.Visit(expr.Arguments)
//                    expr.Update(args)
//
//                static member private VisitMemberBindingWrapper(visitor : ExpressionVisitor, expr : MemberBinding) =
//                    expr
//
//                static member private VisitSwitchCaseWrapper(visitor : ExpressionVisitor, expr : SwitchCase) =
//                    let tests = visitor.Visit expr.TestValues
//                    let body  = visitor.Visit expr.Body
//                    expr.Update(tests, body)
//
//                static member private VisitCatchBlockWrapper(visitor : ExpressionVisitor, expr : CatchBlock) =
//                    let var    = visitor.Visit expr.Variable :?> ParameterExpression
//                    let body   = visitor.Visit expr.Body
//                    let filter = visitor.Visit expr.Filter
//                    expr.Update(var, filter, body)