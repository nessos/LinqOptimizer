namespace Nessos.LinqOptimizer.Core
    
    open System
    open System.Collections
    open System.Collections.Generic
    open System.Linq
    open System.Linq.Expressions
    open System.Reflection
    open System.Reflection.Emit

    type internal Session =
        
        static member private GetNewMethodName ()   = "LinqOptMethod"
        static member private GetNewTypeName ()     = "LinqOptTy_" + Guid.NewGuid().ToString("N")
        static member private GetNewAssemblyName () = "LinqOptAsm_" + Guid.NewGuid().ToString("N")
        static member private ModuleName            = "Module"

        static member private ModuleBuilder 
            with get () =
                lazy (  let asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(AssemblyName(Session.GetNewAssemblyName()), AssemblyBuilderAccess.Run)
                        let moduleBuilder = asmBuilder.DefineDynamicModule(Session.ModuleName)
                        moduleBuilder )

        static member private CompileToMethod(expr : LambdaExpression) =
            let methodName = Session.GetNewMethodName()
            let typeBuilder = Session.ModuleBuilder.Value.DefineType(Session.GetNewTypeName(), TypeAttributes.Public)
            let methodBuilder = typeBuilder.DefineMethod(methodName, MethodAttributes.Public ||| MethodAttributes.Static)
            expr.CompileToMethod(methodBuilder)
            let ty = typeBuilder.CreateType()
            let methodInfo = ty.GetMethod(methodName)
            methodInfo

        static member Compile (expr : LambdaExpression) = 
            let mi = Session.CompileToMethod(expr)
            mi

//        static member Compile (expr : Expression<Action>) : Action =
//            let mi = Session.CompileToMethod(expr)
//            Action(fun () -> mi.Invoke(null, Array.empty) :?> unit)