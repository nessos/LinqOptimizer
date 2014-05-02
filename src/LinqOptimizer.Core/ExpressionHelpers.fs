    
namespace Nessos.LinqOptimizer.Core
    open System
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Runtime.CompilerServices

    [<AutoOpen>]
    module ExpressionHelpers =
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

        let (|Map|) (f : 'T -> 'S) (v : 'T) = f v

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

        let (|Constant|_|) (expr : Expression) = 
            match expr with
            | :? ConstantExpression as constExpr -> Some (constExpr.Value, constExpr.Type)
            | _ -> None

        let (|Parameter|_|) (expr : Expression) = 
            match expr with
            | :? ParameterExpression as paramExpr -> Some (paramExpr)
            | _ -> None

        let (|Assign|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as assignExpr 
                when assignExpr.NodeType = ExpressionType.Assign -> Some (assignExpr.Left, assignExpr.Right)
            | _ -> None

        let (|AddAssign|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as assignExpr 
                when assignExpr.NodeType = ExpressionType.AddAssign -> Some (assignExpr.Left, assignExpr.Right)
            | _ -> None

        let (|Plus|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as plusExpr 
                when plusExpr.NodeType = ExpressionType.Add -> Some (plusExpr.Left, plusExpr.Right)
            | _ -> None

        let (|Subtract|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as plusExpr 
                when plusExpr.NodeType = ExpressionType.Subtract -> Some (plusExpr.Left, plusExpr.Right)
            | _ -> None

        let (|Divide|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as plusExpr 
                when plusExpr.NodeType = ExpressionType.Divide -> Some (plusExpr.Left, plusExpr.Right)
            | _ -> None

        let (|Times|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as plusExpr 
                when plusExpr.NodeType = ExpressionType.Multiply -> Some (plusExpr.Left, plusExpr.Right)
            | _ -> None

        let (|Modulo|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as plusExpr 
                when plusExpr.NodeType = ExpressionType.Modulo -> Some (plusExpr.Left, plusExpr.Right)
            | _ -> None

        let (|Negate|_|) (expr : Expression) = 
            match expr with
            | :? UnaryExpression as unaryExpr
                when unaryExpr.NodeType = ExpressionType.Negate -> Some (unaryExpr.Operand)
            | _ -> None

        let (|IfThenElse|_|) (expr : Expression) = 
            match expr with
            | :? ConditionalExpression as contExpr -> 
                Some (contExpr.Test, contExpr.IfTrue, contExpr.IfFalse)
            | _ -> None


        let (|Equal|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as equalExpr 
                when equalExpr.NodeType = ExpressionType.Equal -> Some (equalExpr.Left, equalExpr.Right) 
            | _ -> None

        let (|NotEqual|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as equalExpr 
                when equalExpr.NodeType = ExpressionType.NotEqual -> Some (equalExpr.Left, equalExpr.Right) 
            | _ -> None

        let (|GreaterThan|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as equalExpr 
                when equalExpr.NodeType = ExpressionType.GreaterThan -> Some (equalExpr.Left, equalExpr.Right) 
            | _ -> None

        let (|GreaterThanOrEqual|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as equalExpr 
                when equalExpr.NodeType = ExpressionType.GreaterThanOrEqual -> Some (equalExpr.Left, equalExpr.Right) 
            | _ -> None

        let (|LessThan|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as equalExpr 
                when equalExpr.NodeType = ExpressionType.LessThan -> Some (equalExpr.Left, equalExpr.Right) 
            | _ -> None

        let (|LessThanOrEqual|_|) (expr : Expression) = 
            match expr with
            | :? BinaryExpression as equalExpr 
                when equalExpr.NodeType = ExpressionType.LessThanOrEqual -> Some (equalExpr.Left, equalExpr.Right) 
            | _ -> None

        let (|Nop|_|) (expr : Expression) = 
            match expr with
            | :? DefaultExpression as defaultExpr -> Some (defaultExpr) 
            | _ -> None

        let (|Goto|_|) (expr : Expression) = 
            match expr with
            | :? GotoExpression as gotoExpr -> Some (gotoExpr.Kind, gotoExpr.Target, gotoExpr.Value) 
            | _ -> None

        let (|Block|_|) (expr : Expression) = 
            match expr with
            | :? BlockExpression as blockExpr -> Some (blockExpr.Variables, blockExpr.Expressions, blockExpr.Result) 
            | _ -> None

        let (|Convert|_|) (expr : Expression) = 
            match expr with
            | :? UnaryExpression as unaryExpr when unaryExpr.NodeType = ExpressionType.Convert -> 
                Some (unaryExpr.Operand, unaryExpr.Type) 
            | _ -> None


        // http://stackoverflow.com/questions/1650681/determining-whether-a-type-is-an-anonymous-type
        // TODO : Mono?
        let isAnonymousType (ty : Type) =
            ty.GetCustomAttributes(typeof<CompilerGeneratedAttribute>, false).Count() > 0
            && ty.FullName.Contains "AnonymousType"
            && ty.Namespace = null
            && ty.IsSealed
            && not ty.IsPublic

        // TODO : Mono?
        let isTransparentIdentifier (expr : Expression) =
            match expr with
            | :? ParameterExpression as expr -> expr.Name.Contains "TransparentIdentifier"
            | _ -> false

        let isAnonymousConstructor (expr : Expression) =
            match expr with
            | :? NewExpression as expr -> isAnonymousType expr.Constructor.DeclaringType
            | _ -> false

        let (|AnonymousTypeAssign|_|) (expr : Expression) =
            if expr.NodeType = ExpressionType.Assign then
                let expr = expr :?> BinaryExpression
                if isTransparentIdentifier expr.Left && isAnonymousConstructor expr.Right then
                    let left = expr.Left :?> ParameterExpression
                    let right = expr.Right :?> NewExpression
                    Some(left, right)
                else None
            else None

        let (|AnonymousTypeConstruction|_|) (expr : Expression) =
            match expr with
            | :? NewExpression as expr when isAnonymousType expr.Constructor.DeclaringType -> Some (expr.Members, expr.Arguments)
            | _ -> None

        let (|TransparentIdentifierIdentityAssignment|_|) (expr : BinaryExpression) =
            if expr.NodeType = ExpressionType.Assign
                && isTransparentIdentifier expr.Left
                && isTransparentIdentifier expr.Right then
                    let left = expr.Left :?> ParameterExpression
                    let right = expr.Right :?> ParameterExpression
                    if left.Name = right.Name then Some (left, right)
                    else None
            else
                None

        let (|AnonymousTypeMember|_|) (expr : Expression) =
            match expr with
            | :? MemberExpression as expr ->
                    match expr.Member.MemberType with
                    | MemberTypes.Property when isAnonymousType expr.Member.DeclaringType -> Some expr
                    | _ -> None
            | _ -> None

        let (|FieldMember|_|) (expr : Expression) =
            match expr with
            | :? MemberExpression as expr ->
                    match expr.Member.MemberType with
                    | MemberTypes.Field -> Some (expr.Expression, expr.Member :?> FieldInfo)
                    | _ -> None
            | _ -> None

        let (|PropertyMember|_|) (expr : Expression) =
            match expr with
            | :? MemberExpression as expr ->
                    match expr.Member.MemberType with
                    | MemberTypes.Property -> Some (expr.Expression, expr.Member)
                    | _ -> None
            | _ -> None

        let (|ValueTypeMemberInit|_|) (expr : Expression) =
            match expr with
            | :? MemberInitExpression as expr when expr.Type.IsValueType -> Some (expr.NewExpression, expr.Bindings |> Seq.map (fun binding -> binding :?> MemberAssignment))
            | _ -> None

        let (|Loop|_|) (expr : Expression) =
            match expr with
            | :? LoopExpression as expr -> Some (expr.Body, expr.BreakLabel, expr.ContinueLabel)
            | _ -> None

        type internal Expression with
            static member ofFSharpFunc<'T,'R>(func : Expression<Func<'T,'R>>) = func
