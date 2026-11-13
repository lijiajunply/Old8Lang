using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.GetOrElse(key, defaultValue) - 根据键获取字典中的值，如果键不存在则返回默认值
/// </summary>
public class DictGetOrElseMethod : BaseInstanceMethod
{
    public override string[] Names => ["GetOrElse", "getOrElse", "TryGet", "tryGet"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["key", "defaultValue"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var key = parameters[0].Run(manager);
        var defaultValue = parameters[1].Run(manager);

        foreach (var (k, v) in dict.Value)
        {
            if (k.Equal(key))
            {
                return v;
            }
        }

        return defaultValue;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);
        parameters[1].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(DictGetOrElseMethod).GetMethod(nameof(GetOrElseHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static object? GetOrElseHelper(Dictionary<object, object> dict, object key, object defaultValue)
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 1)
        {
            var key = arguments[0];
            var defaultValue = arguments[1];
            return dict.TryGetValue(key!, out var value) ? value : defaultValue;
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
