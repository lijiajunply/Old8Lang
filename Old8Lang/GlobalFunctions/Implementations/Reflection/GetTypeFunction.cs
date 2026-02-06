using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using Old8Lang.Utilities;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// GetType 函数 - 从类型名获取 TypeLangValue
/// </summary>
public sealed class GetTypeFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetType"];
    public override string[] ParameterNames => ["typeName"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var typeNameValue = results[0];

        if (typeNameValue is not StringLangValue stringValue)
        {
            throw new InvalidOperationError(position, "类型名必须是字符串");
        }

        var typeName = stringValue.Value;
        var template = TypeTemplate.FindType(typeName);

        if (template is null)
        {
            throw new InvalidOperationError(position, $"找不到类型: {typeName}");
        }

        return new TypeLangValue(template);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载类型名参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetType(string)
        var method = GlobalMethodInfoCache.GetMethod(typeof(ReflectionHelper), nameof(ReflectionHelper.GetType));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TypeLangValue);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        var typeName = arguments[0] as string;

        if (string.IsNullOrEmpty(typeName))
        {
            throw new InvalidOperationException("类型名必须是字符串");
        }

        // 在 VM 模式下，返回 TypeLangValue
        return new TypeLangValue(typeName);
    }
}
