using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 元组
/// </summary>
/// <param name="v1"></param>
/// <param name="v2"></param>
/// <param name="position"></param>
public class TupleLangValue(LangExpression v1, LangExpression v2, SourcePosition position = default)
    : LangValueType(position), ILangList
{
    public readonly LangExpression V1 = v1;
    public readonly LangExpression V2 = v2;

    public ValueTuple<LangValueType, LangValueType> Value { get; private set; }

    public override LangValueType Run(VariateManager manager)
    {
        // 运行第一个元素
        var item1Result = V1.Run(manager);

        // 运行第二个元素，处理空名称的特殊情况
        LangValueType item2Result;
        if (V2 is LangId item2Id && string.IsNullOrEmpty(item2Id.IdName))
        {
            // 如果第二个元素是空名称的LangId，直接使用NullLangValue，避免NameError
            item2Result = NullLangValue.Instance;
        }
        else
        {
            // 正常运行第二个元素
            item2Result = V2.Run(manager);
        }

        Value = (item1Result, item2Result);
        return this;
    }

    public override string ToString() => $"({V1},{V2})";

    /// <summary>
    /// 明确标识元组类型，防止与列表混淆
    /// </summary>
    /// <returns>返回 "Tuple" 作为类型标识</returns>
    public override string TypeToString() => "Tuple";

    public override object GetValue() => (Value.Item1.GetValue(), Value.Item2.GetValue());

    // 支持多元元组的构造函数
    public TupleLangValue(List<LangExpression> elements, SourcePosition position = default) : this(elements[0],
        elements[1], position)
    {
        // 如果是多元元组，递归构建嵌套结构
        for (int i = 2; i < elements.Count; i++)
        {
            V2 = new TupleLangValue(V2, elements[i], position);
        }
    }

    /// <summary>
    /// 创建空元组的专用构造函数
    /// </summary>
    /// <param name="isEmpty"></param>
    /// <param name="position">源代码位置</param>
    public TupleLangValue(bool isEmpty, SourcePosition position = default) : this(new NullLangValue(),
        new NullLangValue(), position)
    {
        if (isEmpty)
        {
            // 标记为空元组
            IsEmpty = true;
        }
        else
        {
            throw new ArgumentException("使用此构造函数时必须指定 isEmpty=true");
        }
    }

    // 标记是否为空元组
    private readonly bool IsEmpty;

    /// <summary>
    /// 获取元组指定索引的元素
    /// </summary>
    /// <param name="index">索引值</param>
    /// <returns>指定索引的元素</returns>
    public LangValueType Get(IntLangValue index)
    {
        return Get(index.Value);
    }

    /// <summary>
    /// 获取元组指定索引的元素（支持嵌套多元元组）
    /// </summary>
    /// <param name="index">索引值</param>
    /// <returns>指定索引的元素</returns>
    public LangValueType Get(int index)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationError(this, $"元组索引越界: {index}，空元组不支持任何索引访问");
        }

        // 收集所有元素用于扁平化访问
        var allElements = new List<LangValueType>();
        CollectElements(this, allElements);

        if (index < 0 || index >= allElements.Count)
        {
            throw new InvalidOperationError(this, $"元组索引越界: {index}，当前元组只支持索引 0 到 {allElements.Count - 1}");
        }

        // 返回扁平化访问的结果
        return allElements[index];
    }

    /// <summary>
    /// 获取元组的直接子元素（不支持扁平化）
    /// 用于嵌套访问：tuple[index1][index2]
    /// </summary>
    /// <param name="index">索引值（只能是0或1）</param>
    /// <returns>直接子元素</returns>
    public LangValueType GetDirectChild(int index)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationError(this, $"元组索引越界: {index}，空元组不支持任何索引访问");
        }

        if (index == 0)
        {
            return Value.Item1;
        }

        return index switch
        {
            1 => Value.Item2,
            _ => throw new InvalidOperationError(this, $"元组直接子元素访问索引越界: {index}，只支持索引 0 和 1")
        };
    }

    /// <summary>
    /// 支持元组的点操作访问，包括索引访问
    /// </summary>
    /// <param name="dotExpression">点操作表达式</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>访问结果</returns>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 支持数字索引访问：tuple.0, tuple.1, tuple.2 等
        if (dotExpression is LangId id)
        {
            if (int.TryParse(id.IdName, out int index))
            {
                return Get(index);
            }

            // 支持 ToStr() 方法
            if (id.IdName == "ToStr")
            {
                return new StringLangValue(ToString());
            }

            // 支持 Length 属性
            if (id.IdName == "Length")
            {
                // 计算元组的长度（对于嵌套元组，需要计算所有元素的总数）
                int length = GetLength();
                return new IntLangValue(length);
            }
        }

        // 如果是Instance，检查是否是方法调用
        if (dotExpression is Instance instance)
        {
            // 设置执行上下文，以便扩展方法可以访问当前的 VariateManager
            Old8Lang.AST.Expression.ValueFunctions.ExecutionContext.SetCurrentManager(manager);

            // 处理无参数的方法调用
            if (instance.Ids.Count == 0)
            {
                if (instance.Id.IdName == "ToStr")
                {
                    return new StringLangValue(ToString());
                }

                if (instance.Id.IdName == "Length")
                {
                    // 计算元组的长度（对于嵌套元组，需要计算所有元素的总数）
                    int length = GetLength();
                    return new IntLangValue(length);
                }
            }

            // 对于其他方法调用，使用扩展方法机制
            return instance.FromClassToResult(this);
        }

        throw new InvalidOperationError(this, $"不支持元组的点操作: {dotExpression}");
    }

    /// <summary>
    /// 计算元组的长度（支持嵌套多元元组）
    /// </summary>
    /// <param name="tuple">要计算长度的元组</param>
    /// <returns>元组中元素的总数</returns>
    private static int GetTupleLength(TupleLangValue tuple)
    {
        // 如果第二个元素是嵌套元组，则递归计算长度
        if (tuple.Value.Item2 is TupleLangValue lengthTuple)
        {
            return 1 + GetTupleLength(lengthTuple);
        }

        return 2; // 二元元组固定为2个元素
    }

    /// <summary>
    /// 覆盖 Equal 方法以支持元组深度比较
    /// 确保元组只与元组比较，不会被误认为列表
    /// </summary>
    /// <param name="otherValueType">要比较的值类型</param>
    /// <returns>只有同为元组且元素相等时才返回true</returns>
    public override bool Equal(LangValueType? otherValueType)
    {
        // 严格的类型检查：只有同为元组才能比较
        if (otherValueType is not TupleLangValue otherTuple)
            return false;

        // 比较两个元素是否都相等
        return Value.Item1.Equal(otherTuple.Value.Item1) &&
               Value.Item2.Equal(otherTuple.Value.Item2);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 获取两个元素的类型
        var type1 = V1.OutputType(local) ?? typeof(object);
        var type2 = V2.OutputType(local) ?? typeof(object);

        // 获取元组类型
        var tupleType = typeof(ValueTuple<,>).MakeGenericType(type1, type2);

        // 获取元组构造函数
        var constructor = tupleType.GetConstructor([type1, type2])!;

        // 加载第一个元素的值
        V1.LoadIlValue(ilGenerator, local);

        // 加载第二个元素的值
        V2.LoadIlValue(ilGenerator, local);

        // 调用元组构造函数创建元组实例
        ilGenerator.Emit(OpCodes.Newobj, constructor);
    }

    public override Type OutputType(LocalManager local)
    {
        // 获取两个元素的的类型
        var type1 = V1.OutputType(local);
        var type2 = V2.OutputType(local);

        // 确保类型不为空
        if (type1 == null || type2 == null)
        {
            return typeof(ValueTuple<object, object>);
        }

        // 返回对应的元组类型
        return typeof(ValueTuple<,>).MakeGenericType(type1, type2);
    }

    // 实现ILangList接口
    public IEnumerable<LangValueType> GetItems()
    {
        if (IsEmpty)
        {
            return new List<LangValueType>();
        }

        var items = new List<LangValueType>();
        CollectElements(this, items);
        return items;
    }

    public int GetLength()
    {
        if (IsEmpty)
        {
            return 0;
        }

        var items = new List<LangValueType>();
        CollectElements(this, items);
        return items.Count;
    }

    public LangValueType Slice(int start, int end)
    {
        var items = new List<LangValueType>();
        CollectElements(this, items);

        // 处理负数索引：-1 表示最后一个元素
        if (start < 0) start += items.Count;
        if (end < 0) end += items.Count;

        // 确保索引在有效范围内
        start = Math.Max(0, Math.Min(start, items.Count));
        end = Math.Max(0, Math.Min(end, items.Count));

        // 如果 start >= end，返回空元组
        if (start >= end)
        {
            return new TupleLangValue(new NullLangValue(), new NullLangValue());
        }

        // 创建子元组
        var sliceItems = items.Skip(start).Take(end - start).ToList();
        if (sliceItems.Count == 0)
        {
            return new TupleLangValue(new NullLangValue(), new NullLangValue());
        }

        return CreateTupleFromList(sliceItems);
    }

    public LangValueType Slice(int start, int end, int step)
    {
        var items = new List<LangValueType>();
        CollectElements(this, items);
        var length = items.Count;
        var result = new List<LangValueType>();

        if (step > 0)
        {
            // 正向切片
            if (start < 0) start += length;
            if (end < 0) end += length;

            start = Math.Max(0, Math.Min(start, length));
            end = Math.Max(0, Math.Min(end, length));

            for (int i = start; i < end; i += step)
            {
                result.Add(items[i]);
            }
        }
        else if (step < 0)
        {
            // 反向切片
            // 处理负数索引，但保留 -1 作为特殊值（表示"到开头之前"）
            if (start < -1) start += length;
            if (end < -1) end += length;

            // 设置边界
            if (start >= length) start = length - 1;
            if (start < -1) start = -1;
            if (end >= length) end = length - 1;

            for (int i = start; i > end; i += step)
            {
                result.Add(items[i]);
            }
        }
        else
        {
            throw new InvalidOperationError(this, "切片步长不能为0");
        }

        if (result.Count == 0)
        {
            return new TupleLangValue(new NullLangValue(), new NullLangValue());
        }

        return CreateTupleFromList(result);
    }

    public void Set(LangValueType index, LangValueType value)
    {
        if (index is IntLangValue indexValue)
        {
            Set(indexValue.Value, value);
        }
        else
        {
            throw new InvalidOperationError(this, "元组不支持使用非整数索引进行赋值");
        }
    }

    public void Set(int index, LangValueType value)
    {
        var allElements = new List<LangValueType>();
        CollectElements(this, allElements);

        if (index < 0 || index >= allElements.Count)
        {
            throw new InvalidOperationError(this, $"元组索引越界: {index}，当前元组只支持索引 0 到 {allElements.Count - 1}");
        }

        allElements[index] = value;

        // 重建元组结构
        RebuildFromElements(allElements);
    }

    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        throw new InvalidOperationError(this, "元组是不可变类型，不支持切片赋值操作");
    }

    /// <summary>
    /// 从元素列表重建元组结构
    /// </summary>
    /// <param name="elements">元素列表</param>
    private void RebuildFromElements(List<LangValueType> elements)
    {
        if (elements.Count == 0)
        {
            Value = (NullLangValue.Instance, NullLangValue.Instance);
            return;
        }

        if (elements.Count == 1)
        {
            Value = (elements[0], NullLangValue.Instance);
            return;
        }

        if (elements.Count == 2)
        {
            Value = (elements[0], elements[1]);
            return;
        }

        // 对于多元元组（>2个元素），创建嵌套结构
        var nested = BuildNestedTupleWithValues(elements, 1);
        Value = (elements[0], nested);
    }

    public bool In(LangValueType value)
    {
        var items = new List<LangValueType>();
        CollectElements(this, items);
        return items.Any(item => item.Equal(value));
    }

    /// <summary>
    /// 递归收集元组中的所有元素
    /// </summary>
    /// <param name="tuple">当前元组</param>
    /// <param name="items">元素列表</param>
    private static void CollectElements(TupleLangValue tuple, List<LangValueType> items)
    {
        // 收集第一个元素
        if (tuple.Value.Item1 is TupleLangValue nested1)
        {
            CollectElements(nested1, items);
        }
        else if (tuple.Value.Item1 is not NullLangValue)
        {
            // 跳过 NullLangValue（用于单元素元组的占位符）
            items.Add(tuple.Value.Item1);
        }

        // 收集第二个元素
        if (tuple.Value.Item2 is TupleLangValue nested2)
        {
            CollectElements(nested2, items);
        }
        else if (tuple.Value.Item2 is not NullLangValue)
        {
            // 跳过 NullLangValue（用于单元素元组的占位符）
            items.Add(tuple.Value.Item2);
        }
    }

    /// <summary>
    /// 从元素列表创建元组
    /// </summary>
    /// <param name="elements">元素列表</param>
    /// <returns>对应的元组</returns>
    private static TupleLangValue CreateTupleFromList(List<LangValueType> elements)
    {
        if (elements.Count == 0)
        {
            var emptyTuple = new TupleLangValue(NullLangValue.Instance, NullLangValue.Instance);
            emptyTuple.Value = (NullLangValue.Instance, NullLangValue.Instance);
            return emptyTuple;
        }

        if (elements.Count == 1)
        {
            var singleTuple = new TupleLangValue(elements[0], NullLangValue.Instance);
            singleTuple.Value = (elements[0], NullLangValue.Instance);
            return singleTuple;
        }

        if (elements.Count == 2)
        {
            var twoTuple = new TupleLangValue(elements[0], elements[1]);
            twoTuple.Value = (elements[0], elements[1]);
            return twoTuple;
        }

        // 对于多元元组（>2个元素），创建嵌套结构并设置Value
        var result = BuildNestedTupleWithValues(elements, 0);
        return result;
    }

    /// <summary>
    /// 从元素列表递归构建嵌套元组，并直接设置Value
    /// </summary>
    private static TupleLangValue BuildNestedTupleWithValues(List<LangValueType> elements, int index)
    {
        if (index >= elements.Count - 1)
        {
            // 最后一个元素
            var lastTuple = new TupleLangValue(elements[index], NullLangValue.Instance);
            lastTuple.Value = (elements[index], NullLangValue.Instance);
            return lastTuple;
        }

        if (index == elements.Count - 2)
        {
            // 最后两个元素
            var tuple = new TupleLangValue(elements[index], elements[index + 1]);
            tuple.Value = (elements[index], elements[index + 1]);
            return tuple;
        }

        // 递归构建
        var nested = BuildNestedTupleWithValues(elements, index + 1);
        var currentTuple = new TupleLangValue(elements[index], nested);
        currentTuple.Value = (elements[index], nested);
        return currentTuple;
    }
}