using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

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
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperMethod = typeof(DictToListMethod).GetMethod(nameof(ToListHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：把桥接后的字典转成键值对列表（每个元素是 <c>[key, value]</c>）。
    /// </summary>
    public static List<object?> ToListHelper(LangValueType instance)
    {
        if (instance is not DictionaryLangValue dict)
        {
            throw new ArgumentException($"实例必须是字典类型，实际是 {instance.GetType().Name}");
        }

        if (dict.Value.Count == 0 && dict.Tuples.Count > 0)
        {
            dict.Accept(RuntimeVisitors.CreateInterpreter(new VariateManager()));
        }

        var result = new List<object?>();
        foreach (var (key, value) in dict.Value)
        {
            result.Add(new object?[] { IlValueBridge.Unwrap(key), IlValueBridge.Unwrap(value) });
        }

        return result;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        // 解释器实现返回 ListLangValue（元素是 (key, value) 元组）；
        // IL 桥接按该声明映射为原生 List<object?>，与解释器表示保持一致。
        return typeof(List<object?>);
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
