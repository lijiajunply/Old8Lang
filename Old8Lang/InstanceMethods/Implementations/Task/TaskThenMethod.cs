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
/// Task.Then(callback) - 任务完成后执行回调函数
/// </summary>
public class TaskThenMethod : BaseInstanceMethod
{
    public override string[] Names => ["Then", "then"];
    public override Type TargetType => typeof(TaskLangValue);
    public override string[]? ParameterNames => ["callback"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var task = (TaskLangValue)instance;

        // 在当前作用域中评估回调参数
        var callbackParam = parameters[0].Run(manager);

        if (callbackParam is not FuncLangValue callback)
        {
            throw new TypeError(position, "FuncValue", callbackParam.GetType().Name);
        }

        // 保存当前的 manager 用于回调执行
        var capturedManager = manager;

        // 创建一个新的任务，在原任务完成后执行回调
        var thenTask = task.Task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                throw (t.Exception?.InnerException ?? t.Exception)!;
            }

            // 使用捕获的 manager 执行回调函数，传入任务结果
            // 注意：这里使用 Run(manager, args) 而不是 Run(tempManager, [result])
            var args = new List<LangExpression> { t.Result };
            return callback.Run(capturedManager, args);
        }, task.CancellationToken);

        var resultTask = new TaskLangValue(thenTask, task.CancellationToken, position);

        // 设置 ExternalManager 以支持链式调用
        resultTask.ExternalManager = capturedManager;

        return resultTask;
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译器模式下暂不支持 Then 方法（需要闭包支持）
        throw new NotSupportedException("Task.Then 方法在编译器模式下暂不支持");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TaskLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // VM 模式下暂不支持 Then 方法
        throw new NotSupportedException("Task.Then 方法在 VM 模式下暂不支持");
    }
}
