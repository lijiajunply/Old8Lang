using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.ElementAtOrDefault(index) - 获取列表中指定索引处的元素，如果索引越界则返回 null
/// </summary>
/// <remarks>
/// 用法: list.ElementAtOrDefault(index)
/// 返回: 指定索引处的元素或 null
/// 支持负数索引（从末尾开始计数）
/// </remarks>
public sealed class ListElementAtOrDefaultFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.ElementAtOrDefault"];
    public override string[]? ParameterNames => ["list", "index"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var list = (ListLangValue)results[0];
        var idx = ((IntLangValue)results[1]).Value;

        // 支持负数索引
        if (idx < 0) idx = list.Values.Count + idx;
        if (idx < 0 || idx >= list.Values.Count)
        {
            return new NullLangValue();
        }

        return list.Values[idx];
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

        // 加载 index
        parameters[1].LoadIlValue(ilGenerator, local);
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        // 处理负数索引
        var positiveIndexLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Bge, positiveIndexLabel);

        // 负数索引: index = count + index
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        ilGenerator.MarkLabel(positiveIndexLabel);

        // 检查索引是否有效
        var returnNullLabel = ilGenerator.DefineLabel();
        var endLabel = ilGenerator.DefineLabel();

        // 检查 index < 0
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Blt, returnNullLabel);

        // 检查 index >= count
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Bge, returnNullLabel);

        // 有效索引: 返回 list[index]
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        // 无效索引: 返回 null
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
        var idx = (int)arguments[1]!;

        if (idx < 0) idx = list.Count + idx;
        if (idx < 0 || idx >= list.Count)
        {
            return null;
        }

        return list[idx];
    }
}
