using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.SortBy(keySelector, ascending?) - 按键选择器排序
/// </summary>
public class LangListSortByMethod : BaseLangListMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["SortBy", "sortBy"];
    public override string[] ParameterNames => ["keySelector", "ascending"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    /// <summary>
    /// 参数类型：keySelector 必须是函数，ascending 可选
    /// </summary>
    public override Type?[]? ParameterTypes => [typeof(FuncLangValue), typeof(BoolLangValue)];

    /// <summary>
    /// 返回类型
    /// </summary>
    public override Type? DeclaredReturnType => typeof(ListLangValue);

    /// <summary>
    /// 方法文档
    /// </summary>
    public override string? Documentation => "按键选择器对列表进行排序";

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var func = parameters[0].Run(manager) as FuncLangValue;

        if (func == null)
        {
            throw new ArgumentError(position, "keySelector 参数必须是函数类型");
        }

        // 检测是否是比较器（两参数）还是 key selector（单参数）
        bool isComparator = func.Ids?.Count >= 2;

        if (isComparator)
        {
            // 比较器模式：(a, b) -> int，直接用作比较函数
            var sortedItems = new List<LangValueType>(items);
            sortedItems.Sort((a, b) =>
            {
                var result = func.Run(manager, [a, b]);
                return result is IntLangValue intResult ? intResult.Value : 0;
            });
            return new ListLangValue(sortedItems);
        }

        // key selector 模式：(item) -> key，按键排序
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
            var key = func.Run(manager, [item]);
            indexedItems.Add((i, item, key));
        }

        // 排序
        indexedItems.Sort((a, b) =>
        {
            var comparison = CompareKeys(a.key, b.key);
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

    /// <summary>
    /// IL 模式：接收者是原生集合，keySelector 是编译好的 .NET 委托，ascending 可选。
    /// 按实际参数个数生成加载代码，缺少 ascending 时压入 null。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        if (parameters.Count > 1)
        {
            IlValueBridge.EmitLoadWrapped(parameters[1], ilGenerator, local);
        }
        else
        {
            ilGenerator.Emit(OpCodes.Ldnull);
        }

        var helperMethod = typeof(LangListSortByMethod).GetMethod(nameof(SortByHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：按键选择器（或两参数比较器）排序，返回原生列表。
    /// 与 <see cref="ExecuteInternal"/> 语义一致（稳定排序、支持降序、支持比较器模式）。
    /// </summary>
    public static List<object?> SortByHelper(LangValueType instance, LangValueType keySelector,
        LangValueType? ascending = null)
    {
        var items = GetIlItems(instance);

        if (keySelector is not FuncLangValue func)
        {
            throw new ArgumentException("keySelector 参数必须是函数类型");
        }

        var manager = new VariateManager();

        // 检测是否是比较器（两参数）还是 key selector（单参数）
        // IL 模式下 lambda 被编译成委托，参数个数只能从委托签名上读
        bool isComparator = func switch
        {
            IlDelegateFuncLangValue delegateFunc => delegateFunc.Function.Method.GetParameters().Length >= 2,
            _ => func.Ids?.Count >= 2
        };

        if (isComparator)
        {
            // 比较器模式：(a, b) -> int，直接用作比较函数
            var sortedItems = new List<LangValueType>(items);
            sortedItems.Sort((a, b) =>
            {
                var result = func.Run(manager, [a, b]);
                return result is IntLangValue intResult ? intResult.Value : 0;
            });
            return sortedItems.Select(IlValueBridge.Unwrap).ToList();
        }

        // key selector 模式：(item) -> key，按键排序
        bool isAscending = true;
        switch (ascending)
        {
            case BoolLangValue boolValue:
                isAscending = boolValue.Value;
                break;
            // IL 模式下 bool 字面量按 int32 压栈，经 IlValueBridge.Wrap 后是 IntLangValue
            case IntLangValue intValue:
                isAscending = intValue.Value != 0;
                break;
            case DoubleLangValue doubleValue:
                isAscending = doubleValue.Value != 0;
                break;
        }

        // 创建索引-元素-键的映射列表（用于稳定排序）
        var indexedItems = new List<(int index, LangValueType item, LangValueType key)>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var key = func.Run(manager, [item]);
            indexedItems.Add((i, item, key));
        }

        // 排序
        indexedItems.Sort((a, b) =>
        {
            var comparison = CompareKeys(a.key, b.key);
            if (comparison == 0)
            {
                comparison = a.index.CompareTo(b.index);
            }

            return isAscending ? comparison : -comparison;
        });

        // 提取排序后的元素
        return indexedItems.Select(x => IlValueBridge.Unwrap(x.item)).ToList();
    }

    /// <summary>
    /// 与 <see cref="BaseLangListMethod.GetItems"/> 等价的静态版本（helper 必须是静态方法）。
    /// </summary>
    private static List<LangValueType> GetIlItems(LangValueType instance)
    {
        if (instance is ILangList langList)
        {
            return langList.GetItems().ToList();
        }

        throw new ArgumentException($"实例必须实现 ILangList 接口，当前类型：{instance.GetType().Name}");
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
