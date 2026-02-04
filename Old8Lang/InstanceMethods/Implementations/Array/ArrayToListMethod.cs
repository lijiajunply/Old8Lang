using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.ToList() - 将数组转换为列表
/// </summary>
public class ArrayToListMethod : BaseInstanceMethod
{
    public override string[] Names => ["ToList", "toList", "tolist"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        return new ListLangValue(array.RunResult.ToList(), array.ElementType, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(ArrayToListMethod).GetMethod(nameof(ToListHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> ToListHelper(object[] array)
    {
        return new List<object?>(array);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is object[] array)
        {
            return new List<object?>(array);
        }
        throw new ArgumentException("实例必须是 object[] 类型");
    }
}
