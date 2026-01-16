using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Old8Lang.Compiler;

public static class CompiledDelegateRegistry
{
    private static readonly ConcurrentDictionary<string, Delegate> Delegates = new();

    public static void Register(string key, System.Reflection.Emit.DynamicMethod method)
    {
        var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        var delegateType = Expression.GetDelegateType(parameterTypes.Concat([method.ReturnType]).ToArray());
        var del = method.CreateDelegate(delegateType);
        Delegates[key] = del;
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

