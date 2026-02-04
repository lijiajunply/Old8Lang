using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Generic;

/// <summary>
/// ILangList.Concat(other) - 连接两个列表
/// 适用于所有实现 ILangList 接口的类型
/// </summary>
public class LangListConcatMethod : BaseLangListMethod
{
    public override string[] Names => ["Concat", "concat"];
    public override string[] ParameterNames => ["other"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var items = GetItems(instance);
        var otherValue = parameters[0].Run(manager);

        if (otherValue is not ILangList otherList)
        {
            throw new ArgumentException("Concat 方法的参数必须实现 ILangList 接口");
        }

        var otherItems = otherList.GetItems();
        var concatenated = items.Concat(otherItems).ToList();

        return new ListLangValue(concatenated, null, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(LangListConcatMethod).GetMethod(nameof(ConcatHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue ConcatHelper(ILangList langList, ILangList other)
    {
        var items = langList.GetItems().Concat(other.GetItems()).ToList();
        return new ListLangValue(items);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is ILangList langList && arguments[0] is ILangList other)
        {
            var items = langList.GetItems().Concat(other.GetItems()).ToList();
            return new ListLangValue(items);
        }

        if (instance is List<object?> list && arguments[0] is List<object?> otherList)
        {
            return list.Concat(otherList).ToList();
        }

        throw new ArgumentException($"实例和参数必须实现 ILangList 接口或为 List<object?> 类型");
    }
}
