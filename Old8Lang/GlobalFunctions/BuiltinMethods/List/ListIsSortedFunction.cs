using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.IsSorted() - 检查列表是否已排序
/// </summary>
/// <remarks>
/// 用法: list.IsSorted()
/// 返回: bool - 如果列表已排序返回 true，否则返回 false
/// </remarks>
public sealed class ListIsSortedFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.IsSorted"];
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

        for (int i = 1; i < list.Values.Count; i++)
        {
            if (list.Values[i].Less(list.Values[i - 1]))
            {
                return new BoolLangValue(false);
            }
        }

        return new BoolLangValue(true);
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

        // 结果变量
        var resultLocal = ilGenerator.DeclareLocal(typeof(bool));
        ilGenerator.Emit(OpCodes.Ldc_I4_1); // 默认为 true
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // 索引变量
        var indexLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);

        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var returnFalse = ilGenerator.DefineLabel();

        ilGenerator.MarkLabel(loopStart);

        // 检查 index < count
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var countProperty = typeof(List<object>).GetProperty("Count")!;
        ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Bge, loopEnd);

        // 获取 list[i] 和 list[i-1]
        var getItemMethod = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;

        // list[i]
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);

        // list[i-1]
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Sub);
        ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);

        // 比较: 使用 IComparable
        var compareMethod = typeof(Comparer<object>).GetProperty("Default")!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Call, compareMethod);
        var compareToMethod = typeof(Comparer<object>).GetMethod("Compare", [typeof(object), typeof(object)])!;
        ilGenerator.Emit(OpCodes.Callvirt, compareToMethod);

        // 如果 list[i] < list[i-1]，返回 false
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Blt, returnFalse);

        // index++
        ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Stloc, indexLocal);
        ilGenerator.Emit(OpCodes.Br, loopStart);

        ilGenerator.MarkLabel(returnFalse);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        ilGenerator.MarkLabel(loopEnd);
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;

        for (int i = 1; i < list.Count; i++)
        {
            if (Comparer<object>.Default.Compare(list[i], list[i - 1]) < 0)
            {
                return false;
            }
        }

        return true;
    }
}
