using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;

using Old8Lang.AST.Visitor;

namespace Old8Lang.GlobalFunctions.Implementations.Concurrency;

/// <summary>
/// 并发全局函数在 IL 调用点的参数转换助手。
/// </summary>
/// <remarks>
/// 并发函数把「加载实参 + call ResourceManager.xxx(int)」直接发射到调用方 IL 里。
/// 实参静态类型可能是 <c>object</c>（例如 for-in 循环变量），直接 call 会生成无效 IL；
/// 这里按目标形参 <c>int</c> 做拆箱/数值转换。
/// </remarks>
public static class ConcurrencyIlHelpers
{
    /// <summary>加载一个会被当作 <c>int</c> 形参传入的实参。</summary>
    public static void EmitLoadIntArgument(
        LangExpression argument,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        argument.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var argumentType = argument.Accept(new TypeInferenceVisitor(local));
        if (argumentType is null || argumentType == typeof(int)) return;

        if (typeof(LangValueType).IsAssignableFrom(argumentType))
        {
            // for-in 循环变量可能是语言值（LangValueType 引用）。
            ilGenerator.Emit(OpCodes.Call,
                typeof(Old8Lang.InstanceMethods.Core.IlValueBridge).GetMethod(
                    nameof(InstanceMethods.Core.IlValueBridge.Unwrap))!);
            ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            return;
        }

        if (argumentType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            return;
        }

        if (argumentType.IsValueType)
        {
            if (argumentType == typeof(double) || argumentType == typeof(float))
            {
                ilGenerator.Emit(OpCodes.Conv_I4);
            }
            else if (argumentType == typeof(long) || argumentType == typeof(short) ||
                     argumentType == typeof(byte) || argumentType == typeof(ushort))
            {
                ilGenerator.Emit(OpCodes.Conv_I4);
            }
        }
    }

    /// <summary>
    /// IL 模式下调用 <see cref="Old8Lang.Concurrency.ResourceManager.TryReceiveChannel"/> 并展开
    /// <see cref="Old8Lang.Concurrency.ChannelReceiveResult"/>，与解释器/VM 返回实际值或 null 保持一致。
    /// </summary>
    public static object? TryReceiveChannelValue(int channelId, int timeoutMs)
    {
        var result = Old8Lang.Concurrency.ResourceManager.TryReceiveChannel(channelId, timeoutMs);
        return result.Success ? result.Value : null;
    }
}

/// <summary>
/// Sleep 函数 - 暂停当前线程
/// </summary>
public sealed class SleepFunction : BaseGlobalFunction
{
    public override string[] Names => ["Sleep"];
    public override string[] ParameterNames => ["milliseconds"];
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
        ConcurrencyIlHelpers.EmitLoadIntArgument(parameters[0], ilGenerator, local);

        // 调用 Thread.Sleep(int)
        var method = typeof(Thread).GetMethod(nameof(Thread.Sleep), [typeof(int)]);
        ilGenerator.Emit(OpCodes.Call, method!);
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
    public override string[] ParameterNames => [];
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
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
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
    public override string[] ParameterNames => [];
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
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        return Environment.ProcessorCount;
    }
}
