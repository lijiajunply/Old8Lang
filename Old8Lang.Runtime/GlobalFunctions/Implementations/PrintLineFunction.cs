using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// PrintLine 函数 - 打印一行文本到输出
/// </summary>
public sealed class PrintLineFunction : BaseGlobalFunction
{
    public override string[] Names => ["PrintLine", "printLine"];
    public override string[] ParameterNames => ["values"];
    public override string? Documentation => "打印一行并换行";

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
        IlPrintHelper.Emit(ilGenerator, local, parameters, newline: true);
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
        // 与 ToStr() 同一套格式化：集合/字典/元组不再打印 .NET 类型名
        return Old8Lang.Runtime.VMValueFormatter.ToDisplayString(value);
    }
}
