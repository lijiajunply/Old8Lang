using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Remove(element) - 从列表中移除指定元素
/// </summary>
/// <remarks>
/// 用法: list.Remove(value)
/// 返回: 被移除的元素
/// 异常: 当元素不存在时抛出 InvalidOperationError
/// </remarks>
public sealed class ListRemoveFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Remove"];
    public override string[]? ParameterNames => ["list", "element"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var element = results[1];

        for (var i = 0; i < list.Values.Count; i++)
        {
            if (!list.Values[i].Equal(element)) continue;
            var removed = list.Values[i];
            list.Values.RemoveAt(i);
            return removed;
        }

        throw new InvalidOperationError(list, "找不到要移除的元素");
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 element
        parameters[1].LoadIlValue(ilGenerator, local);
        var elementType = parameters[1].OutputType(local);

        // 如果元素是值类型，需要装箱
        if (elementType is not null && elementType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, elementType);
        }

        // 调用 List<object>.Remove(object) - 返回 bool
        var removeMethod = typeof(List<object>).GetMethod("Remove", [typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, removeMethod);

        // Remove 返回 bool，但我们需要返回被移除的元素
        // 简化处理：返回传入的元素
        ilGenerator.Emit(OpCodes.Pop); // 丢弃 bool 结果
        parameters[1].LoadIlValue(ilGenerator, local);
        if (elementType is not null && elementType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, elementType);
        }
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var element = arguments[1];
        list.Remove(element!);
        return element;
    }
}
