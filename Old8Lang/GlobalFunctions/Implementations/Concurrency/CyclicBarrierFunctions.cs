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
/// CyclicBarrierCreate 函数 - 创建循环栅栏
/// </summary>
public sealed class CyclicBarrierCreateFunction : BaseGlobalFunction
{
    public override string[] Names => ["CyclicBarrierCreate"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int participantCount = ((IntLangValue)results[0]).Value;
        int id = ResourceManager.CreateCyclicBarrier(participantCount);
        return new IntLangValue(id);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 participantCount 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.CreateCyclicBarrier(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CreateCyclicBarrier));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }
}

/// <summary>
/// CyclicBarrierAwait 函数 - 等待所有参与者到达栅栏
/// </summary>
public sealed class CyclicBarrierAwaitFunction : BaseGlobalFunction
{
    public override string[] Names => ["CyclicBarrierAwait"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int barrierId = ((IntLangValue)results[0]).Value;
        ResourceManager.AwaitCyclicBarrier(barrierId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 barrierId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.AwaitCyclicBarrier(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.AwaitCyclicBarrier));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}

/// <summary>
/// CyclicBarrierAwaitTimeout 函数 - 等待所有参与者到达栅栏（带超时）
/// </summary>
public sealed class CyclicBarrierAwaitTimeoutFunction : BaseGlobalFunction
{
    public override string[] Names => ["CyclicBarrierAwaitTimeout"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int barrierId = ((IntLangValue)results[0]).Value;
        int timeoutMs = ((IntLangValue)results[1]).Value;
        bool success = ResourceManager.AwaitCyclicBarrierTimeout(barrierId, timeoutMs);
        return new BoolLangValue(success);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 barrierId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 timeoutMs 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.AwaitCyclicBarrierTimeout(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.AwaitCyclicBarrierTimeout));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }
}

/// <summary>
/// CyclicBarrierGetParticipantCount 函数 - 获取循环栅栏的参与者数量
/// </summary>
public sealed class CyclicBarrierGetParticipantCountFunction : BaseGlobalFunction
{
    public override string[] Names => ["CyclicBarrierGetParticipantCount"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int barrierId = ((IntLangValue)results[0]).Value;
        int count = ResourceManager.GetCyclicBarrierParticipantCount(barrierId);
        return new IntLangValue(count);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 barrierId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.GetCyclicBarrierParticipantCount(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.GetCyclicBarrierParticipantCount));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }
}

/// <summary>
/// CyclicBarrierGetWaitingCount 函数 - 获取当前等待的参与者数量
/// </summary>
public sealed class CyclicBarrierGetWaitingCountFunction : BaseGlobalFunction
{
    public override string[] Names => ["CyclicBarrierGetWaitingCount"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int barrierId = ((IntLangValue)results[0]).Value;
        int count = ResourceManager.GetCyclicBarrierWaitingCount(barrierId);
        return new IntLangValue(count);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 barrierId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.GetCyclicBarrierWaitingCount(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.GetCyclicBarrierWaitingCount));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }
}

/// <summary>
/// CyclicBarrierDispose 函数 - 销毁循环栅栏
/// </summary>
public sealed class CyclicBarrierDisposeFunction : BaseGlobalFunction
{
    public override string[] Names => ["CyclicBarrierDispose"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int barrierId = ((IntLangValue)results[0]).Value;
        ResourceManager.DisposeCyclicBarrier(barrierId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 barrierId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.DisposeCyclicBarrier(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DisposeCyclicBarrier));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}
