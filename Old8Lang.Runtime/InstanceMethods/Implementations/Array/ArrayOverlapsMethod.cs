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
/// Array.Overlaps(other) - 检查两个数组是否有交集
/// </summary>
public class ArrayOverlapsMethod : BaseInstanceMethod
{
    public override string[] Names => ["Overlaps", "overlaps"];
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

        var otherSet = new HashSet<string>(otherArray.RunResult.Select(v => v.ToDisplayString()));

        foreach (var item in array.RunResult)
        {
            if (otherSet.Contains(item.ToDisplayString()))
            {
                return new BoolLangValue(true);
            }
        }

        return new BoolLangValue(false);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(ArrayOverlapsMethod).GetMethod(nameof(OverlapsHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static bool OverlapsHelper(object?[] array, object?[] other)
    {
        var otherSet = new HashSet<string>(other.Select(v => v?.ToString() ?? "null"));

        foreach (var item in array)
        {
            if (otherSet.Contains(item?.ToString() ?? "null"))
            {
                return true;
            }
        }

        return false;
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

            return OverlapsHelper(array, other);
        }

        throw new ArgumentException("实例必须是 object?[] 类型");
    }
}
