using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Task;

/// <summary>
/// Task.ContinueWith(callback) - 任务完成后执行延续函数
/// </summary>
public class TaskContinueWithMethod : BaseInstanceMethod
{
    public override string[] Names => ["ContinueWith", "continueWith"];
    public override Type TargetType => typeof(TaskLangValue);
    public override string[]? ParameterNames => ["callback"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var task = (TaskLangValue)instance;

        var callbackParam = parameters[0].Run(manager);

        if (callbackParam is not FuncLangValue callback)
        {
            throw new TypeError(position, "FuncValue", callbackParam.GetType().Name);
        }

        var capturedManager = manager;

        var continueTask = task.Task.ContinueWith(async t =>
        {
            if (t.IsFaulted)
            {
                throw (t.Exception?.InnerException ?? t.Exception)!;
            }

            var args = new List<LangExpression> { t.Result };
            var result = callback.Run(capturedManager, args);

            if (result is TaskLangValue taskValue)
            {
                return await taskValue.AwaitAsync();
            }

            return result;
        }, task.CancellationToken).Unwrap();

        var resultTask = new TaskLangValue(continueTask, task.CancellationToken, position);
        resultTask.ExternalManager = capturedManager;

        return resultTask;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Task.ContinueWith 方法在编译器模式下暂不支持");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TaskLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("Task.ContinueWith 方法在 VM 模式下暂不支持");
    }
}
