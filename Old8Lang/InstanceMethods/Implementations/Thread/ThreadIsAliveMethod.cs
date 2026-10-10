using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Thread;

/// <summary>
/// Thread.IsAlive() - 检查线程是否正在运行
/// </summary>
public class ThreadIsAliveMethod : BaseInstanceMethod
{
    public override string[] Names => ["IsAlive", "isAlive", "isalive"];
    public override Type TargetType => typeof(ThreadLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var thread = (ThreadLangValue)instance;
        return new BoolLangValue(thread.IsAlive());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // IL 模式：IL 下的线程值是 ThreadLangValue，不是原生 Thread
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);
        var helperMethod = typeof(ThreadIsAliveMethod).GetMethod(nameof(IsAliveHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：线程是否仍在运行。
    /// </summary>
    public static bool IsAliveHelper(LangValueType instance)
    {
        if (instance is not ThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是线程，实际是 {instance.GetType().Name}");
        }

        return thread.IsAlive();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is System.Threading.Thread thread)
        {
            return thread.IsAlive;
        }
        throw new ArgumentException("实例必须是 Thread 类型");
    }
}
