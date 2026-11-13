using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.ContainsValue(value) - 检查字典是否包含指定的值
/// </summary>
public class DictContainsValueMethod : BaseInstanceMethod
{
    public override string[] Names => ["ContainsValue", "containsValue"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[] ParameterNames => ["value"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var value = parameters[0].Run(manager);

        var contains = dict.Value.Any(x => x.Value.Equal(value));
        return new BoolLangValue(contains);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(DictContainsValueMethod).GetMethod(nameof(ContainsValueHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool ContainsValueHelper(Dictionary<object, object> dict, object value)
    {
        return dict.ContainsValue(value);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict && arguments.Length > 0)
        {
            var value = arguments[0];
            return dict.ContainsValue(value!);
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
