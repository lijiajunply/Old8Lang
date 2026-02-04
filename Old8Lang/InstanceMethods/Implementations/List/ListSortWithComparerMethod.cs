using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.SortWithComparer 方法 - 使用自定义比较器排序
/// </summary>
public class ListSortWithComparerMethod : BaseInstanceMethod
{
    public override string[] Names => ["SortWith", "sortWith", "SortBy", "sortBy"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["comparer"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var comparerParam = parameters[0].Run(manager);

        if (comparerParam is not FuncLangValue comparer)
        {
            throw new TypeError(position, $"SortWith 方法的参数必须是函数类型，但实际是 {comparerParam.GetType().Name}");
        }

        var sorted = new List<LangValueType>(list.Values);

        // 使用快速排序算法
        QuickSortWithComparer(sorted, 0, sorted.Count - 1, comparer);

        return new ListLangValue(sorted);
    }

    /// <summary>
    /// 使用自定义比较器的快速排序
    /// </summary>
    private static void QuickSortWithComparer(List<LangValueType> list, int left, int right, FuncLangValue comparer)
    {
        if (left < right)
        {
            int pivotIndex = PartitionWithComparer(list, left, right, comparer);
            QuickSortWithComparer(list, left, pivotIndex - 1, comparer);
            QuickSortWithComparer(list, pivotIndex + 1, right, comparer);
        }
    }

    /// <summary>
    /// 分区操作
    /// </summary>
    private static int PartitionWithComparer(List<LangValueType> list, int left, int right, FuncLangValue comparer)
    {
        var pivot = list[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            var tempManager = new VariateManager();
            var result = comparer.Run(tempManager, [list[j], pivot]);

            if (result is IntLangValue intResult && intResult.Value < 0)
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
        // 加载列表实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载比较器函数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(ListSortWithComparerMethod).GetMethod(nameof(SortWithComparerHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：使用比较器排序
    /// </summary>
    public static ListLangValue SortWithComparerHelper(ListLangValue list, LangValueType comparerParam)
    {
        if (comparerParam is not FuncLangValue comparer)
        {
            throw new Exception("SortWith 方法的参数必须是函数类型");
        }

        var sorted = new List<LangValueType>(list.Values);
        QuickSortWithComparer(sorted, 0, sorted.Count - 1, comparer);
        return new ListLangValue(sorted);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            var comparer = arguments[0];
            var vm = VMContext.CurrentVM;

            var sorted = new List<object?>(list);
            sorted.Sort((a, b) =>
            {
                try
                {
                    var result = vm.CallFunctionObject(comparer, [a, b]);
                    if (result is int intResult)
                    {
                        return intResult;
                    }
                    // 尝试转换为 int
                    return Convert.ToInt32(result);
                }
                catch
                {
                    return 0;
                }
            });
            return sorted;
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
