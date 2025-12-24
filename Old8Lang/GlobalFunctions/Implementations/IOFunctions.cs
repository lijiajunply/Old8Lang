using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Print 函数 - 打印文本到输出（不换行）
/// </summary>
public sealed class PrintFunction : BaseGlobalFunction
{
    public override string[] Names => ["Print", "print"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => -1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        if (parameters.Count == 0) return new VoidLangValue();

        var results = EvaluateParameters(parameters, manager);
        var value = results[0].ToDisplayString();
        for (var i = 1; i < results.Count; i++)
        {
            value += results[i].ToDisplayString();
        }

        manager.Interpreter.OutputProvider.Write(value);
        return new VoidLangValue();
    }

    protected override void GenerateILInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        if (parameters.Count == 0) return;

        var printExpr = parameters[0];
        printExpr.LoadIlValue(ilGenerator, local);
        var printType = printExpr.OutputType(local);

        if (printType != typeof(string))
        {
            var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
            if (printType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, printType);
            }
            ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
        }

        ilGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new[] { typeof(string) })!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}

/// <summary>
/// ReadLine 函数 - 从输入读取一行文本
/// </summary>
public sealed class ReadLineFunction : BaseGlobalFunction
{
    public override string[] Names => ["ReadLine", "readLine"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        var res = manager.Interpreter.OutputProvider.ReadLine();
        return new StringLangValue(res);
    }

    protected override void GenerateILInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持
        ilGenerator.Emit(OpCodes.Ldstr, "");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(string);
    }
}

/// <summary>
/// Error 函数 - 打印错误信息
/// </summary>
public sealed class ErrorFunction : BaseGlobalFunction
{
    public override string[] Names => ["Error", "error"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => -1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        if (parameters.Count == 0)
        {
            manager.Interpreter.OutputProvider.Error("");
            return new VoidLangValue();
        }

        var results = EvaluateParameters(parameters, manager);
        var value = results[0].ToDisplayString();
        for (var i = 1; i < results.Count; i++)
        {
            value += results[i].ToDisplayString();
        }

        manager.Interpreter.OutputProvider.Error(value);
        return new VoidLangValue();
    }

    protected override void GenerateILInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式使用 Console.Error.WriteLine
        if (parameters.Count == 0)
        {
            var errorProp = typeof(Console).GetProperty("Error")!;
            ilGenerator.Emit(OpCodes.Call, errorProp.GetGetMethod()!);
            var writeLineMethod = typeof(TextWriter).GetMethod("WriteLine", Type.EmptyTypes)!;
            ilGenerator.Emit(OpCodes.Callvirt, writeLineMethod);
            return;
        }

        var expr = parameters[0];
        expr.LoadIlValue(ilGenerator, local);
        var exprType = expr.OutputType(local);

        if (exprType != typeof(string))
        {
            if (exprType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, exprType);
            }
            var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
            ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
        }

        var errorProperty = typeof(Console).GetProperty("Error")!;
        ilGenerator.Emit(OpCodes.Call, errorProperty.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Ldarg_0); // 交换栈顶两个元素的顺序
        var writeLineStringMethod = typeof(System.IO.TextWriter).GetMethod("WriteLine", new[] { typeof(string) })!;
        ilGenerator.Emit(OpCodes.Callvirt, writeLineStringMethod);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}

/// <summary>
/// Clear 函数 - 清空控制台
/// </summary>
public sealed class ClearFunction : BaseGlobalFunction
{
    public override string[] Names => ["Clear", "clear"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        manager.Interpreter.OutputProvider.Clear();
        return new VoidLangValue();
    }

    protected override void GenerateILInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        var clearMethod = typeof(Console).GetMethod("Clear", Type.EmptyTypes);
        if (clearMethod != null)
        {
            ilGenerator.Emit(OpCodes.Call, clearMethod);
        }
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}
