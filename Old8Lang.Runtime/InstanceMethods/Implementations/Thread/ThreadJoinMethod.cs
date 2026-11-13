using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Thread;

/// <summary>
/// Thread.Join(timeout?) - 等待线程完成
/// </summary>
public class ThreadJoinMethod : BaseInstanceMethod
{
    public override string[] Names => ["Join", "join"];
    public override Type TargetType => typeof(ThreadLangValue);
    public override string[]? ParameterNames => ["timeout"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var thread = (ThreadLangValue)instance;

        // 如果有超时参数
        if (parameters.Count == 1)
        {
            var timeoutParam = parameters[0].Run(manager);
            if (timeoutParam is not IntLangValue timeout)
            {
                throw new Error.TypeError(position, "IntValue", timeoutParam.GetType().Name);
            }

            return thread.Join(timeout);
        }

        // 无超时参数
        return thread.Join();
    }

    /// <summary>
    /// IL 模式：IL 下的线程值是 <see cref="ThreadLangValue"/>（不是原生 Thread）。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperName = parameters.Count == 1 ? nameof(JoinWithTimeoutHelper) : nameof(JoinHelper);
        if (parameters.Count == 1)
        {
            IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);
        }

        var helperMethod = typeof(ThreadJoinMethod).GetMethod(helperName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：等待线程结束并返回结果。
    /// </summary>
    public static LangValueType JoinHelper(LangValueType instance)
    {
        if (instance is not ThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是线程，实际是 {instance.GetType().Name}");
        }

        return thread.Join();
    }

    /// <summary>
    /// IL 模式的辅助方法：带超时等待线程（返回是否在超时前结束）。
    /// </summary>
    public static bool JoinWithTimeoutHelper(LangValueType instance, LangValueType timeoutMs)
    {
        if (instance is not ThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是线程，实际是 {instance.GetType().Name}");
        }

        var result = thread.Join(new IntLangValue((int)IlValueBridge.ToInt64(timeoutMs)));
        return result is BoolLangValue { Value: true };
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return parameters.Count == 1 ? typeof(bool) : typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not System.Threading.Thread thread)
        {
            throw new ArgumentException("实例必须是 Thread 类型");
        }

        if (arguments.Length == 1 && arguments[0] is int timeout)
        {
            return thread.Join(timeout);
        }

        thread.Join();
        return null;
    }
}
