using System.Text;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 字典 键对值
/// </summary>
public partial class DictionaryLangValue : LangValueType, ILangList
{
    /// <summary>
    /// 字典的元组列表（键值对）
    /// </summary>
    public List<TupleLangValue> Tuples { get; }

    public readonly List<(LangValueType Key, LangValueType Value)> Value = [];

    /// <summary>
    /// 键类型（泛型参数），null 表示非泛型或未推断
    /// </summary>
    public string? KeyType { get; set; }

    /// <summary>
    /// 值类型（泛型参数），null 表示非泛型或未推断
    /// </summary>
    public string? ValueType { get; set; }

    public DictionaryLangValue()
    {
        Tuples = [];
    }

    public DictionaryLangValue(List<TupleLangValue> tuples, string? keyType = null, string? valueType = null,
        SourcePosition position = default) : base(position)
    {
        Tuples = tuples;
        KeyType = keyType;
        ValueType = valueType;
    }

    public DictionaryLangValue(string? keyType = null, string? valueType = null, SourcePosition position = default) :
        base(position)
    {
        Tuples = [];
        KeyType = keyType;
        ValueType = valueType;
    }

    public DictionaryLangValue(List<KeyValuePair<LangExpression, LangExpression>> list,
        string? keyType = null,
        string? valueType = null,
        SourcePosition position = default) :
        base(position)
    {
        Tuples = list.Select(x => new TupleLangValue(x.Key, x.Value)).ToList();
        KeyType = keyType;
        ValueType = valueType;
    }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        // 优先检查是否是字符串键的索引访问
        if (dotExpression is StringLangValue stringKey)
        {
            return Get(stringKey);
        }

        // 检查是否是其他类型的键访问（整数、对象等）
        if (dotExpression is not Instance and not LangId)
        {
            // 尝试运行表达式获取键值
            var result = dotExpression.Accept(RuntimeVisitors.CreateInterpreter(manager));
            return Get(result);
        }

        // 处理属性访问：obj.property
        if (dotExpression is LangId langId)
        {
            // 特殊处理 Count, Keys 和 Values 属性
            switch (langId.IdName)
            {
                case "Count":
                case "Length":
                    // 返回字典的键值对数量（Length 与 Count 等价；键 "Length" 请用 d["Length"]）
                    return new IntLangValue(Value.Count);
                case "Keys":
                    // 返回字典的键集合
                    return new ListLangValue(Value.Select(x => x.Key).ToList());
                case "Values":
                    // 返回字典的值集合
                    return new ListLangValue(Value.Select(x => x.Value).ToList());
                default:
                    // 将属性名作为字符串键来访问字典值
                    var key = new StringLangValue(langId.IdName);
                    return Get(key);
            }
        }

        // 处理方法调用
        if (dotExpression is Instance a)
        {
            // 检查是否是 Keys 或 Values 属性调用（通过方法名）
            var methodName = a.Id.IdName;
            if (methodName is "Keys" or "Values")
            {
                // 直接返回对应的集合，不调用 FromClassToResult
                return methodName == "Keys"
                    ? new ListLangValue(Value.Select(x => x.Key).ToList())
                    : new ListLangValue(Value.Select(x => x.Value).ToList());
            }

            // 只有在特定情况下才当作索引访问：方法名不是已知方法且只有一个参数
            // 先检查 InstanceMethodRegistry 中是否有这个方法，如果有就调用方法而不是索引访问
            if (a.Ids is { Count: 1 })
            {
                InstanceMethodInitializer.EnsureInitialized();
                var registeredMethod = InstanceMethodRegistry.Instance.ResolveMethod(
                    typeof(DictionaryLangValue), methodName, a.Ids, null);
                if (registeredMethod == null)
                {
                    // 没有注册的方法，当作索引访问
                    var result = a.Ids[0].Accept(RuntimeVisitors.CreateInterpreter(manager));
                    return Get(result);
                }
            }

            // 默认情况下，调用 FromClassToResult（使用新的 InstanceMethods 系统）
            return a.FromClassToResult(this, manager);
        }

        throw new InvalidOperationError(this, "字典类型只支持实例调用操作、属性访问或键索引访问");
    }

    // 覆盖 Equal 方法以支持字典深度比较
    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is not DictionaryLangValue otherDict)
            return false;

        // 比较大小
        if (Value.Count != otherDict.Value.Count)
            return false;

        // 检查所有键值对是否相同
        foreach (var (key, value) in Value)
        {
            // 查找相同的键
            var foundMatch = false;
            foreach (var otherPair in otherDict.Value.Where(otherPair => otherPair.Key.Equal(key)))
            {
                // 找到相同的键，比较值是否相等
                if (!value.Equal(otherPair.Value))
                    return false;
                foundMatch = true;
                break;
            }

            // 如果找不到相同的键，返回 false
            if (!foundMatch)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 按键取值；键不存在时抛 <see cref="Old8Lang.Error.KeyError"/>（KEY_ERROR）。
    /// </summary>
    /// <remarks>
    /// 2026-11-15 统一：此前解释器返回 null、IL 抛 CLR KeyNotFoundException、VM 抛 KEY_ERROR。
    /// 语义上缺失键属于错误（安全访问用 <c>GetOrElse</c> / <c>TryGet</c>），现在三模式一致。
    /// </remarks>
    public LangValueType Get(LangValueType key)
    {
        foreach (var (k, value) in Value)
        {
            if (k.Equal(key))
            {
                return value;
            }
        }

        throw new KeyError(this, key.ToDisplayString());
    }

    public void Set(LangValueType key, LangValueType value)
    {
        var b = Value.FindLastIndex(x => key.Equal(x.Key));
        if (b >= 0)
        {
            // 更新现有键值对
            Value[b] = (key, value);
            if (b < Tuples.Count)
            {
                Tuples[b] = new TupleLangValue(key, value, Position);
            }
        }
        else
        {
            // 添加新键值对
            Value.Add((key, value));
            Tuples.Add(new TupleLangValue(key, value, Position));
        }
    }

    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        throw new InvalidOperationError(this, "字典类型不支持切片赋值操作");
    }

    public bool In(LangValueType value)
    {
        // 检查键是否存在（与 IL 模式的ContainsKey行为一致）
        return Value.Any(x => x.Key.Equal(value));
    }

    public override string ToString()
    {
        if (Value.Count == 0)
        {
            return "{" + string.Join(", ", Tuples) + "}";
        }

        var sb = new StringBuilder();
        for (var i = 0; i < Value.Count; i++)
        {
            var valueTuple = Value[i];
            sb.Append($"{valueTuple.Key}: {valueTuple.Value}");
            if (i < Value.Count - 1)
            {
                sb.Append(", ");
            }
        }

        return "{" + sb + "}"; // Old8Lang 风格的字典，使用 { } 包裹，键值对用 : 分隔
    }

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue type)
            throw new TypeError(this, "Type", otherLangValueType.GetType().Name);
        var info = manager.GetAny(new LangId(type.Value ?? ""));
        if (info is not TypeTemplate typeTemplate)
        {
            throw new TypeError(this, "Type", otherLangValueType.GetType().Name);
        }

        var typeAny = typeTemplate.CreateInstance(manager);
        typeAny.Init(manager.Interpreter);

        foreach (var a in Value)
        {
            var key = a.Key.Accept(RuntimeVisitors.CreateInterpreter(manager));
            var value = a.Value.Accept(RuntimeVisitors.CreateInterpreter(manager));
            if (key is not StringLangValue s) continue;
            typeAny.SetField(s.Value, value, manager);
        }

        return typeAny;
    }

    public IEnumerable<LangValueType> GetItems()
        => Value.Select(x => new TupleLangValue(x.Key, x.Value));

    public int GetLength() => Value.Count;

    public LangValueType Slice(int start, int end, int step)
    {
        throw new InvalidOperationError(this, "字典类型不支持切片操作");
    }

    /// <summary>
    /// 获取值的实际.NET对象
    /// </summary>
    /// <returns>Dictionary&lt;object, object&gt; 对象</returns>
    public override object GetValue()
    {
        var result = new Dictionary<object, object>();

        // 使用 Value 字段（已运行的键值对）
        foreach (var (key, value) in Value)
        {
            var objKey = ValueToObj(key) ?? new object();
            var objValue = ValueToObj(value) ?? new object();
            result[objKey] = objValue;
        }

        return result;
    }
}
