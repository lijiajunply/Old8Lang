using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.IndexOf(element) - 查找元素在列表中第一次出现的索引
/// </summary>
/// <remarks>
/// 用法: list.IndexOf(element)
/// 返回: int (未找到返回 -1)
/// </remarks>
public sealed class ListIndexOfFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.IndexOf"];
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
            if (list.Values[i].Equal(element))
            {
                return new IntLangValue(i);
            }
        }

        return new IntLangValue(-1);
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

        // 调用 List<object>.IndexOf(object)
        var indexOfMethod = typeof(List<object>).GetMethod("IndexOf", [typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, indexOfMethod);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var element = arguments[1];
        return list.IndexOf(element!);
    }
}
