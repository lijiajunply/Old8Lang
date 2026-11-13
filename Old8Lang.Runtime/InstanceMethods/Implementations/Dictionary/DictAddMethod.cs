using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.Add(key, value) - 向字典中添加键值对
/// </summary>
public class DictAddMethod : BaseInstanceMethod
{
    public override string[] Names => ["Add", "add"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["key", "value"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var key = parameters[0].Run(manager);
        var value = parameters[1].Run(manager);

        dict.Value.Add((key, value));
        return new TupleLangValue(key, value);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);
        parameters[1].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(DictAddMethod).GetMethod(nameof(AddHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static object?[] AddHelper(Dictionary<object, object> dict, object key, object value)
    {
        dict[key] = value;
        return [key, value];
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object[]);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 1)
        {
            var key = arguments[0];
            var value = arguments[1];
            dict[key!] = value!;
            return new[] { key, value };
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
