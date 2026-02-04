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
/// Array.SetEquals(other) - 检查两个数组是否包含相同的元素（忽略顺序和重复）
/// </summary>
public class ArraySetEqualsMethod : BaseInstanceMethod
{
    public override string[] Names => ["SetEquals", "setEquals"];
    public override Type TargetType => typeof(ArrayLangValue);
    public override string[] ParameterNames => ["other"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var array = (ArrayLangValue)instance;
        var otherValue = parameters[0].Run(manager);

        if (otherValue is not ArrayLangValue otherArray)
        {
            throw new ArgumentError(position, "other 参数必须是数组类型");
        }

        var thisSet = new HashSet<string>(array.RunResult.Select(v => v.ToDisplayString()));
        var otherSet = new HashSet<string>(otherArray.RunResult.Select(v => v.ToDisplayString()));

        return new BoolLangValue(thisSet.SetEquals(otherSet));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ArraySetEqualsMethod).GetMethod(nameof(SetEqualsHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool SetEqualsHelper(object?[] array, object?[] other)
    {
        var thisSet = new HashSet<string>(array.Select(v => v?.ToString() ?? "null"));
        var otherSet = new HashSet<string>(other.Select(v => v?.ToString() ?? "null"));

        return thisSet.SetEquals(otherSet);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is object?[] array && arguments.Length > 0)
        {
            if (arguments[0] is not object?[] other)
            {
                throw new ArgumentException("other 参数必须是数组类型");
            }

            return SetEqualsHelper(array, other);
        }

        throw new ArgumentException("实例必须是 object?[] 类型");
    }
}
