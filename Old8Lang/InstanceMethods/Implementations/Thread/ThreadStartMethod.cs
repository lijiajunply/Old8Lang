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

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        if (parameters.Count == 1)
        {
            parameters[0].LoadIlValue(ilGenerator, local);
            var helperMethod = typeof(ThreadStartMethod).GetMethod(nameof(StartWithParameterHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            var helperMethod = typeof(ThreadStartMethod).GetMethod(nameof(StartHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    public static void StartHelper(System.Threading.Thread thread)
    {
        thread.Start();
    }

    public static void StartWithParameterHelper(System.Threading.Thread thread, object? parameter)
    {
        thread.Start(parameter);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ThreadLangValue);
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
