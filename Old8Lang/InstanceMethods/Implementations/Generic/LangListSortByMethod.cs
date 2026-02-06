using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.SortBy(keySelector, ascending?) - 按键选择器排序
/// </summary>
public class LangListSortByMethod : BaseLangListMethod
{
    public override string[] Names => ["SortBy", "sortBy"];
    public override string[] ParameterNames => ["keySelector", "ascending"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var keySelector = parameters[0].Run(manager) as FuncLangValue;

        if (keySelector == null)
        {
            throw new ArgumentError(position, "keySelector 参数必须是函数类型");
        }

        // 获取排序方向（默认升序）
        bool isAscending = true;
        if (parameters.Count > 1)
        {
            var ascendingValue = parameters[1].Run(manager);
            if (ascendingValue is BoolLangValue boolValue)
            {
                isAscending = boolValue.Value;
            }
        }

        // 创建索引-元素-键的映射列表（用于稳定排序）
        var indexedItems = new List<(int index, LangValueType item, LangValueType key)>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var key = keySelector.Run(manager, [item]);
            indexedItems.Add((i, item, key));
        }

        // 排序
        indexedItems.Sort((a, b) =>
        {
            var comparison = CompareKeys(a.key, b.key);
            // 如果键相同，保持原始顺序（稳定排序）
            if (comparison == 0)
            {
                comparison = a.index.CompareTo(b.index);
            }

            return isAscending ? comparison : -comparison;
        });

        // 提取排序后的元素
        var sortedValues = indexedItems.Select(x => x.item).ToList();
        return new ListLangValue(sortedValues);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("SortBy 方法暂不支持编译模式");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var items = GetItemsForVM(instance);

        if (arguments.Length == 0)
        {
            throw new ArgumentException("需要至少一个参数");
        }

        var keySelector = arguments[0];
        var vm = VMContext.CurrentVM;

        // 获取排序方向（默认升序）
        bool isAscending = true;
        if (arguments.Length > 1 && arguments[1] is bool ascending)
        {
            isAscending = ascending;
        }

        // 创建索引-元素-键的映射列表
        var indexedItems = new List<(int index, object? item, object? key)>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var key = vm.CallFunctionObject(keySelector, [item]);
            indexedItems.Add((i, item, key));
        }

        // 排序
        indexedItems.Sort((a, b) =>
        {
            var comparison = CompareKeysVM(a.key, b.key);
            // 如果键相同，保持原始顺序（稳定排序）
            if (comparison == 0)
            {
                comparison = a.index.CompareTo(b.index);
            }

            return isAscending ? comparison : -comparison;
        });

        // 提取排序后的元素
        return indexedItems.Select(x => x.item).ToList();
    }

    /// <summary>
    /// 比较两个键的大小（解释器模式）
    /// </summary>
    private static int CompareKeys(LangValueType a, LangValueType b)
    {
        return (a, b) switch
        {
            (IntLangValue ia, IntLangValue ib) => ia.Value.CompareTo(ib.Value),
            (DoubleLangValue da, DoubleLangValue db) => da.Value.CompareTo(db.Value),
            (StringLangValue sa, StringLangValue sb) => string.Compare(sa.Value, sb.Value, StringComparison.Ordinal),
            (BoolLangValue ba, BoolLangValue bb) => ba.Value.CompareTo(bb.Value),
            (CharLangValue ca, CharLangValue cb) => ca.Value.CompareTo(cb.Value),
            _ => string.Compare(a.ToDisplayString(), b.ToDisplayString(), StringComparison.Ordinal)
        };
    }

    /// <summary>
    /// 比较两个键的大小（VM 模式）
    /// </summary>
    private static int CompareKeysVM(object? a, object? b)
    {
        return (a, b) switch
        {
            (int ia, int ib) => ia.CompareTo(ib),
            (double da, double db) => da.CompareTo(db),
            (string sa, string sb) => string.Compare(sa, sb, StringComparison.Ordinal),
            (bool ba, bool bb) => ba.CompareTo(bb),
            (char ca, char cb) => ca.CompareTo(cb),
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal)
        };
    }
}
