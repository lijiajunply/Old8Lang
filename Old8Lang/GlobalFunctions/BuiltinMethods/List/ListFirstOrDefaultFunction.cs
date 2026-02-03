using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.FirstOrDefault() - 获取列表的第一个元素，如果列表为空则返回 null
/// </summary>
/// <remarks>
/// 用法: list.FirstOrDefault()
/// 返回: 列表的第一个元素或 null
/// </remarks>
public sealed class ListFirstOrDefaultFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.FirstOrDefault"];
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

        return list.Values.Count == 0 ? new NullLangValue() : list.Values[0];
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

        // 检查 Count > 0
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Cgt);

        var returnNullLabel = ilGenerator.DefineLabel();
        var endLabel = ilGenerator.DefineLabel();

        ilGenerator.Emit(OpCodes.Brfalse, returnNullLabel);

        // Count > 0: 返回 list[0]
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        // Count == 0: 返回 null
        ilGenerator.MarkLabel(returnNullLabel);
        ilGenerator.Emit(OpCodes.Ldnull);

        ilGenerator.MarkLabel(endLabel);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        return list.Count == 0 ? null : list[0];
    }
}
