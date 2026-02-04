using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.String;

/// <summary>
/// String.Substring 方法 - 获取子字符串
/// </summary>
public class StringSubstringMethod : BaseInstanceMethod
{
    public override string[] Names => ["Substring", "substring", "Substr", "substr"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[] ParameterNames => ["start", "length"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        var startValue = parameters[0].Run(manager);

        if (startValue is not IntLangValue start)
        {
            throw new TypeError(position, $"Substring 方法的第一个参数必须是整数类型，但实际是 {startValue.GetType().Name}");
        }

        if (start.Value < 0 || start.Value > str.Value.Length)
        {
            throw new ArgumentError(position, $"起始索引 {start.Value} 超出范围 [0, {str.Value.Length}]");
        }

        // 如果提供了长度参数
        if (parameters.Count > 1)
        {
            var lengthValue = parameters[1].Run(manager);
            if (lengthValue is not IntLangValue length)
            {
                throw new TypeError(position, $"Substring 方法的第二个参数必须是整数类型，但实际是 {lengthValue.GetType().Name}");
            }

            if (length.Value < 0 || start.Value + length.Value > str.Value.Length)
            {
                throw new ArgumentError(position, $"长度 {length.Value} 超出范围");
            }

            return new StringLangValue(str.Value.Substring(start.Value, length.Value));
        }

        // 只有起始索引，截取到末尾
        return new StringLangValue(str.Value.Substring(start.Value));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载字符串实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载起始索引
        parameters[0].LoadIlValue(ilGenerator, local);

        if (parameters.Count > 1)
        {
            // 加载长度
            parameters[1].LoadIlValue(ilGenerator, local);

            var helperMethod = typeof(StringSubstringMethod).GetMethod(nameof(SubstringWithLengthHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            var helperMethod = typeof(StringSubstringMethod).GetMethod(nameof(SubstringHelper),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    /// <summary>
    /// 辅助方法：获取子字符串（只有起始索引）
    /// </summary>
    public static StringLangValue SubstringHelper(StringLangValue str, IntLangValue start)
    {
        if (start.Value < 0 || start.Value > str.Value.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start), $"起始索引 {start.Value} 超出范围");
        }

        return new StringLangValue(str.Value.Substring(start.Value));
    }

    /// <summary>
    /// 辅助方法：获取子字符串（带长度）
    /// </summary>
    public static StringLangValue SubstringWithLengthHelper(StringLangValue str, IntLangValue start, IntLangValue length)
    {
        if (start.Value < 0 || start.Value > str.Value.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start), $"起始索引 {start.Value} 超出范围");
        }

        if (length.Value < 0 || start.Value + length.Value > str.Value.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"长度 {length.Value} 超出范围");
        }

        return new StringLangValue(str.Value.Substring(start.Value, length.Value));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(StringLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str && arguments.Length > 0 && arguments[0] is int start)
        {
            if (arguments.Length > 1 && arguments[1] is int length)
            {
                return str.Substring(start, length);
            }
            return str.Substring(start);
        }

        throw new ArgumentException("实例必须是 string 类型");
    }
}
