using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Thread;

/// <summary>
/// Thread.Start(parameter?) - 启动线程
/// </summary>
public class ThreadStartMethod : BaseInstanceMethod
{
    public override string[] Names => ["Start", "start"];
    public override Type TargetType => typeof(ThreadLangValue);
    public override string[]? ParameterNames => ["parameter"];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var thread = (ThreadLangValue)instance;

        // 如果有参数
        if (parameters.Count == 1)
        {
            var paramValue = parameters[0].Run(manager);
            thread.Start(paramValue.GetValue());
        }
        else
        {
            thread.Start();
        }

        return thread;
    }

    /// <summary>
    /// IL 模式：IL 下的线程值是 <see cref="ThreadLangValue"/>（不是原生 Thread），
    /// 所以把接收者与参数统一转成 Old8Lang 值后交给静态 helper。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        var helperName = parameters.Count == 1 ? nameof(StartWithParameterHelper) : nameof(StartHelper);
        if (parameters.Count == 1)
        {
            IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);
        }

        var helperMethod = typeof(ThreadStartMethod).GetMethod(helperName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：启动线程。
    /// </summary>
    public static LangValueType StartHelper(LangValueType instance)
    {
        if (instance is not ThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是线程，实际是 {instance.GetType().Name}");
        }

        thread.Start();
        return thread;
    }

    /// <summary>
    /// IL 模式的辅助方法：带参数启动线程。
    /// </summary>
    public static LangValueType StartWithParameterHelper(LangValueType instance, LangValueType parameter)
    {
        if (instance is not ThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是线程，实际是 {instance.GetType().Name}");
        }

        thread.Start(parameter.GetValue());
        return thread;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not System.Threading.Thread thread)
        {
            throw new ArgumentException("实例必须是 Thread 类型");
        }

        if (arguments.Length == 1)
        {
            thread.Start(arguments[0]);
        }
        else
        {
            thread.Start();
        }

        return instance;
    }
}
