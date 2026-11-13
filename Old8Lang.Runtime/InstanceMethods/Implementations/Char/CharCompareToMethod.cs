using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Char;

/// <summary>
/// Char.CompareTo(other) - 比较当前字符与另一个字符
/// </summary>
public class CharCompareToMethod : BaseInstanceMethod
{
    public override string[] Names => ["CompareTo", "compareTo"];
    public override Type TargetType => typeof(CharLangValue);
    public override string[] ParameterNames => ["other"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var charValue = (CharLangValue)instance;
        var other = parameters[0].Run(manager) as CharLangValue;

        if (other == null)
        {
            throw new ArgumentException("参数必须是 Char 类型");
        }

        return IntLangValue.Create(charValue.Value.CompareTo(other.Value), position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(CharCompareToMethod).GetMethod(nameof(CompareToHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static int CompareToHelper(char c1, char c2)
    {
        return c1.CompareTo(c2);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not char c1)
        {
            throw new ArgumentException("实例必须是 Char 类型");
        }

        if (arguments.Length == 0 || arguments[0] is not char c2)
        {
            throw new ArgumentException("参数必须是 Char 类型");
        }

        return c1.CompareTo(c2);
    }
}
