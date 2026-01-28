using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Concurrency;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.GlobalFunctions.Implementations.Concurrency;

/// <summary>
/// SemaphoreCreate 函数 - 创建信号量
/// </summary>
public sealed class SemaphoreCreateFunction : BaseGlobalFunction
{
    public override string[] Names => ["SemaphoreCreate"];
    public override string[]? ParameterNames => ["initialCount", "maxCount"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int initialCount = ((IntLangValue)results[0]).Value;
        int maxCount = ((IntLangValue)results[1]).Value;
        int id = ResourceManager.CreateSemaphore(initialCount, maxCount);
        return new IntLangValue(id);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 initialCount 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 maxCount 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.CreateSemaphore(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CreateSemaphore));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int initialCount = Convert.ToInt32(arguments[0]);
        int maxCount = Convert.ToInt32(arguments[1]);
        return ResourceManager.CreateSemaphore(initialCount, maxCount);
    }
}

/// <summary>
/// SemaphoreAcquire 函数 - 获取信号量（阻塞）
/// </summary>
public sealed class SemaphoreAcquireFunction : BaseGlobalFunction
{
    public override string[] Names => ["SemaphoreAcquire"];
    public override string[]? ParameterNames => ["semaphoreId"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int semaphoreId = ((IntLangValue)results[0]).Value;
        ResourceManager.AcquireSemaphore(semaphoreId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 semaphoreId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.AcquireSemaphore(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.AcquireSemaphore));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int semaphoreId = Convert.ToInt32(arguments[0]);
        ResourceManager.AcquireSemaphore(semaphoreId);
        return null;
    }
}

/// <summary>
/// SemaphoreTryAcquire 函数 - 尝试获取信号量（带超时）
/// </summary>
public sealed class SemaphoreTryAcquireFunction : BaseGlobalFunction
{
    public override string[] Names => ["SemaphoreTryAcquire"];
    public override string[]? ParameterNames => ["semaphoreId", "timeoutMs"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int semaphoreId = ((IntLangValue)results[0]).Value;
        int timeoutMs = ((IntLangValue)results[1]).Value;
        bool success = ResourceManager.TryAcquireSemaphore(semaphoreId, timeoutMs);
        return new BoolLangValue(success);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 semaphoreId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 timeoutMs 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.TryAcquireSemaphore(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.TryAcquireSemaphore));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int semaphoreId = Convert.ToInt32(arguments[0]);
        int timeoutMs = Convert.ToInt32(arguments[1]);
        return ResourceManager.TryAcquireSemaphore(semaphoreId, timeoutMs);
    }
}

/// <summary>
/// SemaphoreRelease 函数 - 释放信号量
/// </summary>
public sealed class SemaphoreReleaseFunction : BaseGlobalFunction
{
    public override string[] Names => ["SemaphoreRelease"];
    public override string[]? ParameterNames => ["semaphoreId"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int semaphoreId = ((IntLangValue)results[0]).Value;
        ResourceManager.ReleaseSemaphore(semaphoreId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 semaphoreId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.ReleaseSemaphore(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.ReleaseSemaphore));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int semaphoreId = Convert.ToInt32(arguments[0]);
        ResourceManager.ReleaseSemaphore(semaphoreId);
        return null;
    }
}

/// <summary>
/// SemaphoreDispose 函数 - 销毁信号量
/// </summary>
public sealed class SemaphoreDisposeFunction : BaseGlobalFunction
{
    public override string[] Names => ["SemaphoreDispose"];
    public override string[]? ParameterNames => ["semaphoreId"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int semaphoreId = ((IntLangValue)results[0]).Value;
        ResourceManager.DisposeSemaphore(semaphoreId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 semaphoreId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.DisposeSemaphore(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DisposeSemaphore));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int semaphoreId = Convert.ToInt32(arguments[0]);
        ResourceManager.DisposeSemaphore(semaphoreId);
        return null;
    }
}
