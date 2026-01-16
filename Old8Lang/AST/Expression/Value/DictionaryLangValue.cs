using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

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

    public DictionaryLangValue(List<TupleLangValue> tuples, string? keyType = null, string? valueType = null, SourcePosition position = default) : base(position)
    {
        Tuples = tuples;
        KeyType = keyType;
        ValueType = valueType;
    }

    public DictionaryLangValue(string? keyType = null, string? valueType = null, SourcePosition position = default) : base(position)
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

    public override LangValueType Run(VariateManager manager)
    {
        // 清空之前的值，避免重复添加
        Value.Clear();

        foreach (var tuple in Tuples)
        {
            tuple.Run(manager);
            Value.Add((tuple.Get(0), tuple.Get(1)));
        }

        return this;
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
            var result = dotExpression.Run(manager);
            return Get(result);
        }

        // 处理属性访问：obj.property
        if (dotExpression is LangId langId)
        {
            // 特殊处理 Count, Keys 和 Values 属性
            switch (langId.IdName)
            {
                case "Count":
                    // 返回字典的键值对数量
                    return new IntLangValue(Value.Count);
                case "Keys":
                    // 返回字典的键集合
                    return new ListLangValue(Value.Select(x => x.Key).ToList());
                case "Values":
                    // 返回字典的值集合
                    return new ListLangValue(Value.Select(x => x.Value).ToList());
                default:
                    // 首先检查是否是扩展方法调用
                    var extensionType =
                        Type.GetType("Old8Lang.AST.Expression.ValueFunctions.DictionaryValueFuncStatic");
                    if (extensionType is not null)
                    {
                        var method = extensionType.GetMethod(langId.IdName);
                        if (method is not null)
                        {
                            // 设置执行上下文，以便扩展方法可以访问当前的 VariateManager
                            ValueFunctions.ExecutionContext.SetCurrentManager(manager);

                            // 找到扩展方法，创建 Instance 来处理方法调用
                            var instance = new Instance(new LangId(langId.IdName), []);
                            return instance.FromClassToResult(this);
                        }
                    }

                    // 如果不是扩展方法，将属性名作为字符串键来访问字典值
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

            // 检查是否是已知的方法调用（如 ContainsKey, GetOrElse 等）
            var extensionType = typeof(ValueFunctions.DictionaryValueFuncStatic);

            MethodInfo? method;
            // 对于 Update 方法，根据参数数量选择正确的重载
            if (methodName == "Update")
            {
                if (a.Ids.Count == 1)
                {
                    // Update(otherDictionary) - 一个参数
                    method = extensionType.GetMethod(methodName, [typeof(DictionaryLangValue)]);
                }
                else if (a.Ids.Count == 2)
                {
                    // Update(key, value) - 两个参数
                    method = extensionType.GetMethod(methodName,
                        new[] { typeof(StringLangValue), typeof(LangValueType) });
                }
                else
                {
                    method = null;
                }
            }
            else
            {
                // 对于其他方法，使用普通的查找
                method = extensionType.GetMethod(methodName);
            }

            if (method is not null)
            {
                // 对于 Merge 和 Update 方法，需要特殊处理参数
                if (methodName == "Merge" || methodName == "Update")
                {
                    // 设置执行上下文，以便扩展方法可以访问当前的 VariateManager
                    ValueFunctions.ExecutionContext.SetCurrentManager(manager);

                    // 手动处理参数，确保使用正确的 manager
                    var parameters = method.GetParameters();
                    var args = new List<object>();

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i == 0) // 第一个参数是 this (当前字典对象)
                        {
                            args.Add(this);
                        }
                        else
                        {
                            // 运行参数表达式，使用正确的 manager
                            var argValue = a.Ids[i - 1].Run(manager);
                            args.Add(argValue);
                        }
                    }

                    return (LangValueType)method.Invoke(null, args.ToArray())!;
                }

                // 对于其他方法，调用 FromClassToResult 来处理方法调用
                // 设置执行上下文，以便扩展方法可以访问当前的 VariateManager
                ValueFunctions.ExecutionContext.SetCurrentManager(manager);
                return a.FromClassToResult(this);
            }


            // 只有在特定情况下才当作索引访问：方法名不是已知方法且只有一个参数
            if (a.Ids is { Count: 1 } && methodName != "Get" && methodName != "ContainsKey" &&
                methodName != "GetOrElse" && methodName != "Merge" && methodName != "Update" &&
                methodName != "Keys" && methodName != "Values")
            {
                // 运行索引表达式获取键值
                var result = a.Ids[0].Run(manager);
                return Get(result);
            }

            // 默认情况下，调用 FromClassToResult
            return a.FromClassToResult(this);
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

    public LangValueType Get(LangValueType key)
    {
        var a = Value.Where(x => x.Key.Equal(key)).ToList();
        if (a.Count == 0)
        {
            return NullLangValue.Instance;
        }

        return a[0].Value;
    }

    public void Set(LangValueType key, LangValueType value)
    {
        var b = Value.FindLastIndex(x => key.Equal(x.Key));
        if (b >= 0)
        {
            // 更新现有键值对
            Value[b] = (key, value);
        }
        else
        {
            // 添加新键值对
            Value.Add((key, value));
        }
    }

    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        throw new InvalidOperationError(this, "字典类型不支持切片赋值操作");
    }

    public bool In(LangValueType value)
    {
        // 检查键是否存在（与编译模式的ContainsKey行为一致）
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

        var typeAny = typeTemplate.CreateInstanceV2(manager);
        typeAny.Init(manager.Interpreter);

        foreach (var a in Value)
        {
            var key = a.Key.Run(manager);
            var value = a.Value.Run(manager);
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


    public override Type OutputType(LocalManager local) => typeof(Dictionary<object, object>);

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        var listConstructor = typeof(Dictionary<object, object>).GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor); // 创建 List<int> 实例

        var l = ilGenerator.DeclareLocal(typeof(Dictionary<object, object>));
        ilGenerator.Emit(OpCodes.Stloc, l);

        // 向 Dictionary<object, object> 中添加元素
        var addMethod = typeof(Dictionary<object, object>).GetMethod("Add")!;
        foreach (var expr in Tuples)
        {
            ilGenerator.Emit(OpCodes.Ldloc, l);
            expr.Elements[0].LoadIlValue(ilGenerator, local);
            var keyType = expr.Elements[0].OutputType(local);
            // 只有值类型才需要装箱
            if (keyType!.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, keyType);
            }
            expr.Elements[1].LoadIlValue(ilGenerator, local);
            var valueType = expr.Elements[1].OutputType(local);
            // 只有值类型才需要装箱
            if (valueType!.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, valueType);
            }
            ilGenerator.Emit(OpCodes.Callvirt, addMethod); // 调用 Add 方法
        }

        ilGenerator.Emit(OpCodes.Ldloc, l);
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
            var objKey = LangValueType.ValueToObj(key) ?? new object();
            var objValue = LangValueType.ValueToObj(value) ?? new object();
            result[objKey] = objValue;
        }

        return result;
    }
}