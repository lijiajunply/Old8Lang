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
/// List.Min() - 获取列表中的最小值
/// </summary>
/// <remarks>
/// 用法: list.Min()
/// 返回: 最小值
/// 异常: 当列表为空时抛出
/// </remarks>
public sealed class ListMinFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Min"];
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
            throw new InvalidOperationError(list, "无法对空列表求最小值");
        }

        var min = list.Values[0];
        for (int i = 1; i < list.Values.Count; i++)
        {
            if (list.Values[i].Less(min))
            {
                min = list.Values[i];
            }
        }

        return min;
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

        // min 变量
        var minLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        ilGenerator.Emit(OpCodes.Stloc, minLocal);

        // 索引变量
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        ilGenerator.MarkLabel(loopStart);

        // 检查 index < count
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Bge, loopEnd);

        // 获取当前元素
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        var currentLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, currentLocal);

        // 比较: current < min
        var compareMethod = typeof(Comparer<object>).GetProperty("Default")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Call, compareMethod);
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        ilGenerator.Emit(OpCodes.Ldloc, minLocal);
        var compareToMethod = typeof(Comparer<object>).GetMethod("Compare", [typeof(object), typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, compareToMethod);

        // 如果 current >= min，跳过更新
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Bge, continueLabel);

        // 更新 min
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        ilGenerator.Emit(OpCodes.Stloc, minLocal);

        ilGenerator.MarkLabel(continueLabel);

        // index++
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);
        ilGenerator.Emit(OpCodes.Br, loopStart);

        ilGenerator.MarkLabel(loopEnd);

        // 返回 min
        ilGenerator.Emit(OpCodes.Ldloc, minLocal);
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
            throw new InvalidOperationException("无法对空列表求最小值");
        }

        var min = list[0];
        for (int i = 1; i < list.Count; i++)
        {
            if (Comparer<object>.Default.Compare(list[i], min) < 0)
            {
                min = list[i];
            }
        }

        return min;
    }
}
