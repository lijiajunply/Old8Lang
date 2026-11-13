using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.String;

/// <summary>
/// String.ToBase64() - 将字符串转换为Base64编码
/// </summary>
public class StringToBase64Method : BaseInstanceMethod
{
    public override string[] Names => ["ToBase64", "toBase64"];
    public override Type TargetType => typeof(StringLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var str = (StringLangValue)instance;
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str.Value);
            return new StringLangValue(Convert.ToBase64String(bytes));
        }
        catch (Exception ex)
        {
            throw new Exception($"Base64编码失败: {ex.Message}", ex);
        }
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(StringToBase64Method).GetMethod(nameof(ToBase64Helper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static StringLangValue ToBase64Helper(StringLangValue str)
    {
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str.Value);
            return new StringLangValue(Convert.ToBase64String(bytes));
        }
        catch (Exception ex)
        {
            throw new Exception($"Base64编码失败: {ex.Message}", ex);
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(StringLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is string str)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                throw new Exception($"Base64编码失败: {ex.Message}", ex);
            }
        }

        throw new ArgumentException("实例必须是 string 类型");
    }
}
