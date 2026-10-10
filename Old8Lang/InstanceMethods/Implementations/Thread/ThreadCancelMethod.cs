using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Thread;

/// <summary>
/// Thread.Cancel() - 取消线程执行
/// </summary>
public class ThreadCancelMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Cancel", "cancel"];
    public override Type TargetType => typeof(ThreadLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var thread = (ThreadLangValue)instance;
        thread.Cancel();
        return new VoidLangValue();
    }

    /// <summary>
    /// IL 模式：接收者是 <see cref="ThreadLangValue"/>，直接调用静态 helper。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperMethod = typeof(ThreadCancelMethod).GetMethod(nameof(CancelHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：取消线程执行。
    /// </summary>
    public static LangValueType CancelHelper(LangValueType instance)
    {
        if (instance is not ThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是线程，实际是 {instance.GetType().Name}");
        }

        thread.Cancel();
        return new VoidLangValue();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // VM 模式的线程对象是 VMThreadLangValue（整数线程 id + Concurrency.ResourceManager），
        // 与解释器的 ThreadLangValue 不同构，且 ResourceManager 没有取消原语，
        // 因此该方法在 VM 下不可达（TargetType 是 ThreadLangValue）。见 Todo.md「VM 线程模型」。
        throw new NotSupportedException(
            "Thread.Cancel 在 VM 模式下不可用：VM 的线程是 VMThreadLangValue（线程 id + ResourceManager），没有取消原语");
    }
}
