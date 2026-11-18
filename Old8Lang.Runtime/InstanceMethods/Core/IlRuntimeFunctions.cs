using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// IL 产物中「编译期无法直接解析」的函数调用。
/// </summary>
/// <remarks>
/// 典型场景：相互递归里后定义的函数、命名实参 / <c>params</c> 变参调用、
/// 以及真正未定义的函数。这里把原始调用（位置实参 + 命名实参）重建为
/// 解释器 <see cref="Instance"/> 节点执行：语义与解释器一致
/// （包含命名参数重排、params 收集、未定义函数报 NAME_ERROR），
/// 不再像以前那样发 <c>ldnull</c> 让调用静默返回 null。
/// </remarks>
public static class IlRuntimeFunctions
{
    private static LangExpression WrapArgument(object? argument)
        => argument is Delegate function
            ? IlValueBridge.WrapFunction(function)
            : IlValueBridge.Wrap(argument);

    /// <summary>按名字调用解释器里的同步函数（参数为位置实参）。</summary>
    public static object? InvokeByName(string name, object?[] arguments)
    {
        var manager = RuntimeGlobalWriter.CurrentManager ?? new VariateManager();

        var candidate = manager.ImportInfos
            .OfType<FuncLangValue>()
            .FirstOrDefault(func => func.Id?.IdName == name && func.Ids?.Count == arguments.Length);

        if (candidate is null)
        {
            throw new NameError(new SourcePosition(), name);
        }

        var parameters = arguments
            .Select(argument => (LangExpression)IlValueBridge.Wrap(argument))
            .ToList();

        // 装饰器 wrapper 是解释器函数，读顶层变量走解释器管理器；IL 顶层变量
        // 存在 RuntimeGlobals 槽位里。调用前同步槽位 → 管理器，调用后再写回。
        var slotSnapshot = RuntimeGlobals.Snapshot();
        foreach (var (slotName, slotValue) in slotSnapshot)
        {
            RuntimeGlobalWriter.Write(slotName, slotValue);
        }

        var result = candidate.Run(manager, parameters);

        foreach (var slotName in slotSnapshot.Keys)
        {
            try
            {
                var slotValue = manager.GetValue(new LangId(slotName));
                if (slotValue is not null)
                {
                    RuntimeGlobals.Set(slotName, IlValueBridge.Unwrap(slotValue));
                }
            }
            catch
            {
                // 管理器里没有该变量（例如本次调用未涉及）：保留原槽位值。
            }
        }

        return IlValueBridge.Unwrap(result);
    }

    /// <summary>按解释器语义执行一次函数调用。</summary>
    public static object? InvokeInstance(
        string name,
        object?[] positionalArguments,
        string[] namedArgumentNames,
        object?[] namedArgumentValues)
    {
        var manager = RuntimeGlobalWriter.CurrentManager ?? new VariateManager();

        var positional = positionalArguments
            .Select(WrapArgument)
            .ToList();

        var named = new List<NamedArgument>();
        for (var index = 0; index < namedArgumentNames.Length; index++)
        {
            named.Add(new NamedArgument(
                namedArgumentNames[index],
                IlValueBridge.Wrap(namedArgumentValues[index])));
        }

        var instance = new Instance(new LangId(name), positional, named);
        var result = instance.Accept(RuntimeVisitors.CreateInterpreter(manager));
        return IlValueBridge.Unwrap(result);
    }
}
