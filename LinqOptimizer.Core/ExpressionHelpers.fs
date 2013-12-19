    
namespace LinqOptimizer.Core
    open System
    open System.Linq.Expressions
    open System.Reflection

    [<AutoOpen>]
    module internal ExpressionHelpers =
        // F# friendly Expression functions 
        let empty = Expression.Empty()
        let ``default`` t = Expression.Default(t)
        let block (varExprs : seq<ParameterExpression>) (exprs : seq<Expression>) = 
            Expression.Block(varExprs, exprs) :> Expression
        let tryfinally bodyExpr finallyExpr = 
            Expression.TryFinally(bodyExpr, finallyExpr)
        let assign leftExpr rightExpr = 
            Expression.Assign(leftExpr, rightExpr) :> Expression
        let addAssign leftExpr rightExpr = 
            Expression.AddAssign (leftExpr, rightExpr) :> Expression
        let subAssign leftExpr rightExpr =
            Expression.SubtractAssign(leftExpr, rightExpr) :> Expression

        let cast expr ty =
            Expression.Convert(expr, ty) :> Expression

        let var name (t : Type) = Expression.Parameter(t, name)
        let lambda paramExprs bodyExpr = 
            Expression.Lambda(bodyExpr, paramExprs) 

        let labelTarget (name : string) = Expression.Label(name) 
        let label (labelTarget : LabelTarget) = Expression.Label(labelTarget) 
        let goto (labelTarget : LabelTarget) = Expression.Goto(labelTarget)

        let constant (value : obj) : Expression = Expression.Constant(value) :> _
         
        let ``new`` (t : Type) = Expression.New(t)
        let call (methodInfo : MethodInfo) (instance : Expression) 
                    (args : seq<Expression>) =
            Expression.Call(instance, methodInfo, args) :> Expression
             
        let ifThenElse boolExpr thenExpr elseExpr = 
            Expression.IfThenElse(boolExpr, thenExpr, elseExpr) :> Expression
        let ifThen boolExpr thenExpr = 
            Expression.IfThen(boolExpr, thenExpr) :> Expression
        let loop bodyExpr breakLabel continueLabel = 
            Expression.Loop(bodyExpr, breakLabel, continueLabel)
        let ``break`` (label : LabelTarget) = 
            Expression.Break(label)
        let ``continue`` (label : LabelTarget) = 
            Expression.Continue(label)
        

        let equal leftExpr rightExpr = Expression.Equal(leftExpr, rightExpr)
        let greaterThan leftExpr rightExpr = Expression.GreaterThan(leftExpr, rightExpr)
        let greaterThanOrEqual leftExpr rightExpr = Expression.GreaterThanOrEqual(leftExpr, rightExpr)
        let lessThan leftExpr rightExpr = Expression.LessThan(leftExpr, rightExpr)
        let lessThanOrEqual leftExpr rightExpr = Expression.LessThanOrEqual(leftExpr, rightExpr)
        let notEqual leftExpr rightExpr = Expression.NotEqual(leftExpr, rightExpr)

        let notExpr expr = Expression.Not(expr)

        let arrayNew (t : Type) (lengthExpr : Expression) =
            Expression.NewArrayBounds(t, [|lengthExpr|]) 
        let arrayAccess (arrayExpr : Expression) (indexExpr  : Expression) = 
            Expression.ArrayAccess(arrayExpr, indexExpr)
        let arrayIndex (arrayExpr : Expression) (indexExpr : Expression) = 
            Expression.ArrayIndex(arrayExpr, indexExpr)
        let arrayLength arrayExpr = Expression.ArrayLength(arrayExpr)

        let isPrimitive (expr : ConstantExpression) =
            expr.Type.IsPrimitive || expr.Type = typeof<string> 

        let (|QuotedLambda|_|) (expr : Expression) =
            match expr with
            | :? UnaryExpression as unaryExpr when unaryExpr.NodeType = ExpressionType.Quote ->
                match unaryExpr.Operand with
                | :? LambdaExpression as lam -> Some lam
                | _ -> None
            | _ -> None

        // Expression Active Patterns
        let (|Lambda|_|) (expr : Expression) = 
            match expr with
            | :? LambdaExpression as lam -> Some (Seq.toList lam.Parameters, lam.Body)
            | _ -> None

        // Expression Active Patterns
        let (|LambdaOrQuote|_|) (expr : Expression) = 
            match expr with
            | Lambda(param, body) -> 
                Some (Seq.toList param, body, expr :?> LambdaExpression)
            | QuotedLambda lam ->
                Some (Seq.toList lam.Parameters, lam.Body, lam)
            | _ ->
                None

           
        let (|MethodCall|_|) (expr : Expression) =
            if expr <> null && (expr.NodeType = ExpressionType.Call && (expr :? MethodCallExpression)) 
                then 
                    let methodCallExpr = expr :?> MethodCallExpression
                    Some (methodCallExpr.Object, methodCallExpr.Method, Seq.toList methodCallExpr.Arguments)
                else None

        let (|NotNull|_|) (expr : Expression) =
            if expr <> null then Some expr else None

        let (|ExprType|) (expr : Expression) = ExprType expr.Type

        type internal Expression with
            static member ofFSharpFunc<'T,'R>(func : Expression<Func<'T,'R>>) = func
