using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.Merge(otherDictionary) - 将另一个字典合并到当前字典中，如果有重复键，当前字典的值优先
/// </summary>
public class DictMergeMethod : BaseInstanceMethod
{
    public override string[] Names => ["Merge", "merge"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["otherDictionary"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var otherDict = parameters[0].Run(manager) as DictionaryLangValue;

        if (otherDict == null)
        {
            throw new ArgumentException("参数必须是 Dictionary 类型");
        }

        // 创建一个新字典
        var newDict = new DictionaryLangValue();

        // 复制当前字典的所有键值对
        foreach (var (key, value) in dict.Value)
        {
            newDict.Value.Add((key, value));
        }

        // 添加另一个字典的键值对，跳过重复的键
        foreach (var (key, value) in otherDict.Value)
        {
            var keyExists = false;
            foreach (var (existingKey, _) in newDict.Value)
            {
                if (existingKey.Equal(key))
                {
                    keyExists = true;
                    break;
                }
            }

            if (!keyExists)
            {
                newDict.Value.Add((key, value));
            }
        }

        return newDict;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(DictMergeMethod).GetMethod(nameof(MergeHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static Dictionary<object, object> MergeHelper(Dictionary<object, object> dict, Dictionary<object, object> otherDict)
    {
        var newDict = new Dictionary<object, object>(dict);

        foreach (var (key, value) in otherDict)
        {
            if (!newDict.ContainsKey(key))
            {
                newDict[key] = value;
            }
        }

        return newDict;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(Dictionary<object, object>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 0)
        {
            if (arguments[0] is Dictionary<object, object> otherDict)
            {
                var newDict = new Dictionary<object, object>(dict);

                foreach (var (key, value) in otherDict)
                {
                    if (!newDict.ContainsKey(key))
                    {
                        newDict[key] = value;
                    }
                }

                return newDict;
            }
            throw new ArgumentException("参数必须是 Dictionary 类型");
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
