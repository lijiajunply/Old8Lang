using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Concurrency;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.GlobalFunctions.Implementations.Concurrency;

/// <summary>
/// ReadWriteLockCreate 函数 - 创建读写锁
/// </summary>
public sealed class ReadWriteLockCreateFunction : BaseGlobalFunction
{
    public override string[] Names => ["ReadWriteLockCreate"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        int id = ResourceManager.CreateReadWriteLock();
        return new IntLangValue(id);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CreateReadWriteLock));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return ResourceManager.CreateReadWriteLock();
    }
}

/// <summary>
/// ReadLockAcquire 函数 - 获取读锁
/// </summary>
public sealed class ReadLockAcquireFunction : BaseGlobalFunction
{
    public override string[] Names => ["ReadLockAcquire"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int lockId = ((IntLangValue)results[0]).Value;
        ResourceManager.AcquireReadLock(lockId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 lockId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.AcquireReadLock(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.AcquireReadLock));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int lockId = Convert.ToInt32(arguments[0]);
        ResourceManager.AcquireReadLock(lockId);
        return null;
    }
}

/// <summary>
/// ReadLockRelease 函数 - 释放读锁
/// </summary>
public sealed class ReadLockReleaseFunction : BaseGlobalFunction
{
    public override string[] Names => ["ReadLockRelease"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int lockId = ((IntLangValue)results[0]).Value;
        ResourceManager.ReleaseReadLock(lockId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 lockId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.ReleaseReadLock(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.ReleaseReadLock));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int lockId = Convert.ToInt32(arguments[0]);
        ResourceManager.ReleaseReadLock(lockId);
        return null;
    }
}

/// <summary>
/// WriteLockAcquire 函数 - 获取写锁
/// </summary>
public sealed class WriteLockAcquireFunction : BaseGlobalFunction
{
    public override string[] Names => ["WriteLockAcquire"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int lockId = ((IntLangValue)results[0]).Value;
        ResourceManager.AcquireWriteLock(lockId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 lockId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.AcquireWriteLock(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.AcquireWriteLock));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int lockId = Convert.ToInt32(arguments[0]);
        ResourceManager.AcquireWriteLock(lockId);
        return null;
    }
}

/// <summary>
/// WriteLockRelease 函数 - 释放写锁
/// </summary>
public sealed class WriteLockReleaseFunction : BaseGlobalFunction
{
    public override string[] Names => ["WriteLockRelease"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int lockId = ((IntLangValue)results[0]).Value;
        ResourceManager.ReleaseWriteLock(lockId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 lockId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.ReleaseWriteLock(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.ReleaseWriteLock));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int lockId = Convert.ToInt32(arguments[0]);
        ResourceManager.ReleaseWriteLock(lockId);
        return null;
    }
}

/// <summary>
/// ReadLockTryAcquire 函数 - 尝试获取读锁（带超时）
/// </summary>
public sealed class ReadLockTryAcquireFunction : BaseGlobalFunction
{
    public override string[] Names => ["ReadLockTryAcquire"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int lockId = ((IntLangValue)results[0]).Value;
        int timeoutMs = ((IntLangValue)results[1]).Value;
        bool success = ResourceManager.TryAcquireReadLock(lockId, timeoutMs);
        return new BoolLangValue(success);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 lockId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 timeoutMs 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.TryAcquireReadLock(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.TryAcquireReadLock));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int lockId = Convert.ToInt32(arguments[0]);
        int timeoutMs = Convert.ToInt32(arguments[1]);
        return ResourceManager.TryAcquireReadLock(lockId, timeoutMs);
    }
}

/// <summary>
/// WriteLockTryAcquire 函数 - 尝试获取写锁（带超时）
/// </summary>
public sealed class WriteLockTryAcquireFunction : BaseGlobalFunction
{
    public override string[] Names => ["WriteLockTryAcquire"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int lockId = ((IntLangValue)results[0]).Value;
        int timeoutMs = ((IntLangValue)results[1]).Value;
        bool success = ResourceManager.TryAcquireWriteLock(lockId, timeoutMs);
        return new BoolLangValue(success);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 lockId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 timeoutMs 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.TryAcquireWriteLock(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.TryAcquireWriteLock));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int lockId = Convert.ToInt32(arguments[0]);
        int timeoutMs = Convert.ToInt32(arguments[1]);
        return ResourceManager.TryAcquireWriteLock(lockId, timeoutMs);
    }
}

/// <summary>
/// ReadWriteLockDispose 函数 - 销毁读写锁
/// </summary>
public sealed class ReadWriteLockDisposeFunction : BaseGlobalFunction
{
    public override string[] Names => ["ReadWriteLockDispose"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int lockId = ((IntLangValue)results[0]).Value;
        ResourceManager.DisposeReadWriteLock(lockId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 lockId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.DisposeReadWriteLock(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DisposeReadWriteLock));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int lockId = Convert.ToInt32(arguments[0]);
        ResourceManager.DisposeReadWriteLock(lockId);
        return null;
    }
}
