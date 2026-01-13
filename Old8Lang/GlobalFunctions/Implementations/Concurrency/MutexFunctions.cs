using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Concurrency;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.GlobalFunctions.Implementations.Concurrency;

/// <summary>
/// MutexCreate 函数 - 创建互斥锁
/// </summary>
public sealed class MutexCreateFunction : BaseGlobalFunction
{
    public override string[] Names => ["MutexCreate"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        int id = ResourceManager.CreateMutex();
        return new IntLangValue(id);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CreateMutex));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return ResourceManager.CreateMutex();
    }
}

/// <summary>
/// MutexLock 函数 - 锁定互斥锁（阻塞）
/// </summary>
public sealed class MutexLockFunction : BaseGlobalFunction
{
    public override string[] Names => ["MutexLock"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int mutexId = ((IntLangValue)results[0]).Value;
        ResourceManager.LockMutex(mutexId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 mutexId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.LockMutex(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.LockMutex));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int mutexId = Convert.ToInt32(arguments[0]);
        ResourceManager.LockMutex(mutexId);
        return null;
    }
}

/// <summary>
/// MutexTryLock 函数 - 尝试锁定互斥锁（带超时）
/// </summary>
public sealed class MutexTryLockFunction : BaseGlobalFunction
{
    public override string[] Names => ["MutexTryLock"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int mutexId = ((IntLangValue)results[0]).Value;
        int timeoutMs = ((IntLangValue)results[1]).Value;
        bool success = ResourceManager.TryLockMutex(mutexId, timeoutMs);
        return new BoolLangValue(success);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 mutexId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 timeoutMs 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.TryLockMutex(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.TryLockMutex));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int mutexId = Convert.ToInt32(arguments[0]);
        int timeoutMs = Convert.ToInt32(arguments[1]);
        return ResourceManager.TryLockMutex(mutexId, timeoutMs);
    }
}

/// <summary>
/// MutexUnlock 函数 - 解锁互斥锁
/// </summary>
public sealed class MutexUnlockFunction : BaseGlobalFunction
{
    public override string[] Names => ["MutexUnlock"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int mutexId = ((IntLangValue)results[0]).Value;
        ResourceManager.UnlockMutex(mutexId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 mutexId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.UnlockMutex(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.UnlockMutex));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int mutexId = Convert.ToInt32(arguments[0]);
        ResourceManager.UnlockMutex(mutexId);
        return null;
    }
}

/// <summary>
/// MutexDispose 函数 - 销毁互斥锁
/// </summary>
public sealed class MutexDisposeFunction : BaseGlobalFunction
{
    public override string[] Names => ["MutexDispose"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int mutexId = ((IntLangValue)results[0]).Value;
        ResourceManager.DisposeMutex(mutexId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 mutexId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.DisposeMutex(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DisposeMutex));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int mutexId = Convert.ToInt32(arguments[0]);
        ResourceManager.DisposeMutex(mutexId);
        return null;
    }
}
