using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Slice(start, end) - 获取列表的切片（子列表）
/// </summary>
/// <remarks>
/// 用法: list.Slice(start, end)
/// 返回: 包含切片元素的新列表
/// start: 起始索引（包含）
/// end: 结束索引（不包含）
/// </remarks>
public sealed class ListSliceFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Slice"];
    public override string[]? ParameterNames => ["list", "start", "end"];
    public override int MinParameterCount => 3;
    public override int MaxParameterCount => 3;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var start = ((IntLangValue)results[1]).Value;
        var end = ((IntLangValue)results[2]).Value;

        var startIndex = Math.Max(0, Math.Min(start, list.Values.Count));
        var endIndex = Math.Max(0, Math.Min(end, list.Values.Count));

        if (startIndex > endIndex)
        {
            return new ListLangValue(new List<LangExpression>());
        }

        var result = list.Values.Skip(startIndex).Take(endIndex - startIndex).ToList();
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

        // 加载 start
        parameters[1].LoadIlValue(ilGenerator, local);
        var startLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, startLocal);

        // 加载 end
        parameters[2].LoadIlValue(ilGenerator, local);
        var endLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, endLocal);

        // 调用 GetRange(start, count)
        // count = end - start
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, startLocal);
        ilGenerator.Emit(OpCodes.Ldloc, endLocal);
        ilGenerator.Emit(OpCodes.Ldloc, startLocal);
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
        var start = (int)arguments[1]!;
        var end = (int)arguments[2]!;

        var startIndex = Math.Max(0, Math.Min(start, list.Count));
        var endIndex = Math.Max(0, Math.Min(end, list.Count));

        if (startIndex > endIndex)
        {
            return new List<object>();
        }

        return list.GetRange(startIndex, endIndex - startIndex);
    }
}
