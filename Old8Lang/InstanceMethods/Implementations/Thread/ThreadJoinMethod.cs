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

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);

        if (parameters.Count == 1)
        {
            parameters[0].LoadIlValue(ilGenerator, local);
            var helperMethod = typeof(ThreadJoinMethod).GetMethod(nameof(JoinWithTimeoutHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
        else
        {
            var helperMethod = typeof(ThreadJoinMethod).GetMethod(nameof(JoinHelper),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            ilGenerator.Emit(OpCodes.Call, helperMethod!);
        }
    }

    public static object? JoinHelper(System.Threading.Thread thread)
    {
        thread.Join();
        return null;
    }

    public static bool JoinWithTimeoutHelper(System.Threading.Thread thread, object timeoutObj)
    {
        if (timeoutObj is not int timeout)
        {
            throw new ArgumentException("超时参数必须是整数类型");
        }

        return thread.Join(timeout);
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return parameters.Count == 1 ? typeof(bool) : typeof(object);
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
