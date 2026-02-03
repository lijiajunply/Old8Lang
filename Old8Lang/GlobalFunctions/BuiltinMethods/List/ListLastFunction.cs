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
/// List.Last() - 获取列表的最后一个元素
/// </summary>
/// <remarks>
/// 用法: list.Last()
/// 返回: 列表的最后一个元素
/// 异常: 当列表为空时抛出 InvalidOperationError
/// </remarks>
public sealed class ListLastFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Last"];
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
            throw new InvalidOperationError(list, "列表为空，无法获取最后一个元素");
        }

        return list.Values[^1];
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 保存 list 到局部变量
        var listLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, listLocal);

        // 获取 Count - 1 作为最后一个索引
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Sub);

        // 获取 list[Count - 1]
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Sub);
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
            throw new InvalidOperationException("列表为空，无法获取最后一个元素");
        }

        return list[^1];
    }
}
