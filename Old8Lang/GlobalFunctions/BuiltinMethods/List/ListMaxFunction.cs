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
/// List.Max() - 获取列表中的最大值
/// </summary>
/// <remarks>
/// 用法: list.Max()
/// 返回: 最大值
/// 异常: 当列表为空时抛出
/// </remarks>
public sealed class ListMaxFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Max"];
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
            throw new InvalidOperationError(list, "无法对空列表求最大值");
        }

        var max = list.Values[0];
        for (int i = 1; i < list.Values.Count; i++)
        {
            if (max.Less(list.Values[i]))
            {
                max = list.Values[i];
            }
        }

        return max;
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

        // max 变量
        var maxLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        ilGenerator.Emit(OpCodes.Stloc, maxLocal);

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

        // 比较: current > max
        var compareMethod = typeof(Comparer<object>).GetProperty("Default")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Call, compareMethod);
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        ilGenerator.Emit(OpCodes.Ldloc, maxLocal);
        var compareToMethod = typeof(Comparer<object>).GetMethod("Compare", [typeof(object), typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, compareToMethod);

        // 如果 current <= max，跳过更新
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Ble, continueLabel);

        // 更新 max
        ilGenerator.Emit(OpCodes.Ldloc, currentLocal);
        ilGenerator.Emit(OpCodes.Stloc, maxLocal);

        ilGenerator.MarkLabel(continueLabel);

        // index++
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);
        ilGenerator.Emit(OpCodes.Br, loopStart);

        ilGenerator.MarkLabel(loopEnd);

        // 返回 max
        ilGenerator.Emit(OpCodes.Ldloc, maxLocal);
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
            throw new InvalidOperationException("无法对空列表求最大值");
        }

        var max = list[0];
        for (int i = 1; i < list.Count; i++)
        {
            if (Comparer<object>.Default.Compare(list[i], max) > 0)
            {
                max = list[i];
            }
        }

        return max;
    }
}
