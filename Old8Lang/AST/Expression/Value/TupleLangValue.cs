using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 元组 - 支持命名和未命名元组
/// 重构后支持扁平化存储，并在编译模式下生成标准的 C# ValueTuple
/// </summary>
public partial class TupleLangValue : LangValueType, ILangList
{
    /// <summary>
    /// 元组的所有元素表达式
    /// </summary>
    public readonly List<LangExpression> Elements;

    /// <summary>
    /// 解释模式下的运行时值
    /// </summary>
    public List<LangValueType> ItemValues { get; private set; } = new();

    // 命名字段支持 - 存储字段名称
    private readonly Dictionary<string, int>? _fieldNames = null;

    // 标记是否有命名字段
    public bool HasNamedFields => _fieldNames is not null && _fieldNames.Count > 0;

    // 标记是否为空元组
    private readonly bool IsEmpty;

    /// <summary>
    /// 构造函数：双元素元组（向后兼容）
    /// </summary>
    public TupleLangValue(LangExpression v1, LangExpression v2, SourcePosition position = default)
        : base(position)
    {
        Elements = new List<LangExpression> { v1, v2 };
        IsEmpty = false;
    }

    /// <summary>
    /// 构造函数：多元元组
    /// </summary>
    public TupleLangValue(List<LangExpression> elements, SourcePosition position = default)
        : base(position)
    {
        Elements = elements ?? new List<LangExpression>();
        IsEmpty = Elements.Count == 0;
        
        // 如果是空列表，为了安全起见，添加两个NullLangValue? 
        // 不，空元组应该是允许的，但在 Old8Lang 旧逻辑中似乎用 (Null, Null) 表示空。
        // 我们这里支持真正的空元组。
        if (IsEmpty)
        {
            // 旧逻辑兼容：如果是显式空元组，可能需要特殊处理，但新逻辑允许 Count=0
        }
    }

    /// <summary>
    /// 支持命名字段的多元元组构造函数
    /// </summary>
    public TupleLangValue(List<LangExpression> elements, List<string?>? fieldNames, SourcePosition position = default)
        : this(elements, position)
    {
        // 设置字段名称映射
        if (fieldNames is not null && fieldNames.Count > 0)
        {
            _fieldNames = new Dictionary<string, int>();
            for (int i = 0; i < fieldNames.Count && i < elements.Count; i++)
            {
                if (!string.IsNullOrEmpty(fieldNames[i]))
                {
                    _fieldNames[fieldNames[i]!] = i;
                }
            }
        }
    }

    public override LangValueType Run(VariateManager manager)
    {
        ItemValues.Clear();
        foreach (var expr in Elements)
        {
            var result = expr.Run(manager);
            ItemValues.Add(result);
        }
        return this;
    }

    public override string ToString()
    {
        // 如果有命名字段，显示命名格式
        if (HasNamedFields)
        {
            var parts = new List<string>();
            for (int i = 0; i < ItemValues.Count; i++)
            {
                // 查找该索引对应的字段名
                string? fieldName = _fieldNames?.FirstOrDefault(kvp => kvp.Value == i).Key;
                if (!string.IsNullOrEmpty(fieldName))
                {
                    parts.Add($"{fieldName}: {ItemValues[i]}");
                }
                else
                {
                    parts.Add(ItemValues[i].ToString() ?? "null");
                }
            }
            return $"({string.Join(", ", parts)})";
        }

        // 未命名元组
        return $"({string.Join(", ", ItemValues)})";
    }

    /// <summary>
    /// 明确标识元组类型，防止与列表混淆
    /// </summary>
    public override string TypeToString() => "Tuple";

    /// <summary>
    /// 获取值的实际.NET对象
    /// 尝试动态构建 ValueTuple，如果元素过多或复杂，返回 ITuple 或 object[] 可能更合适
    /// 但为了完全模拟 C# 互操作，我们尝试构建 ValueTuple
    /// </summary>
    public override object GetValue()
    {
        var values = ItemValues.Select(v => LangValueType.ValueToObj(v) ?? new object()).ToArray();
        return CreateValueTuple(values);
    }

    /// <summary>
    /// 静态辅助方法：动态创建 ValueTuple 实例
    /// </summary>
    public static object CreateValueTupleStatic(object[] values)
    {
        if (values.Length == 0) return new ValueTuple();
        return CreateValueTupleRecursiveStatic(values);
    }

    private static object CreateValueTupleRecursiveStatic(object[] values)
    {
        var count = values.Length;
        if (count > 7)
        {
            // 前7个元素
            var args = new object[8];
            Array.Copy(values, 0, args, 0, 7);
            
            // 第8个元素是剩余部分的元组
            var restValues = new object[count - 7];
            Array.Copy(values, 7, restValues, 0, count - 7);
            args[7] = CreateValueTupleRecursiveStatic(restValues);
            
            // 获取对应的泛型类型
            var types = args.Select(v => v.GetType()).ToArray();
            var tupleType = GetValueTupleTypeStatic(types);
            
            return Activator.CreateInstance(tupleType, args)!;
        }
        else
        {
            var types = values.Select(v => v.GetType()).ToArray();
            var tupleType = GetValueTupleTypeStatic(types);
            return Activator.CreateInstance(tupleType, values)!;
        }
    }

    private static Type GetValueTupleTypeStatic(Type[] types)
    {
        if (types.Length > 7)
        {
            var first7Types = types.Take(7).ToArray();
            var restTypes = types.Skip(7).ToArray();
            var restTupleType = GetValueTupleTypeStatic(restTypes);
            
            var allTypes = new Type[8];
            Array.Copy(first7Types, allTypes, 7);
            allTypes[7] = restTupleType;
            
            return typeof(ValueTuple<,,,,,,,>).MakeGenericType(allTypes);
        }
        
        return types.Length switch
        {
            1 => typeof(ValueTuple<>).MakeGenericType(types),
            2 => typeof(ValueTuple<,>).MakeGenericType(types),
            3 => typeof(ValueTuple<,,>).MakeGenericType(types),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(types),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(types),
            6 => typeof(ValueTuple<,,,,,>).MakeGenericType(types),
            7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(types),
            _ => throw new InvalidOperationException("不支持 0 元素或异常长度的元组类型生成")
        };
    }

    /// <summary>
    /// 动态创建 ValueTuple 实例
    /// </summary>
    private object CreateValueTuple(object[] values)
    {
        return CreateValueTupleStatic(values);
    }

    private object CreateValueTupleRecursive(object[] values)
    {
        return CreateValueTupleRecursiveStatic(values);
    }

    private Type GetValueTupleType(Type[] types)
    {
        return GetValueTupleTypeStatic(types);
    }

    /// <summary>
    /// 获取元组指定索引的元素
    /// </summary>
    public LangValueType Get(IntLangValue index)
    {
        return Get(index.Value);
    }

    public LangValueType Get(int index)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationError(this, $"元组索引越界: {index}，空元组不支持任何索引访问");
        }

        if (index < 0 || index >= ItemValues.Count)
        {
            throw new InvalidOperationError(this, $"元组索引越界: {index}，当前元组只支持索引 0 到 {ItemValues.Count - 1}");
        }

        return ItemValues[index];
    }

    /// <summary>
    /// 支持元组的点操作访问
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is LangId id)
        {
            // 1. 支持 ItemN 访问：tuple.Item1, tuple.Item2
            if (id.IdName.StartsWith("Item") && id.IdName.Length > 4)
            {
                var itemNumStr = id.IdName.Substring(4);
                if (int.TryParse(itemNumStr, out int itemNum) && itemNum >= 1)
                {
                    return Get(itemNum - 1);
                }
            }

            // 2. 支持命名字段访问
            if (_fieldNames is not null && _fieldNames.ContainsKey(id.IdName))
            {
                int fieldIndex = _fieldNames[id.IdName];
                return Get(fieldIndex);
            }

            // 3. 支持数字索引访问：tuple.0
            if (int.TryParse(id.IdName, out int index))
            {
                return Get(index);
            }

            // 4. ToStr
            if (id.IdName == "ToStr")
            {
                return new StringLangValue(ToString());
            }

            // 5. Length
            if (id.IdName == "Length")
            {
                return new IntLangValue(GetLength());
            }
        }

        if (dotExpression is Instance instance)
        {
             // 设置执行上下文
            Old8Lang.AST.Expression.ValueFunctions.ExecutionContext.SetCurrentManager(manager);

            if (instance.Ids.Count == 0)
            {
                if (instance.Id.IdName == "ToStr") return new StringLangValue(ToString());
                if (instance.Id.IdName == "Length") return new IntLangValue(GetLength());
            }
            
            return instance.FromClassToResult(this, manager);
        }

        throw new InvalidOperationError(this, $"不支持元组的点操作: {dotExpression}");
    }

    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is not TupleLangValue otherTuple)
            return false;

        if (ItemValues.Count != otherTuple.ItemValues.Count)
            return false;

        for (int i = 0; i < ItemValues.Count; i++)
        {
            if (!ItemValues[i].Equal(otherTuple.ItemValues[i]))
                return false;
        }

        return true;
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 获取所有元素的类型
        var types = Elements.Select(e => e.OutputType(local) ?? typeof(object)).ToArray();
        
        // 递归生成创建代码
        EmitValueTupleCreation(ilGenerator, local, Elements, types);
    }
    
    private void EmitValueTupleCreation(ILGenerator ilGenerator, LocalManager local, List<LangExpression> elements, Type[] types)
    {
        var count = elements.Count;
        if (count > 7)
        {
            // 构造 ValueTuple<T1..T7, TRest>
            var first7Types = types.Take(7).ToArray();
            var restTypes = types.Skip(7).ToArray();
            var restElements = elements.Skip(7).ToList();
            
            // 计算 Rest 的类型
            var restTupleType = GetValueTupleTypeForCompiler(restTypes);
            
            var allTypes = new Type[8];
            Array.Copy(first7Types, allTypes, 7);
            allTypes[7] = restTupleType;
            
            var tupleType = typeof(ValueTuple<,,,,,,,>).MakeGenericType(allTypes);
            var constructor = tupleType.GetConstructor(allTypes)!;
            
            // 加载前7个元素
            for (int i = 0; i < 7; i++)
            {
                elements[i].LoadIlValue(ilGenerator, local);
            }
            
            // 递归加载剩余元素作为第8个参数
            EmitValueTupleCreation(ilGenerator, local, restElements, restTypes);
            
            ilGenerator.Emit(OpCodes.Newobj, constructor);
        }
        else if (count == 0)
        {
             // 空元组
             // 正确的 IL 序列生成 default(ValueTuple)
             var tempLocal = ilGenerator.DeclareLocal(typeof(ValueTuple));
             ilGenerator.Emit(OpCodes.Ldloca, tempLocal);
             ilGenerator.Emit(OpCodes.Initobj, typeof(ValueTuple));
             ilGenerator.Emit(OpCodes.Ldloc, tempLocal);
        }
        else
        {
            var tupleType = GetValueTupleTypeForCompiler(types);
            var constructor = tupleType.GetConstructor(types)!;
            
            foreach (var element in elements)
            {
                element.LoadIlValue(ilGenerator, local);
            }
            
            ilGenerator.Emit(OpCodes.Newobj, constructor);
        }
    }
    
    private Type GetValueTupleTypeForCompiler(Type[] types)
    {
         if (types.Length > 7)
        {
            var first7Types = types.Take(7).ToArray();
            var restTypes = types.Skip(7).ToArray();
            var restTupleType = GetValueTupleTypeForCompiler(restTypes);
            
            var allTypes = new Type[8];
            Array.Copy(first7Types, allTypes, 7);
            allTypes[7] = restTupleType;
            
            return typeof(ValueTuple<,,,,,,,>).MakeGenericType(allTypes);
        }
        
        return types.Length switch
        {
            0 => typeof(ValueTuple),
            1 => typeof(ValueTuple<>).MakeGenericType(types),
            2 => typeof(ValueTuple<,>).MakeGenericType(types),
            3 => typeof(ValueTuple<,,>).MakeGenericType(types),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(types),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(types),
            6 => typeof(ValueTuple<,,,,,>).MakeGenericType(types),
            7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(types),
            _ => throw new InvalidOperationException("不支持异常长度的元组类型生成")
        };
    }

    public override Type OutputType(LocalManager local)
    {
        var types = Elements.Select(e => e.OutputType(local) ?? typeof(object)).ToArray();
        return GetValueTupleTypeForCompiler(types);
    }

    // 实现ILangList接口
    public IEnumerable<LangValueType> GetItems()
    {
        return ItemValues;
    }

    public int GetLength()
    {
        return ItemValues.Count;
    }

    public LangValueType Slice(int start, int end)
    {
        var items = ItemValues;
        
        if (start < 0) start += items.Count;
        if (end < 0) end += items.Count;
        
        start = Math.Max(0, Math.Min(start, items.Count));
        end = Math.Max(0, Math.Min(end, items.Count));
        
        if (start >= end)
        {
             return new TupleLangValue(new List<LangExpression>());
        }
        
        var sliceItems = items.Skip(start).Take(end - start).ToList();
        
        // 注意：这里我们返回的是一个新的 TupleLangValue，但我们需要用 LangExpression 来构造它
        // 实际上在解释器中，我们可以直接构造一个已经包含值的 TupleLangValue
        // 为了方便，我们需要一个支持直接传入值的构造函数或者方法
        
        // 既然我们不能直接传值（因为 AST 存的是 Expression），我们需要把 Value 包装成 Expression
        // 或者我们直接 new 一个 TupleLangValue，然后设置它的 ItemValues
        
        var newTuple = new TupleLangValue(sliceItems.Cast<LangExpression>().ToList());
        newTuple.ItemValues.AddRange(sliceItems); // 预填充值
        return newTuple;
    }

    public LangValueType Slice(int start, int end, int step)
    {
        var items = ItemValues;
        var length = items.Count;
        var result = new List<LangValueType>();

        if (step > 0)
        {
            if (start < 0) start += length;
            if (end < 0) end += length;
            start = Math.Max(0, Math.Min(start, length));
            end = Math.Max(0, Math.Min(end, length));
            for (int i = start; i < end; i += step) result.Add(items[i]);
        }
        else if (step < 0)
        {
            if (start < -1) start += length;
            if (end < -1) end += length;
            if (start >= length) start = length - 1;
            if (start < -1) start = -1;
            if (end >= length) end = length - 1;
            for (int i = start; i > end; i += step) result.Add(items[i]);
        }
        else
        {
            throw new InvalidOperationError(this, "切片步长不能为0");
        }

        var newTuple = new TupleLangValue(result.Cast<LangExpression>().ToList());
        newTuple.ItemValues.AddRange(result);
        return newTuple;
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
        if (index < 0 || index >= ItemValues.Count)
        {
            throw new InvalidOperationError(this, $"元组索引越界: {index}");
        }
        
        // 元组在 C# 中通常是不可变的（ValueTuple 的字段是可变的，但作为整体值类型...）
        // Old8Lang 的元组设计是否允许修改？
        // 原来的 Set 方法允许修改，并重建结构。
        // 现在扁平化了，直接修改即可。
        ItemValues[index] = value;
    }

    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        throw new InvalidOperationError(this, "元组不支持切片赋值操作");
    }

    public bool In(LangValueType value)
    {
        return ItemValues.Any(item => item.Equal(value));
    }
}

