namespace Nessos.LinqOptimizer.Core
    open System
    open System.Linq.Expressions
    open System.Reflection

    [<AutoOpen>]
    module ReflectionHelpers =

        let (|MethodName|_|) (methodName : string) (methodInfo : MethodInfo) = 
            if methodInfo.Name = methodName then
                Some <| methodInfo.GetParameters()
            else None


        let (|ParameterName|_|) (parameterName : string) (parameterInfo : ParameterInfo) = 
            if parameterInfo.Name = parameterName then
                Some parameterInfo
            else None

        let (|TypeCheck|_|) (paramTypeDef : Type) (typeDef : Type) = 
            if paramTypeDef = typeDef then
                Some typeDef
            else None

        let (|ParamType|) (paramInfo : ParameterInfo) = paramInfo.ParameterType

        let (|Named|Array|Ptr|Param|) (t : System.Type) =
            if t.IsGenericType
            then Named(t.GetGenericTypeDefinition(), t.GetGenericArguments())
            elif t.IsGenericParameter
            then Param(t.GenericParameterPosition)
            elif not t.HasElementType
            then Named(t, [| |])
            elif t.IsArray
            then Array(t.GetElementType(), t.GetArrayRank())
            elif t.IsByRef
            then Ptr(true, t.GetElementType())
            elif t.IsPointer
            then Ptr(false, t.GetElementType())
            else failwith "MSDN says this can’t happen"

        let getIEnumerableType (ty : Type) =
            ty.GetInterface("IEnumerable`1").GetGenericArguments().[0]
