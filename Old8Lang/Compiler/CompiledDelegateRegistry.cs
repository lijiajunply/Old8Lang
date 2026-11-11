using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Old8Lang.Compiler;

public static class CompiledDelegateRegistry
{
    private static readonly ConcurrentDictionary<string, Delegate> Delegates = new();

    /// <summary>
    /// 按整数 id 持有的委托表，供 IL 模式把 lambda / 函数引用当作「函数值」传给实例方法使用。
    /// 生成代码只嵌入 id，运行时通过 <see cref="Get"/> 取回委托实例。
    /// </summary>
    private static readonly ConcurrentDictionary<int, Delegate> FunctionValues = new();

    private static readonly ConcurrentDictionary<int, System.Reflection.Emit.DynamicMethod> ClosureTemplates = new();

    private static int _nextFunctionValueId;

    public static void Register(string key, System.Reflection.Emit.DynamicMethod method)
    {
        var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        var delegateType = Expression.GetDelegateType(parameterTypes.Concat([method.ReturnType]).ToArray());
        var del = method.CreateDelegate(delegateType);
        Delegates[key] = del;
    }

    /// <summary>
    /// 把一个已编译的方法（<see cref="System.Reflection.Emit.DynamicMethod"/> 或普通方法）
    /// 注册成运行期可直接取用的委托。
    /// </summary>
    /// <param name="method">已生成 IL 体的方法</param>
    /// <returns>委托在注册表中的 id，用于在生成的 IL 中通过 <see cref="Get"/> 取回</returns>
    public static int RegisterFunctionValue(System.Reflection.MethodInfo method)
    {
        var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        var delegateType = Expression.GetDelegateType(parameterTypes.Concat([method.ReturnType]).ToArray());
        var del = method.CreateDelegate(delegateType);
        var id = Interlocked.Increment(ref _nextFunctionValueId);
        FunctionValues[id] = del;
        return id;
    }

    /// <summary>
    /// 取回 <see cref="RegisterFunctionValue"/> 注册的委托。
    /// </summary>
    public static Delegate Get(int id)
    {
        if (!FunctionValues.TryGetValue(id, out var del))
        {
            throw new InvalidOperationException($"函数值 '{id}' 未注册");
        }

        return del;
    }

    /// <summary>
    /// 注册首个参数为闭包数组的 lambda 模板。
    /// </summary>
    public static int RegisterClosureTemplate(System.Reflection.Emit.DynamicMethod method)
    {
        var id = Interlocked.Increment(ref _nextFunctionValueId);
        ClosureTemplates[id] = method;
        return id;
    }

    /// <summary>
    /// 将模板的首个闭包数组参数绑定为目标对象，创建独立的函数值。
    /// </summary>
    public static Delegate CreateClosure(int id, object?[] capturedValues)
    {
        if (!ClosureTemplates.TryGetValue(id, out var method))
        {
            throw new InvalidOperationException($"闭包模板 '{id}' 未注册");
        }

        var parameterTypes = method.GetParameters().Skip(1).Select(p => p.ParameterType).ToArray();
        var delegateType = Expression.GetDelegateType(parameterTypes.Concat([method.ReturnType]).ToArray());
        return method.CreateDelegate(delegateType, capturedValues);
    }

    public static object? Invoke(string key, object?[] args)
    {
        if (!Delegates.TryGetValue(key, out var del))
        {
            throw new InvalidOperationException($"Delegate '{key}' not registered");
        }

        return del.DynamicInvoke(args);
    }
}

