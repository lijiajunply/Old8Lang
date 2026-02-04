using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.String;

/// <summary>
/// String.ToUpper 方法 - 转换为大写
/// </summary>
public class StringToUpperMethod : BaseInstanceMethod
{
    public override string[] Names => ["ToUpper", "toUpper", "Upper", "upper"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        return new StringLangValue(str.Value.ToUpper());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载字符串实例
        instance.LoadIlValue(ilGenerator, local);

        // 调用辅助方法
        var helperMethod = typeof(StringToUpperMethod).GetMethod(nameof(ToUpperHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：转换为大写
    /// </summary>
    public static StringLangValue ToUpperHelper(StringLangValue str)
    {
        return new StringLangValue(str.Value.ToUpper());
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(StringLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str)
        {
            return str.ToUpper();
        }

        throw new ArgumentException("实例必须是 string 类型");
    }
}
