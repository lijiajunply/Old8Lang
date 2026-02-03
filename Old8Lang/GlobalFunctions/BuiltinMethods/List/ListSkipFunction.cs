using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Skip(count) - 跳过列表的前 n 个元素，返回剩余元素
/// </summary>
/// <remarks>
/// 用法: list.Skip(count)
/// 返回: 包含剩余元素的新列表
/// </remarks>
public sealed class ListSkipFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Skip"];
    public override string[]? ParameterNames => ["list", "count"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var count = ((IntLangValue)results[1]).Value;

        var skipCount = Math.Max(0, Math.Min(count, list.Values.Count));
        var result = list.Values.Skip(skipCount).ToList();
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 保存 list
        var listLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, listLocal);

        // 加载 count
        parameters[1].LoadIlValue(ilGenerator, local);
        var countLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, countLocal);

        // 获取列表长度
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        var listCountLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, listCountLocal);

        // 调用 GetRange(skipCount, listCount - skipCount)
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, countLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listCountLocal);
        ilGenerator.Emit(OpCodes.Ldloc, countLocal);
        ilGenerator.Emit(OpCodes.Sub);

        var getRangeMethod = typeof(List<object>).GetMethod("GetRange", [typeof(int), typeof(int)])!;
        ilGenerator.Emit(OpCodes.Callvirt, getRangeMethod);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object>);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var count = (int)arguments[1]!;

        var skipCount = Math.Max(0, Math.Min(count, list.Count));
        return list.GetRange(skipCount, list.Count - skipCount);
    }
}
