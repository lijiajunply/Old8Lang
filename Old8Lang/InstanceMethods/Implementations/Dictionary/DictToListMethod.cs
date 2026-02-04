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
/// Dictionary.ToList() - 将字典转换为列表，每个元素是包含键值对的元组
/// </summary>
public class DictToListMethod : BaseInstanceMethod
{
    public override string[] Names => ["ToList", "toList"];
    public override Type TargetType => typeof(DictionaryLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var dict = (DictionaryLangValue)instance;
        var list = new List<LangValueType>();

        foreach (var (key, value) in dict.Value)
        {
            var tuple = new TupleLangValue(key, value);
            tuple.ItemValues.Add(key);
            tuple.ItemValues.Add(value);
            list.Add(tuple);
        }

        return new ListLangValue(list);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(DictToListMethod).GetMethod(nameof(ToListHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static object[] ToListHelper(Dictionary<object, object> dict)
    {
        var list = new List<object>();

        foreach (var (key, value) in dict)
        {
            list.Add(new object[] { key, value });
        }

        return list.ToArray();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object[]);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is Dictionary<object, object> dict)
        {
            var list = new List<object>();

            foreach (var (key, value) in dict)
            {
                list.Add(new object[] { key, value });
            }

            return list.ToArray();
        }
        throw new ArgumentException("实例必须是 Dictionary 类型");
    }
}
