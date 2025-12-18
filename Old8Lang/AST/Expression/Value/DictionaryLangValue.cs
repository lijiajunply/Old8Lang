using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using System.Reflection;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 字典 键对值
/// </summary>
public class DictionaryLangValue : LangValueType, ILangList
{
    private readonly List<TupleLangValue> Tuples;
    public readonly List<(LangValueType Key, LangValueType Value)> Value = [];

    public DictionaryLangValue(List<TupleLangValue> tuples, SourcePosition position = default) : base(position)
    {
        Tuples = tuples;
    }

    public DictionaryLangValue(SourcePosition position = default) : base(position)
    {
        Tuples = [];
    }

    public DictionaryLangValue(List<KeyValuePair<LangExpression, LangExpression>> list,
        SourcePosition position = default) :
        base(position)
    {
        Tuples = list.Select(x => new TupleLangValue(x.Key, x.Value)).ToList();
    }

    public override LangValueType Run(VariateManager manager)
    {
        // 清空之前的值，避免重复添加
        Value.Clear();

        foreach (var tuple in Tuples)
        {
            tuple.Run(manager);
            Value.Add(tuple.Value);
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
            // 特殊处理 Keys 和 Values 属性
            switch (langId.IdName)
            {
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
                    if (extensionType != null)
                    {
                        var method = extensionType.GetMethod(langId.IdName);
                        if (method != null)
                        {
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
            var extensionType = typeof(Old8Lang.AST.Expression.ValueFunctions.DictionaryValueFuncStatic);
            if (extensionType != null)
            {
                var method = extensionType.GetMethod(methodName);
                if (method != null)
                {
                    // 对于 Merge 和 Update 方法，需要特殊处理参数
                    if (methodName == "Merge" || methodName == "Update")
                    {
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
                    return a.FromClassToResult(this);
                }
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

        var typeAny = typeTemplate.CreateInstance(manager);

        foreach (var a in Value)
        {
            var key = a.Key.Run(manager);
            var value = a.Value.Run(manager);
            if (key is not StringLangValue s) continue;
            typeAny.Set(new LangId(s.Value), value);
        }

        return typeAny;
    }

    public IEnumerable<LangValueType> GetItems()
        => Value.Select(x => new TupleLangValue(x.Key, x.Value));

    public int GetLength() => Value.Count;

    public LangValueType Slice(int start, int end)
    {
        throw new InvalidOperationError(this, "字典类型不支持切片操作");
    }


    public override Type OutputType(LocalManager local) => typeof(Dictionary<object, object>);

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        var listConstructor = typeof(Dictionary<object, object>).GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor); // 创建 List<int> 实例

        var l = ilGenerator.DeclareLocal(typeof(Dictionary<object, object>));
        ilGenerator.Emit(OpCodes.Stloc, l.LocalIndex);

        // 向 List<int> 中添加元素
        var addMethod = typeof(Dictionary<object, object>).GetMethod("Add")!;
        foreach (var expr in Tuples)
        {
            ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
            expr.V1.LoadIlValue(ilGenerator, local);
            var t = expr.V1.OutputType(local);
            ilGenerator.Emit(OpCodes.Box, t!);
            expr.V2.LoadIlValue(ilGenerator, local);
            t = expr.V2.OutputType(local);
            ilGenerator.Emit(OpCodes.Box, t!);
            ilGenerator.Emit(OpCodes.Callvirt, addMethod); // 调用 Add 方法
        }

        ilGenerator.Emit(OpCodes.Ldloc, l.LocalIndex);
    }
}