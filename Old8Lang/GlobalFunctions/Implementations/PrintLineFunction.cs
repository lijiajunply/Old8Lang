using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Utilities;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// PrintLine 函数 - 打印一行文本到输出
/// </summary>
public sealed class PrintLineFunction : BaseGlobalFunction
{
    public override string[] Names => ["PrintLine", "printLine"];
    public override string[]? ParameterNames => ["values"];

    public override int MinParameterCount => 0;

    public override int MaxParameterCount => -1; // 不限制参数数量

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        if (parameters.Count == 0)
        {
            manager.Interpreter.OutputProvider.WriteLine("");
            return new VoidLangValue();
        }

        var results = EvaluateParameters(parameters, manager);
        var value = results[0].ToDisplayString();
        for (var i = 1; i < results.Count; i++)
        {
            value += results[i].ToDisplayString();
        }

        manager.Interpreter.OutputProvider.WriteLine(value);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        if (parameters.Count == 0)
        {
            // 没有参数，调用 Console.WriteLine()
            var writeLineNoArg = GlobalMethodInfoCache.GetMethod(typeof(Console), "WriteLine", Type.EmptyTypes);
            if (writeLineNoArg is not null)
            {
                ilGenerator.Emit(OpCodes.Call, writeLineNoArg);
            }
            return;
        }

        // 简化实现：只处理第一个参数，将其转换为字符串
        var printLineExpr = parameters[0];
        printLineExpr.LoadIlValue(ilGenerator, local);
        var printLineType = printLineExpr.OutputType(local);

        // 直接调用Console.WriteLine(object)方法，让CLR处理类型转换
        var writeLineObject = GlobalMethodInfoCache.GetMethod(typeof(Console), "WriteLine", [typeof(object)]);
        if (writeLineObject is not null)
        {
            // 如果是值类型，先装箱
            if (printLineType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, printLineType);
            }

            ilGenerator.Emit(OpCodes.Call, writeLineObject);
        }
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        if (arguments.Length == 0)
        {
            Console.WriteLine();
            return null;
        }

        // 将所有参数转换为字符串并连接
        var value = ToString(arguments[0]);
        for (var i = 1; i < arguments.Length; i++)
        {
            value += ToString(arguments[i]);
        }

        Console.WriteLine(value);
        return null;
    }

    private static string ToString(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return s;
        if (value is LangValueType langValue) return langValue.ToDisplayString();
        return value.ToString() ?? "";
    }
}
