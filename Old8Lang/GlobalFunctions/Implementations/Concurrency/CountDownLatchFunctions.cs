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
/// CountDownLatchCreate 函数 - 创建倒计时锁
/// </summary>
public sealed class CountDownLatchCreateFunction : BaseGlobalFunction
{
    public override string[] Names => ["CountDownLatchCreate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int count = ((IntLangValue)results[0]).Value;
        int id = ResourceManager.CreateCountDownLatch(count);
        return new IntLangValue(id);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 count 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.CreateCountDownLatch(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CreateCountDownLatch));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int count = Convert.ToInt32(arguments[0]);
        return ResourceManager.CreateCountDownLatch(count);
    }
}

/// <summary>
/// CountDownLatchCountDown 函数 - 倒计时锁计数减一
/// </summary>
public sealed class CountDownLatchCountDownFunction : BaseGlobalFunction
{
    public override string[] Names => ["CountDownLatchCountDown"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int latchId = ((IntLangValue)results[0]).Value;
        ResourceManager.CountDown(latchId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 latchId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.CountDown(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CountDown));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int latchId = Convert.ToInt32(arguments[0]);
        ResourceManager.CountDown(latchId);
        return null;
    }
}

/// <summary>
/// CountDownLatchWait 函数 - 等待倒计时锁归零
/// </summary>
public sealed class CountDownLatchWaitFunction : BaseGlobalFunction
{
    public override string[] Names => ["CountDownLatchWait"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int latchId = ((IntLangValue)results[0]).Value;
        ResourceManager.WaitCountDownLatch(latchId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 latchId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.WaitCountDownLatch(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.WaitCountDownLatch));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int latchId = Convert.ToInt32(arguments[0]);
        ResourceManager.WaitCountDownLatch(latchId);
        return null;
    }
}

/// <summary>
/// CountDownLatchWaitTimeout 函数 - 等待倒计时锁归零（带超时）
/// </summary>
public sealed class CountDownLatchWaitTimeoutFunction : BaseGlobalFunction
{
    public override string[] Names => ["CountDownLatchWaitTimeout"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int latchId = ((IntLangValue)results[0]).Value;
        int timeoutMs = ((IntLangValue)results[1]).Value;
        bool success = ResourceManager.WaitCountDownLatchTimeout(latchId, timeoutMs);
        return new BoolLangValue(success);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 latchId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 timeoutMs 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.WaitCountDownLatchTimeout(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.WaitCountDownLatchTimeout));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int latchId = Convert.ToInt32(arguments[0]);
        int timeoutMs = Convert.ToInt32(arguments[1]);
        return ResourceManager.WaitCountDownLatchTimeout(latchId, timeoutMs);
    }
}

/// <summary>
/// CountDownLatchGetCount 函数 - 获取倒计时锁当前计数
/// </summary>
public sealed class CountDownLatchGetCountFunction : BaseGlobalFunction
{
    public override string[] Names => ["CountDownLatchGetCount"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int latchId = ((IntLangValue)results[0]).Value;
        int count = ResourceManager.GetCountDownLatchCount(latchId);
        return new IntLangValue(count);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 latchId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.GetCountDownLatchCount(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.GetCountDownLatchCount));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int latchId = Convert.ToInt32(arguments[0]);
        return ResourceManager.GetCountDownLatchCount(latchId);
    }
}

/// <summary>
/// CountDownLatchDispose 函数 - 销毁倒计时锁
/// </summary>
public sealed class CountDownLatchDisposeFunction : BaseGlobalFunction
{
    public override string[] Names => ["CountDownLatchDispose"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int latchId = ((IntLangValue)results[0]).Value;
        ResourceManager.DisposeCountDownLatch(latchId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 latchId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.DisposeCountDownLatch(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DisposeCountDownLatch));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int latchId = Convert.ToInt32(arguments[0]);
        ResourceManager.DisposeCountDownLatch(latchId);
        return null;
    }
}
