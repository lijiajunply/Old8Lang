using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Sort() - 排序（升序）
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListSortMethod : BaseLangListMethod
{
    public override string[] Names => ["Sort", "sort"];
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    /// <summary>
    /// 参数类型：无参数
    /// </summary>
    public override Type?[]? ParameterTypes => [];

    /// <summary>
    /// 返回类型
    /// </summary>
    public override Type? DeclaredReturnType => typeof(ListLangValue);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "对列表进行升序排序";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var sortedItems = new List<LangValueType>(items);

        // 使用快速排序
        QuickSortInternal(sortedItems, 0, sortedItems.Count - 1);

        return new ListLangValue(sortedItems, null, position);
    }

    private void QuickSortInternal(List<LangValueType> list, int left, int right)
    {
        if (left < right)
        {
            int pivotIndex = Partition(list, left, right);
            QuickSortInternal(list, left, pivotIndex - 1);
            QuickSortInternal(list, pivotIndex + 1, right);
        }
    }

    private int Partition(List<LangValueType> list, int left, int right)
    {
        var pivot = list[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (list[j].Less(pivot) || list[j].Equal(pivot))
            {
                i++;
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        (list[i + 1], list[right]) = (list[right], list[i + 1]);
        return i + 1;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListSortMethod).GetMethod(nameof(SortHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue SortHelper(ILangList langList)
    {
        var items = langList.GetItems().ToList();
        var sortedItems = new List<LangValueType>(items);

        // 使用 LINQ OrderBy
        sortedItems = sortedItems.OrderBy(x => x, new LangValueComparer()).ToList();

        return new ListLangValue(sortedItems);
    }

    private class LangValueComparer : IComparer<LangValueType>
    {
        public int Compare(LangValueType? x, LangValueType? y)
        {
            if (x == null || y == null) return 0;
            if (x.Less(y)) return -1;
            if (x.Greater(y)) return 1;
            return 0;
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList)
        {
            return SortHelper(langList);
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口，当前类型：{instance?.GetType().Name}");
    }
}
