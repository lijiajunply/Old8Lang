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
/// List.First() - 获取列表的第一个元素
/// </summary>
/// <remarks>
/// 用法: list.First()
/// 返回: 列表的第一个元素
/// 异常: 当列表为空时抛出 InvalidOperationError
/// </remarks>
public sealed class ListFirstFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.First"];
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

        if (list.Values.Count == 0)
        {
            throw new InvalidOperationError(list, "列表为空，无法获取第一个元素");
        }

        return list.Values[0];
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载索引 0
        ilGenerator.Emit(OpCodes.Ldc_I4_0);

        // 调用 list[0]
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        if (list.Count == 0)
        {
            throw new InvalidOperationException("列表为空，无法获取第一个元素");
        }

        return list[0];
    }
}
