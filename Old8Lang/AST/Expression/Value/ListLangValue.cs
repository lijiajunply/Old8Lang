using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;


namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 列表
/// </summary>
public class ListLangValue : LangValueType, ILangList
{
    private readonly List<LangExpression> Value;

    public readonly List<LangValueType> Values = [];


    public ListLangValue(List<LangExpression> value, SourcePosition position = default) : base(position)
    {
        Value = value;
    }

    public ListLangValue(List<object> value, SourcePosition position = default) : base(position)
    {
        Values.AddRange(value.Select(ObjToValue));
        Value = [];
    }

    public ListLangValue(List<LangValueType> value, SourcePosition position = default) : base(position)
    {
        Values.AddRange(value);
        Value = [];
    }

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        if (Values.Count > 0) return this;
        foreach (var expr in Value)
            Values.Add(expr.Run(manager));
        return this;
    }

    public LangValueType Get(IntLangValue i)
    {
        int idx = i.Value;
        if (idx < 0)
            idx = Values.Count + idx;
        return Values[idx];
    }

    public void Set(LangValueType index, LangValueType value)
    {
        if (index is IntLangValue i)
        {
            int idx = i.Value;
            if (idx < 0)
                idx = Values.Count + idx;
            if (idx < 0 || idx >= Values.Count)
                throw new IndexError(this, idx, Values.Count);

            // 类型检查和转换：确保添加的元素类型与列表中已有的元素类型一致
            // 如果类型不一致，尝试进行类型转换
            var convertedValue = value;
            if (Values.Count > 0)
            {
                var existingType = Values[0].TypeToString().ToLower();
                var newValueType = value.TypeToString().ToLower();

                if (existingType != newValueType)
                {
                    // 尝试进行类型转换
                    try
                    {
                        // 创建类型值用于转换
                        var targetType = new TypeLangValue(existingType);
                        // 调用 Converse 方法进行类型转换
                        convertedValue = value.Converse(targetType, new LangParser.VariateManager());
                    }
                    catch (Exception e)
                    {
                        // 如果转换失败，抛出类型不匹配错误
                        throw new TypeError(this, existingType, newValueType,
                            $"列表元素类型必须一致，无法将 {newValueType} 转换为 {existingType}: {e.Message}");
                    }
                }
            }

            Values[idx] = convertedValue;
        }
        else
        {
            throw new TypeError(this, "IntValue", index.GetType().Name);
        }
    }

    public override string ToString() =>
        "[" + string.Join(", ", Values) + "]"; // Old8Lang 风格的列表，使用 [ ] 包裹

    public override LangValueType Dot(LangExpression dotExpression)
    {
        // 先检查是否是索引访问
        if (dotExpression is IntLangValue intValue)
        {
            return Get(intValue);
        }

        // 如果是其他类型的表达式，尝试将其作为索引
        if (dotExpression is not Instance)
        {
            var tempManager = new LangParser.VariateManager();
            var result = dotExpression.Run(tempManager);

            if (result is IntLangValue idx)
            {
                return Get(idx);
            }
        }

        // 如果是 Instance，则作为方法调用
        if (dotExpression is Instance a)
        {
            return a.FromClassToResult(this);
        }

        throw new InvalidOperationError(this, "列表类型只支持索引访问或实例方法调用");
    }

    // 覆盖 Equal 方法以支持列表深度比较
    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is not ListLangValue otherList)
            return false;

        // 比较长度
        if (Values.Count != otherList.Values.Count)
            return false;

        // 逐个比较元素
        for (int i = 0; i < Values.Count; i++)
        {
            if (!Values[i].Equal(otherList.Values[i]))
                return false;
        }

        return true;
    }

    public override object GetValue() => Apis.ListToObjects(Values);
    public IEnumerable<LangValueType> GetItems() => Values;

    public int GetLength() => Values.Count;

    public LangValueType Slice(int start, int end)
    {
        if (start < 0) start += Values.Count;
        if (end < 0) end += Values.Count + 1;
        // 使用接受 List<LangValueType> 的构造函数，因为 Values 已经包含了运行后的值
        return new ListLangValue(Values[start..end], Position);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 确定列表元素的类型
        var itemType = typeof(object);
        if (Value.Count > 0)
        {
            itemType = Value[0].OutputType(local);
            // 检查所有元素是否为同一类型
            if (Value.Select(expr => expr.OutputType(local)).Any(exprType => exprType != itemType))
            {
                itemType = typeof(object);
            }
        }
        else if (Values.Count > 0)
        {
            itemType = Values[0].OutputType(local);
            // 检查所有元素是否为同一类型
            if (Values.Select(value => value.OutputType(local)).Any(valueType => valueType != itemType))
            {
                itemType = typeof(object);
            }
        }

        var listType = typeof(List<>).MakeGenericType(itemType ?? typeof(object));
        // 创建泛型List实例
        var listConstructor = listType.GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor);

        // 如果没有元素需要添加，直接返回List实例
        if (Value.Count == 0)
        {
            return;
        }

        // 否则，将List实例存储到局部变量
        var l = ilGenerator.DeclareLocal(listType);
        ilGenerator.Emit(OpCodes.Stloc, l.LocalIndex);

        // 向List中添加元素
        var addMethod = listType.GetMethod("Add", [itemType ?? typeof(object)])!;
        foreach (var expr in Value)
        {
            ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
            expr.LoadIlValue(ilGenerator, local);
            var t = expr.OutputType(local);
            if (t != itemType)
            {
                ilGenerator.Emit(OpCodes.Box, t!);
            }

            ilGenerator.Emit(OpCodes.Callvirt, addMethod); // 调用Add方法
        }

        // 将填充好的List实例加载到堆栈
        ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
    }

    public override Type OutputType(LocalManager local)
    {
        // 确定列表元素的类型
        var itemType = typeof(object);
        if (Value.Count > 0)
        {
            itemType = Value[0].OutputType(local);
            // 检查所有元素是否为同一类型
            if (Value.Select(expr => expr.OutputType(local)).Any(exprType => exprType != itemType))
            {
                itemType = typeof(object);
            }
        }
        else if (Values.Count > 0)
        {
            itemType = Values[0].OutputType(local);
            // 检查所有元素是否为同一类型
            if (Values.Select(value => value.OutputType(local)).Any(valueType => valueType != itemType))
            {
                itemType = typeof(object);
            }
        }

        // 返回泛型List类型
        return typeof(List<>).MakeGenericType(itemType ?? typeof(object));
    }

    public override LangValueType Converse(LangValueType otherLangValueType, LangParser.VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue value)
            throw new TypeError(this, "TypeValue", otherLangValueType.GetType().Name);

        switch (value.Value)
        {
            case "List" or "list":
                // 已经是列表，直接返回
                return this;
            case "Array" or "array":
                // 列表转换为数组
                return new ArrayLangValue(Values, Position);
            case "String" or "string":
                // 列表转换为字符串，用逗号分隔
                return new StringLangValue(string.Join(", ", Values.Select(v => v.ToDisplayString())));
            default:
                throw new TypeError(this, $"不支持的类型转换: {GetType().Name} 到 {value.Value}");
        }
    }
}