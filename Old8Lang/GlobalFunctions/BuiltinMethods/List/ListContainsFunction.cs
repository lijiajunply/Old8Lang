using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Contains(element) - 检查列表是否包含指定元素
/// </summary>
/// <remarks>
/// 用法: list.Contains(element)
/// 返回: bool
/// </remarks>
public sealed class ListContainsFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Contains"];
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

        return new BoolLangValue(list.Values.Any(item => item.Equal(element)));
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

        // 调用 List<object>.Contains(object)
        var containsMethod = typeof(List<object>).GetMethod("Contains", [typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, containsMethod);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var element = arguments[1];
        return list.Contains(element!);
    }
}
