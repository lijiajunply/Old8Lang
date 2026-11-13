using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List.TakeLast(count) - 获取列表的最后 n 个元素
/// </summary>
public class ListTakeLastMethod : BaseInstanceMethod
{
    public override string[] Names => ["TakeLast", "takeLast"];
    public override Type TargetType => typeof(ListLangValue);
    public override string[] ParameterNames => ["count"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var list = (ListLangValue)instance;
        var countValue = parameters[0].Run(manager);

        if (countValue is not IntLangValue countInt)
        {
            throw new ArgumentError(position, "count 参数必须是整数类型");
        }

        var count = countInt.Value;

        if (count < 0)
        {
            throw new ArgumentError(position, "count 参数不能为负数");
        }

        var result = list.Values.Skip(Math.Max(0, list.Values.Count - count)).ToList();
        return new ListLangValue(result);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ListTakeLastMethod).GetMethod(nameof(TakeLastHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static List<object?> TakeLastHelper(List<object?> list, int count)
    {
        if (count < 0)
        {
            throw new ArgumentException("count 参数不能为负数");
        }

        return list.Skip(Math.Max(0, list.Count - count)).ToList();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(List<object?>);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is List<object?> list && arguments.Length > 0)
        {
            if (arguments[0] is not int count)
            {
                throw new ArgumentException("count 参数必须是整数类型");
            }

            if (count < 0)
            {
                throw new ArgumentException("count 参数不能为负数");
            }

            return list.Skip(Math.Max(0, list.Count - count)).ToList();
        }

        throw new ArgumentException("实例必须是 List<object?> 类型");
    }
}
