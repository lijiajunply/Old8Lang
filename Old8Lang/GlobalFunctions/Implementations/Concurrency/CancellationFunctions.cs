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
/// CreateCancellationTokenSource 函数 - 创建取消令牌源
/// </summary>
public sealed class CreateCancellationTokenSourceFunction : BaseGlobalFunction
{
    public override string[] Names => ["CreateCancellationTokenSource"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        int id = ResourceManager.CreateCancellationTokenSource();
        return new IntLangValue(id);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CreateCancellationTokenSource));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }
}

/// <summary>
/// Cancel 函数 - 取消令牌源
/// </summary>
public sealed class CancelFunction : BaseGlobalFunction
{
    public override string[] Names => ["Cancel"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int ctsId = ((IntLangValue)results[0]).Value;
        ResourceManager.Cancel(ctsId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 ctsId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.Cancel(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.Cancel));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}

/// <summary>
/// CancelAfter 函数 - 延迟取消令牌源
/// </summary>
public sealed class CancelAfterFunction : BaseGlobalFunction
{
    public override string[] Names => ["CancelAfter"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int ctsId = ((IntLangValue)results[0]).Value;
        int delayMs = ((IntLangValue)results[1]).Value;
        ResourceManager.CancelAfter(ctsId, delayMs);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 ctsId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 delayMs 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.CancelAfter(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CancelAfter));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}

/// <summary>
/// DisposeCancellationTokenSource 函数 - 销毁取消令牌源
/// </summary>
public sealed class DisposeCancellationTokenSourceFunction : BaseGlobalFunction
{
    public override string[] Names => ["DisposeCancellationTokenSource"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int ctsId = ((IntLangValue)results[0]).Value;
        ResourceManager.DisposeCancellationTokenSource(ctsId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 ctsId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.DisposeCancellationTokenSource(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DisposeCancellationTokenSource));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }
}
