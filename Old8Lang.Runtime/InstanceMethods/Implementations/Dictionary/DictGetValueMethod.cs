using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.GetValue(key) - 根据键获取字典中的值
/// </summary>
public class DictGetValueMethod : BaseInstanceMethod
{
    public override string[] Names => ["GetValue", "getValue"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["key"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var key = parameters[0].Run(manager);

        foreach (var (k, v) in dict.Value)
        {
            if (k.Equal(key))
            {
                return v;
            }
        }

        throw new KeyNotFoundException($"字典中不存在键: {key.ToDisplayString()}");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(DictGetValueMethod).GetMethod(nameof(GetValueHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static object? GetValueHelper(Dictionary<object, object> dict, object key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }
        throw new KeyNotFoundException($"字典中不存在键: {key}");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 0)
        {
            var key = arguments[0];
            if (dict.TryGetValue(key!, out var value))
            {
                return value;
            }
            throw new KeyNotFoundException($"字典中不存在键: {key}");
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
