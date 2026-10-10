using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Thread;

/// <summary>
/// Thread.WithTimeout(timeoutMs) - 为线程添加超时限制
/// </summary>
public class ThreadWithTimeoutMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["WithTimeout", "withTimeout"];
    public override Type TargetType => typeof(ThreadLangValue);
    public override string[]? ParameterNames => ["timeoutMs"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var thread = (ThreadLangValue)instance;
        var timeoutParam = parameters[0].Run(manager);

        if (timeoutParam is not IntLangValue timeout)
        {
            throw new TypeError(position, "int", timeoutParam.TypeToString());
        }

        return thread.WithTimeout(timeout.Value);
    }

    /// <summary>
    /// IL 模式：接收者与超时时间都转成 Old8Lang 值后调用静态 helper。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(ThreadWithTimeoutMethod).GetMethod(nameof(WithTimeoutHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：给线程加超时。
    /// </summary>
    public static ThreadLangValue WithTimeoutHelper(LangValueType instance, LangValueType timeoutMs)
    {
        if (instance is not ThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是线程，实际是 {instance.GetType().Name}");
        }

        return thread.WithTimeout((int)IlValueBridge.ToInt64(timeoutMs));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ThreadLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 见 Todo.md「VM 线程模型」：VM 的线程是 VMThreadLangValue，与本方法的 TargetType 不同构。
        throw new NotSupportedException(
            "Thread.WithTimeout 在 VM 模式下不可用：VM 的线程是 VMThreadLangValue（线程 id + ResourceManager），没有超时原语");
    }
}
