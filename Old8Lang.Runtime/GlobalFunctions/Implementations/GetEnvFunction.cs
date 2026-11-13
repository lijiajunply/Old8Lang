using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// GetEnv 函数 - 获取环境变量
/// </summary>
public sealed class GetEnvFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetEnv", "getEnv"];
    public override string[] ParameterNames => ["key"];

    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var key = results[0].ToDisplayString();

        var value = Environment.GetEnvironmentVariable(key);
        return value != null ? new StringLangValue(value) : new NullLangValue();
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载参数（环境变量名）
        var keyExpr = parameters[0];
        keyExpr.LoadIlValue(ilGenerator, local);

        // 转换为字符串
        var keyType = keyExpr.OutputType(local);
        if (keyType != typeof(string))
        {
            if (keyType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, keyType);
            }
            var toStringMethod = typeof(object).GetMethod("ToString");
            if (toStringMethod != null)
            {
                ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
            }
        }

        // 调用 Environment.GetEnvironmentVariable
        var getEnvMethod = typeof(Environment).GetMethod("GetEnvironmentVariable", [typeof(string)]);
        if (getEnvMethod != null)
        {
            ilGenerator.Emit(OpCodes.Call, getEnvMethod);
        }
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        if (arguments.Length == 0)
            return null;

        var key = arguments[0]?.ToString();
        if (string.IsNullOrEmpty(key))
            return null;

        return Environment.GetEnvironmentVariable(key);
    }
}
