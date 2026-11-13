using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.Update(otherDictionary) 或 Dictionary.Update(key, value) - 更新字典的键值对
/// </summary>
public class DictUpdateMethod : BaseInstanceMethod
{
    public override string[] Names => ["Update", "update"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[]? ParameterNames => null; // 支持两种参数形式
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;

        if (parameters.Count == 1)
        {
            // Update(otherDictionary) 形式
            var otherDict = parameters[0].Run(manager) as DictionaryLangValue;
            if (otherDict == null)
            {
                throw new ArgumentException("参数必须是 Dictionary 类型");
            }

            foreach (var (key, value) in otherDict.Value)
            {
                // 查找并更新现有的键值对
                var keyFound = false;
                for (int i = 0; i < dict.Value.Count; i++)
                {
                    var (existingKey, _) = dict.Value[i];
                    if (existingKey.Equal(key))
                    {
                        dict.Value[i] = (existingKey, value);
                        keyFound = true;
                        break;
                    }
                }

                // 如果键不存在，添加新的键值对
                if (!keyFound)
                {
                    dict.Value.Add((key, value));
                }
            }
        }
        else
        {
            // Update(key, value) 形式
            var key = parameters[0].Run(manager);
            var value = parameters[1].Run(manager);

            // 查找并更新现有的键值对
            var keyFound = false;
            for (int i = 0; i < dict.Value.Count; i++)
            {
                var (existingKey, _) = dict.Value[i];
                if (existingKey.Equal(key))
                {
                    dict.Value[i] = (existingKey, value);
                    keyFound = true;
                    break;
                }
            }

            // 如果键不存在，添加新的键值对
            if (!keyFound)
            {
                dict.Value.Add((key, value));
            }
        }

        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        if (parameters.Count == 1)
        {
            parameters[0].LoadIlValue(ilGenerator, local);
            var helperMethod = typeof(DictUpdateMethod).GetMethod(nameof(UpdateDictHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            parameters[0].LoadIlValue(ilGenerator, local);
            parameters[1].LoadIlValue(ilGenerator, local);
            var helperMethod = typeof(DictUpdateMethod).GetMethod(nameof(UpdateKeyValueHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    public static void UpdateDictHelper(Dictionary<object, object> dict, Dictionary<object, object> otherDict)
    {
        foreach (var (key, value) in otherDict)
        {
            dict[key] = value;
        }
    }

    public static void UpdateKeyValueHelper(Dictionary<object, object> dict, object key, object value)
    {
        dict[key] = value;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict)
        {
            if (arguments.Length == 1 && arguments[0] is Dictionary<object, object> otherDict)
            {
                // Update(otherDictionary) 形式
                foreach (var (key, value) in otherDict)
                {
                    dict[key] = value;
                }
            }
            else if (arguments.Length == 2)
            {
                // Update(key, value) 形式
                var key = arguments[0];
                var value = arguments[1];
                dict[key!] = value!;
            }
            else
            {
                throw new ArgumentException("参数数量或类型不正确");
            }

            return null;
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
