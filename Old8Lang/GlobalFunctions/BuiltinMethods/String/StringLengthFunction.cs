using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.BuiltinMethods.String;

/// <summary>
/// String.Length() - 获取字符串长度
/// </summary>
/// <remarks>
/// 用法: str.Length()
/// 返回: 字符串的长度 (int)
/// </remarks>
public sealed class StringLengthFunction : BaseGlobalFunction
{
    public override string[] Names => ["String.Length"];
    public override string[]? ParameterNames => ["str"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var str = (StringLangValue)results[0];
        return new IntLangValue(str.Value.Length);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 参数 0: string (已在栈上)
        // 调用 string.Length 属性的 getter
        var lengthProperty = typeof(string).GetProperty("Length")!;
        ilGenerator.Emit(OpCodes.Callvirt, lengthProperty.GetGetMethod()!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var str = (string)arguments[0]!;
        return str.Length;
    }
}
