using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.List;

/// <summary>
/// List.Sort() - 对列表进行排序（默认使用快速排序）
/// </summary>
/// <remarks>
/// 用法: list.Sort()
/// 返回: 排序后的新列表
/// </remarks>
public sealed class ListSortFunction : BaseGlobalFunction
{
    public override string[] Names => ["List.Sort"];
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

        var sortedValues = new List<LangValueType>(list.Values);
        QuickSort(sortedValues, 0, sortedValues.Count - 1);
        return new ListLangValue(sortedValues);
    }

    private static void QuickSort(List<LangValueType> nums, int left, int right)
    {
        while (left < right)
        {
            int pivotIndex = Partition(nums, left, right);
            QuickSort(nums, left, pivotIndex - 1);
            left = pivotIndex + 1;
        }
    }

    private static int Partition(List<LangValueType> nums, int left, int right)
    {
        var pivot = nums[right];
        var i = left - 1;

        for (var j = left; j < right; j++)
        {
            if (!nums[j].Less(pivot)) continue;
            i++;
            (nums[i], nums[j]) = (nums[j], nums[i]);
        }

        (nums[i + 1], nums[right]) = (nums[right], nums[i + 1]);
        return i + 1;
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 保存原列表
        var listLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, listLocal);

        // 创建新列表副本: new List<object>(originalList)
        ilGenerator.Emit(OpCodes.Ldloc, listLocal);
        var listCtor = typeof(List<object>).GetConstructor([typeof(IEnumerable<object>)])!;
        ilGenerator.Emit(OpCodes.Newobj, listCtor);

        // 保存新列表
        var newListLocal = ilGenerator.DeclareLocal(typeof(List<object>));
        ilGenerator.Emit(OpCodes.Stloc, newListLocal);

        // 调用 Sort()
        ilGenerator.Emit(OpCodes.Ldloc, newListLocal);
        var sortMethod = typeof(List<object>).GetMethod("Sort", Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Callvirt, sortMethod);

        // 返回新列表
        ilGenerator.Emit(OpCodes.Ldloc, newListLocal);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object>);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var list = (List<object>)arguments[0]!;
        var newList = new List<object>(list);
        newList.Sort();
        return newList;
    }
}
