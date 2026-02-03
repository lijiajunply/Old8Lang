using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.String;

/// <summary>
/// String.Length 方法 - 获取字符串长度
/// </summary>
public class StringLengthMethod : BaseInstanceMethod
{
    public override string[] Names => ["Length", "length", "Len", "len"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        return new IntLangValue(str.Value.Length);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载字符串实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(StringLengthMethod).GetMethod(nameof(LengthHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：获取字符串长度
    /// </summary>
    public static IntLangValue LengthHelper(StringLangValue str)
    {
        return new IntLangValue(str.Value.Length);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(IntLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str)
        {
            return str.Length;
        }

        throw new ArgumentException("实例必须是 string 类型");
    }
}
