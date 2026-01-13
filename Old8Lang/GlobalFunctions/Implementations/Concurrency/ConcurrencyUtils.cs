using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Expression.Intermediates;
namespace Old8Lang.GlobalFunctions.Implementations.Concurrency;

/// <summary>
/// Sleep 函数 - 暂停当前线程
/// </summary>
public sealed class SleepFunction : BaseGlobalFunction
{
    public override string[] Names => ["Sleep"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int milliseconds = ((IntLangValue)results[0]).Value;
        Thread.Sleep(milliseconds);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 milliseconds 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 Thread.Sleep(int)
        var method = typeof(Thread).GetMethod(nameof(Thread.Sleep), new[] { typeof(int) });
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int milliseconds = Convert.ToInt32(arguments[0]);
        Thread.Sleep(milliseconds);
        return null;
    }
}

/// <summary>
/// GetCurrentThreadId 函数 - 获取当前线程ID
/// </summary>
public sealed class GetCurrentThreadIdFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetCurrentThreadId"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        int threadId = Environment.CurrentManagedThreadId;
        return new IntLangValue(threadId);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 调用 Environment.CurrentManagedThreadId
        var property = typeof(Environment).GetProperty(nameof(Environment.CurrentManagedThreadId));
        var method = property!.GetGetMethod();
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return Environment.CurrentManagedThreadId;
    }
}

/// <summary>
/// GetProcessorCount 函数 - 获取处理器数量
/// </summary>
public sealed class GetProcessorCountFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetProcessorCount"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        int count = Environment.ProcessorCount;
        return new IntLangValue(count);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 调用 Environment.ProcessorCount
        var property = typeof(Environment).GetProperty(nameof(Environment.ProcessorCount));
        var method = property!.GetGetMethod();
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return Environment.ProcessorCount;
    }
}
