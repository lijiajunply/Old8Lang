using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.String;

/// <summary>
/// String.Repeat(count) - 重复字符串指定次数
/// </summary>
public class StringRepeatMethod : BaseInstanceMethod
{
    public override string[] Names => ["Repeat", "repeat"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[]? ParameterNames => ["count"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var countValue = parameters[0].Run(manager);

        if (countValue is not IntLangValue count)
        {
            throw new TypeError(position, "int", countValue.TypeToString());
        }

        if (count.Value < 0)
        {
            throw new InvalidOperationError(position, "重复次数不能为负数");
        }

        if (count.Value == 0)
        {
            return new StringLangValue(string.Empty);
        }

        var builder = new StringBuilder(str.Value.Length * count.Value);
        for (int i = 0; i < count.Value; i++)
        {
            builder.Append(str.Value);
        }

        return new StringLangValue(builder.ToString());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(StringRepeatMethod).GetMethod(nameof(RepeatHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static StringLangValue RepeatHelper(StringLangValue str, IntLangValue count)
    {
        if (count.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "重复次数不能为负数");
        }

        if (count.Value == 0)
        {
            return new StringLangValue(string.Empty);
        }

        var builder = new StringBuilder(str.Value.Length * count.Value);
        for (int i = 0; i < count.Value; i++)
        {
            builder.Append(str.Value);
        }

        return new StringLangValue(builder.ToString());
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(StringLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments[0] is int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "重复次数不能为负数");
            }

            if (count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(str.Length * count);
            for (int i = 0; i < count; i++)
            {
                builder.Append(str);
            }

            return builder.ToString();
        }

        throw new ArgumentException("参数类型不匹配");
    }
}
