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
/// ChannelCreate 函数 - 创建无界通道
/// </summary>
public sealed class ChannelCreateFunction : BaseGlobalFunction
{
    public override string[] Names => ["ChannelCreate"];
    public override string[]? ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        int id = ResourceManager.CreateChannel();
        return new IntLangValue(id);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CreateChannel));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        return ResourceManager.CreateChannel();
    }
}

/// <summary>
/// ChannelCreateBounded 函数 - 创建有界通道
/// </summary>
public sealed class ChannelCreateBoundedFunction : BaseGlobalFunction
{
    public override string[] Names => ["ChannelCreateBounded"];
    public override string[]? ParameterNames => ["capacity"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int capacity = ((IntLangValue)results[0]).Value;
        int id = ResourceManager.CreateBoundedChannel(capacity);
        return new IntLangValue(id);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 capacity 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.CreateBoundedChannel(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CreateBoundedChannel));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(int);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int capacity = Convert.ToInt32(arguments[0]);
        return ResourceManager.CreateBoundedChannel(capacity);
    }
}

/// <summary>
/// ChannelSend 函数 - 发送数据到通道（阻塞）
/// </summary>
public sealed class ChannelSendFunction : BaseGlobalFunction
{
    public override string[] Names => ["ChannelSend"];
    public override string[]? ParameterNames => ["channelId", "value"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int channelId = ((IntLangValue)results[0]).Value;
        object value = results[1].GetValue();
        ResourceManager.SendChannel(channelId, value);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 channelId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 value 参数并转换为 object
        parameters[1].LoadIlValue(ilGenerator, local);
        if (parameters[1].OutputType(local).IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, parameters[1].OutputType(local));
        }

        // 调用 ResourceManager.SendChannel(int, object)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.SendChannel));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int channelId = Convert.ToInt32(arguments[0]);
        object value = arguments[1]!;
        ResourceManager.SendChannel(channelId, value);
        return null;
    }
}

/// <summary>
/// ChannelTrySend 函数 - 尝试发送数据到通道（带超时）
/// </summary>
public sealed class ChannelTrySendFunction : BaseGlobalFunction
{
    public override string[] Names => ["ChannelTrySend"];
    public override string[]? ParameterNames => ["channelId", "value", "timeoutMs"];
    public override int MinParameterCount => 3;
    public override int MaxParameterCount => 3;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int channelId = ((IntLangValue)results[0]).Value;
        object value = results[1].GetValue();
        int timeoutMs = ((IntLangValue)results[2]).Value;
        bool success = ResourceManager.TrySendChannel(channelId, value, timeoutMs);
        return new BoolLangValue(success);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 channelId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 value 参数并转换为 object
        parameters[1].LoadIlValue(ilGenerator, local);
        if (parameters[1].OutputType(local).IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, parameters[1].OutputType(local));
        }

        // 加载 timeoutMs 参数
        parameters[2].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.TrySendChannel(int, object, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.TrySendChannel));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int channelId = Convert.ToInt32(arguments[0]);
        object value = arguments[1]!;
        int timeoutMs = Convert.ToInt32(arguments[2]);
        return ResourceManager.TrySendChannel(channelId, value, timeoutMs);
    }
}

/// <summary>
/// ChannelReceive 函数 - 从通道接收数据（阻塞）
/// </summary>
public sealed class ChannelReceiveFunction : BaseGlobalFunction
{
    public override string[] Names => ["ChannelReceive"];
    public override string[]? ParameterNames => ["channelId"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int channelId = ((IntLangValue)results[0]).Value;
        object value = ResourceManager.ReceiveChannel(channelId);
        return LangValueType.ObjToValue(value);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 channelId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.ReceiveChannel(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.ReceiveChannel));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int channelId = Convert.ToInt32(arguments[0]);
        return ResourceManager.ReceiveChannel(channelId);
    }
}

/// <summary>
/// ChannelTryReceive 函数 - 尝试从通道接收数据（带超时）
/// </summary>
public sealed class ChannelTryReceiveFunction : BaseGlobalFunction
{
    public override string[] Names => ["ChannelTryReceive"];
    public override string[]? ParameterNames => ["channelId", "timeoutMs"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int channelId = ((IntLangValue)results[0]).Value;
        int timeoutMs = ((IntLangValue)results[1]).Value;
        object? value = ResourceManager.TryReceiveChannel(channelId, timeoutMs);
        return value is null ? new VoidLangValue() : LangValueType.ObjToValue(value);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 channelId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载 timeoutMs 参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.TryReceiveChannel(int, int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.TryReceiveChannel));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int channelId = Convert.ToInt32(arguments[0]);
        int timeoutMs = Convert.ToInt32(arguments[1]);
        var result = ResourceManager.TryReceiveChannel(channelId, timeoutMs);
        // 返回实际值或 null，而不是 ChannelReceiveResult 对象
        return result.Success ? result.Value : null;
    }
}

/// <summary>
/// ChannelClose 函数 - 关闭通道
/// </summary>
public sealed class ChannelCloseFunction : BaseGlobalFunction
{
    public override string[] Names => ["ChannelClose"];
    public override string[]? ParameterNames => ["channelId"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int channelId = ((IntLangValue)results[0]).Value;
        ResourceManager.CloseChannel(channelId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 channelId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.CloseChannel(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.CloseChannel));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int channelId = Convert.ToInt32(arguments[0]);
        ResourceManager.CloseChannel(channelId);
        return null;
    }
}

/// <summary>
/// ChannelDispose 函数 - 销毁通道
/// </summary>
public sealed class ChannelDisposeFunction : BaseGlobalFunction
{
    public override string[] Names => ["ChannelDispose"];
    public override string[]? ParameterNames => ["channelId"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int channelId = ((IntLangValue)results[0]).Value;
        ResourceManager.DisposeChannel(channelId);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载 channelId 参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ResourceManager.DisposeChannel(int)
        var method = typeof(ResourceManager).GetMethod(nameof(ResourceManager.DisposeChannel));
        ilGenerator.Emit(OpCodes.Call, method);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int channelId = Convert.ToInt32(arguments[0]);
        ResourceManager.DisposeChannel(channelId);
        return null;
    }
}
