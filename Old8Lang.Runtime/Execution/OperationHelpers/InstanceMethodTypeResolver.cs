using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Runtime;
using Old8Lang.InstanceMethods.Core;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// 实例方法的共享类型解析：IL 后端发射方法调用与 Runtime 类型推断必须使用同一套规则。
/// </summary>
/// <remarks>
/// 该解析只依赖实例方法注册表与运行时值类型，不发射 IL，因此保留在 Runtime；
/// 具体 IL 发射位于 <c>Old8Lang.Compiler</c> 的 <c>DotOperatorILHelper</c>。
/// </remarks>
internal static class InstanceMethodTypeResolver
{
    /// <summary>
    /// 推断实例方法调用在 IL 模式下的返回类型。
    /// </summary>
    internal static Type? InferInstanceMethodReturnType(Type receiverType, Instance instance, LocalManager local)
    {
        var method = ResolveInstanceMethod(receiverType, instance, local, out var mappedType);
        if (method is null)
        {
            return InferIlBridgeReturnType(receiverType, instance, local);
        }

        var returnReceiverType = method is ExtensionMethodWrapper ? receiverType : mappedType;
        return method?.GetReturnType(returnReceiverType, instance.Ids, local);
    }

    /// <summary>
    /// 推断「原生集合接收者 + 解释器实现桥接」这条 IL 路径的返回类型。
    /// </summary>
    internal static Type? InferIlBridgeReturnType(Type receiverType, Instance instance, LocalManager local)
    {
        var method = ResolveIlBridgeMethod(receiverType, instance, local, out var mappedType);
        if (method is null)
        {
            return null;
        }

        try
        {
            return MapIlBridgeReturnType(method.GetReturnType(mappedType, instance.Ids, local));
        }
        catch
        {
            return typeof(object);
        }
    }

    /// <summary>
    /// 解析 IL 模式下由 <see cref="IlNativeCollectionBridge"/> 承接的实例方法。
    /// </summary>
    /// <remarks>
    /// 只在接收者是原生 <c>List&lt;T&gt;</c> / <c>T[]</c> / <c>Dictionary&lt;K,V&gt;</c>、
    /// 且等价 LangValueType 上存在对应方法时返回；已实现
    /// <see cref="IIlNativeValueInstanceMethod"/> 的方法走各自的专用 IL，不进入该路径。
    /// </remarks>
    internal static IInstanceMethod? ResolveIlBridgeMethod(Type receiverType, Instance instance,
        LocalManager local, out Type mappedType)
    {
        mappedType = typeof(LangValueType);
        if (!TryGetEquivalentLangValueType(receiverType, out var equivalentType))
        {
            return null;
        }

        if (equivalentType != typeof(ListLangValue) &&
            equivalentType != typeof(ArrayLangValue) &&
            equivalentType != typeof(DictionaryLangValue) &&
            equivalentType != typeof(TupleLangValue))
        {
            return null;
        }

        mappedType = equivalentType;
        InstanceMethodInitializer.EnsureInitialized();
        return InstanceMethodRegistry.Instance.ResolveMethod(equivalentType, instance.Id.IdName, instance.Ids, local);
    }

    /// <summary>
    /// 把实例方法声明的返回类型映射为 IL 栈上的实际类型。
    /// </summary>
    /// <remarks>
    /// <see cref="IlNativeCollectionBridge.Invoke"/> 返回的是已还原的原生值，
    /// 因此这里必须返回与运行期结果一致的 IL 类型，编译期据此决定是否
    /// <c>pop</c> / <c>unbox.any</c> / <c>castclass</c>。类型推断也复用同一映射，
    /// 避免「推断是 object、栈上是 List」产生无效 IL。
    /// </remarks>
    internal static Type MapIlBridgeReturnType(Type declared)
    {
        if (declared == typeof(void) || declared == typeof(VoidLangValue))
        {
            return typeof(void);
        }

        if (declared == typeof(ListLangValue) || declared == typeof(List<object?>))
        {
            return typeof(List<object?>);
        }

        if (declared == typeof(ArrayLangValue))
        {
            return typeof(object?[]);
        }

        if (declared == typeof(DictionaryLangValue) || declared == typeof(Dictionary<object, object>))
        {
            return typeof(Dictionary<object, object?>);
        }

        // 元组结果保留为 TupleLangValue（object），否则显示会变成数组的 "[1, 2]"。

        if (declared == typeof(int) || declared == typeof(bool) || declared == typeof(double) ||
            declared == typeof(char) || declared == typeof(string))
        {
            return declared;
        }

        if (declared.IsArray)
        {
            return declared;
        }

        return typeof(object);
    }

    /// <summary>
    /// 按接收者类型解析实例方法，并给出实际匹配的运行时值类型。
    /// </summary>
    internal static IInstanceMethod? ResolveInstanceMethod(Type receiverType, Instance instance,
        LocalManager local, out Type mappedType)
    {
        // 用户类自带的同名方法优先于内置实例方法（例如类里定义的 toString/equals）：
        // 返回 null 让调用方走 CLR 方法解析，避免被 registry 里的 ToString 别名截走。
        // 仅限 Old8Lang 自定义类（LangObject 子类）；string/native 集合等框架类型
        // 仍走内置实例方法，避免 Split/Trim 等被 CLR 同名方法截走。
        if (receiverType is not System.Reflection.Emit.TypeBuilder &&
            typeof(LangObject).IsAssignableFrom(receiverType))
        {
            var declared = receiverType
                .GetMethods(System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance)
                .Any(method => method.Name == instance.Id.IdName &&
                               method.DeclaringType != typeof(object) &&
                               method.DeclaringType != typeof(LangObject));
            if (declared)
            {
                mappedType = receiverType;
                return null;
            }
        }

        InstanceMethodInitializer.EnsureInitialized();
        var registry = InstanceMethodRegistry.Instance;
        mappedType = MapToLangValueType(receiverType);
        var method = registry.ResolveMethod(mappedType, instance.Id.IdName, instance.Ids, local);
        if (method is null && TryGetEquivalentLangValueType(receiverType, out var equivalentType))
        {
            var candidate = registry.ResolveMethod(equivalentType, instance.Id.IdName, instance.Ids, local);
            if (candidate is IIlNativeValueInstanceMethod)
            {
                method = candidate;
                mappedType = equivalentType;
            }
        }
        return method;
    }

    /// <summary>
    /// 将 C# 原生类型映射到对应的 LangValueType，用于在实例方法注册表中查找方法。
    /// </summary>
    internal static Type MapToLangValueType(Type nativeType)
    {
        if (nativeType == typeof(int) || nativeType == typeof(long))
        {
            return typeof(IntLangValue);
        }

        if (nativeType == typeof(double))
        {
            return typeof(DoubleLangValue);
        }

        if (nativeType == typeof(bool))
        {
            return typeof(BoolLangValue);
        }

        if (nativeType == typeof(char))
        {
            return typeof(CharLangValue);
        }

        if (nativeType == typeof(string))
        {
            return typeof(StringLangValue);
        }

        if (typeof(LangValueType).IsAssignableFrom(nativeType))
        {
            return nativeType;
        }

        return typeof(LangValueType);
    }

    /// <summary>
    /// 尝试把 IL 模式下的原生集合类型映射到等价的 LangValueType。
    /// </summary>
    /// <remarks>
    /// 只用于「按等价类型再解析一次实例方法」的回退路径，且调用方只会采纳
    /// 显式实现 <see cref="IIlNativeValueInstanceMethod"/> 的方法。
    /// </remarks>
    internal static bool TryGetEquivalentLangValueType(Type nativeType, out Type equivalentType)
    {
        if (nativeType.IsArray)
        {
            equivalentType = typeof(ArrayLangValue);
            return true;
        }

        // IL 里的元组是 ValueTuple / Tuple；解释器实例方法注册在 TupleLangValue 上。
        if (typeof(System.Runtime.CompilerServices.ITuple).IsAssignableFrom(nativeType) &&
            !typeof(TupleLangValue).IsAssignableFrom(nativeType))
        {
            equivalentType = typeof(TupleLangValue);
            return true;
        }

        if (typeof(Task).IsAssignableFrom(nativeType))
        {
            equivalentType = typeof(TaskLangValue);
            return true;
        }

        if (nativeType.IsGenericType)
        {
            var genericDefinition = nativeType.GetGenericTypeDefinition();
            if (genericDefinition == typeof(List<>))
            {
                equivalentType = typeof(ListLangValue);
                return true;
            }

            if (genericDefinition == typeof(Dictionary<,>))
            {
                equivalentType = typeof(DictionaryLangValue);
                return true;
            }
        }

        equivalentType = typeof(LangValueType);
        return false;
    }
}
