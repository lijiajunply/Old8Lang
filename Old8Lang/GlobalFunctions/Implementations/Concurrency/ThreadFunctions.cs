using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Concurrency;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations.Concurrency;

/// <summary>
/// ThreadJoin 函数 - 等待线程完成
/// </summary>
public sealed class ThreadJoinFunction : BaseGlobalFunction
{
    public override string[] Names => ["ThreadJoin"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int threadId = ((IntLangValue)results[0]).Value;
        var result = ResourceManager.JoinThread(threadId);
        return result != null ? LangValueType.ObjToValue(result) : new AST.Expression.Intermediates.VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        Compiler.LocalManager local,
        SourcePosition position)
    {
        parameters[0].LoadIlValue(ilGenerator, local);
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.JoinThread));
        ilGenerator.Emit(OpCodes.Call, method!);
        // Result conversion handled by caller or generic Object wrapping
    }
    
    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, Compiler.LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int threadId = Convert.ToInt32(arguments[0]);
        return ResourceManager.JoinThread(threadId);
    }
}

/// <summary>
/// ThreadIsAlive 函数 - 检查线程是否存活
/// </summary>
public sealed class ThreadIsAliveFunction : BaseGlobalFunction
{
    public override string[] Names => ["ThreadIsAlive"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int threadId = ((IntLangValue)results[0]).Value;
        bool isAlive = ResourceManager.IsThreadAlive(threadId);
        return new BoolLangValue(isAlive);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        Compiler.LocalManager local,
        SourcePosition position)
    {
        parameters[0].LoadIlValue(ilGenerator, local);
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.IsThreadAlive));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, Compiler.LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int threadId = Convert.ToInt32(arguments[0]);
        return ResourceManager.IsThreadAlive(threadId);
    }
}

/// <summary>
/// ThreadDispose 函数 - 释放线程资源
/// </summary>
public sealed class ThreadDisposeFunction : BaseGlobalFunction
{
    public override string[] Names => ["ThreadDispose"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int threadId = ((IntLangValue)results[0]).Value;
        ResourceManager.DisposeThread(threadId);
        return new AST.Expression.Intermediates.VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        Compiler.LocalManager local,
        SourcePosition position)
    {
        parameters[0].LoadIlValue(ilGenerator, local);
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DisposeThread));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, Compiler.LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int threadId = Convert.ToInt32(arguments[0]);
        ResourceManager.DisposeThread(threadId);
        return null;
    }
}
