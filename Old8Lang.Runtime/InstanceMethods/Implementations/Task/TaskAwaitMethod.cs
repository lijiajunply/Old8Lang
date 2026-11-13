using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Task;

/// <summary>
/// Task.Await() - 等待任务完成并返回结果（阻塞）
/// </summary>
public class TaskAwaitMethod : BaseInstanceMethod
{
    public override string[] Names => ["Await", "await", "Wait", "wait"];
    public override Type TargetType => typeof(TaskLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var task = (TaskLangValue)instance;
        return task.Await();
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(TaskAwaitMethod).GetMethod(nameof(AwaitHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static object? AwaitHelper(System.Threading.Tasks.Task<object> task)
    {
        try
        {
            return task.Result;
        }
        catch (AggregateException aggEx)
        {
            var innerException = aggEx.InnerException ?? aggEx;
            throw innerException;
        }
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 字节码模式求值栈上放的是 TaskLangValue（OpCode.CallAsync 与 Task.* 静态方法都产出它），
        // 不是底层的 Task<object>。两者都要支持，否则 t.Wait() / t.Await() 会报“实例必须是 Task<object> 类型”。
        if (instance is TaskLangValue taskLangValue)
        {
            return taskLangValue.Await();
        }

        if (instance is System.Threading.Tasks.Task<object> task)
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException aggEx)
            {
                var innerException = aggEx.InnerException ?? aggEx;
                throw innerException;
            }
        }

        throw new ArgumentException("实例必须是 TaskLangValue 或 Task<object> 类型");
    }
}
