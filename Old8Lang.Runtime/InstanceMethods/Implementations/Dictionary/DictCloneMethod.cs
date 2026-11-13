using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

/// <summary>
/// Dictionary.Clone() - 创建字典的独立副本
/// </summary>
public class DictCloneMethod : BaseInstanceMethod
{
    public override string[] Names => ["Clone", "clone"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var newDict = new DictionaryLangValue();

        foreach (var (key, value) in dict.Value)
        {
            newDict.Value.Add((key, value));
        }

        return newDict;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(DictCloneMethod).GetMethod(nameof(CloneHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static Dictionary<object, object> CloneHelper(Dictionary<object, object> dict)
    {
        return new Dictionary<object, object>(dict);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(Dictionary<object, object>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict)
        {
            return new Dictionary<object, object>(dict);
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
