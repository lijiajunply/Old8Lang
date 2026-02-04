using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Task;

/// <summary>
/// Task.Finally(finallyHandler) - 无论任务成功或失败都执行的清理函数
/// </summary>
public class TaskFinallyMethod : BaseInstanceMethod
{
    public override string[] Names => ["Finally", "finally"];
    public override Type TargetType => typeof(TaskLangValue);
    public override string[]? ParameterNames => ["finallyHandler"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var task = (TaskLangValue)instance;

        // 在当前作用域中评估 finally 处理函数参数
        var finallyHandlerParam = parameters[0].Run(manager);

        if (finallyHandlerParam is not FuncLangValue finallyHandler)
        {
            throw new TypeError(position, "FuncValue", finallyHandlerParam.GetType().Name);
        }

        // 创建一个新的任务，无论原任务成功或失败都执行 finally 处理函数
        var finallyTask = task.Task.ContinueWith(t =>
        {
            LangValueType result;
            Exception? exception = null;

            // 保存原任务的结果或异常
            if (t.IsFaulted)
            {
                exception = t.Exception?.InnerException ?? t.Exception;
                result = new VoidLangValue(position);
            }
            else if (t.IsCanceled)
            {
                exception = new OperationCanceledException("Task was canceled");
                result = new VoidLangValue(position);
            }
            else
            {
                result = t.Result;
            }

            // 执行 finally 处理函数（不传递参数）
            try
            {
                var tempManager = new VariateManager();
                finallyHandler.Run(tempManager, []);
            }
            catch (Exception finallyEx)
            {
                // 如果 finally 处理函数抛出异常，优先抛出 finally 的异常
                throw finallyEx;
            }

            // 如果原任务有异常，重新抛出
            if (exception != null)
            {
                throw exception;
            }

            // 返回原任务的结果
            return result;
        }, task.CancellationToken);

        return new TaskLangValue(finallyTask, task.CancellationToken, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译器模式下暂不支持 Finally 方法（需要闭包支持）
        throw new NotSupportedException("Task.Finally 方法在编译器模式下暂不支持");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TaskLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // VM 模式下暂不支持 Finally 方法
        throw new NotSupportedException("Task.Finally 方法在 VM 模式下暂不支持");
    }
}
