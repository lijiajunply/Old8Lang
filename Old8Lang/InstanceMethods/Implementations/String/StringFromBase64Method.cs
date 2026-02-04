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
/// String.FromBase64() - 将Base64编码的字符串转换为原始字符串
/// </summary>
public class StringFromBase64Method : BaseInstanceMethod
{
    public override string[] Names => ["FromBase64", "fromBase64"];
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
            byte[] bytes = Convert.FromBase64String(str.Value);
            return new StringLangValue(Encoding.UTF8.GetString(bytes));
        }
        catch (Exception ex)
        {
            throw new Exception($"Base64解码失败: {ex.Message}", ex);
        }
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        var helperMethod = typeof(StringFromBase64Method).GetMethod(nameof(FromBase64Helper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static StringLangValue FromBase64Helper(StringLangValue str)
    {
        try
        {
            byte[] bytes = Convert.FromBase64String(str.Value);
            return new StringLangValue(Encoding.UTF8.GetString(bytes));
        }
        catch (Exception ex)
        {
            throw new Exception($"Base64解码失败: {ex.Message}", ex);
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
                byte[] bytes = Convert.FromBase64String(str);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                throw new Exception($"Base64解码失败: {ex.Message}", ex);
            }
        }

        throw new ArgumentException("实例必须是 string 类型");
    }
}
