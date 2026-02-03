using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.ToArray() - 将列表转换为数组
/// </summary>
/// <remarks>
/// 用法: list.ToArray()
/// 返回: 包含相同元素的新数组
/// </remarks>
public sealed class ListToArrayFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.ToArray"];
    public override string[]? ParameterNames => ["list"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];

        return new ArrayLangValue(list.Values);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 调用 List<object>.ToArray()
        var toArrayMethod = typeof(List<object>).GetMethod("ToArray", Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Callvirt, toArrayMethod);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object[]);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        return list.ToArray();
    }
}
