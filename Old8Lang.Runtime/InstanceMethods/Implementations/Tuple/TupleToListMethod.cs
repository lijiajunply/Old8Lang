using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Tuple;

/// <summary>
/// Tuple.ToList() - 将元组转换为列表
/// </summary>
public class TupleToListMethod : BaseInstanceMethod
{
    public override string[] Names => ["ToList", "toList"];
    public override Type TargetType => typeof(TupleLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var tuple = (TupleLangValue)instance;

        // 创建一个新的列表，包含元组的所有元素
        var list = new ListLangValue(tuple.ItemValues.ToList(), null, position);

        return list;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(TupleToListMethod).GetMethod(nameof(ToListHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static ListLangValue ToListHelper(TupleLangValue tuple)
    {
        return new ListLangValue(tuple.ItemValues.ToList());
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not TupleLangValue tuple)
        {
            throw new ArgumentException("实例必须是 Tuple 类型");
        }

        return new ListLangValue(tuple.ItemValues.ToList());
    }
}
