using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
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
    /// <summary>按解释器语义执行一次函数调用。</summary>
    public static object? InvokeInstance(
        string name,
        object?[] positionalArguments,
        string[] namedArgumentNames,
        object?[] namedArgumentValues)
    {
        var manager = RuntimeGlobalWriter.CurrentManager ?? new VariateManager();

        var positional = positionalArguments
            .Select(argument => (LangExpression)IlValueBridge.Wrap(argument))
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
