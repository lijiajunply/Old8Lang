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
/// Task.Catch(errorHandler) - 捕获任务异常并执行错误处理函数
/// </summary>
public class TaskCatchMethod : BaseInstanceMethod
{
    public override string[] Names => ["Catch", "catch"];
    public override Type TargetType => typeof(TaskLangValue);
    public override string[]? ParameterNames => ["errorHandler"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var task = (TaskLangValue)instance;

        // 在当前作用域中评估错误处理函数参数
        var errorHandlerParam = parameters[0].Run(manager);

        if (errorHandlerParam is not FuncLangValue errorHandler)
        {
            throw new TypeError(position, "FuncValue", errorHandlerParam.GetType().Name);
        }

        // 创建一个新的任务，在原任务失败时执行错误处理
        var catchTask = task.Task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                // 任务失败，执行错误处理函数
                var exception = t.Exception?.InnerException ?? t.Exception;
                var errorMessage = exception?.Message ?? "Unknown error";

                var tempManager = new VariateManager();
                return errorHandler.Run(tempManager, [new StringLangValue(errorMessage, position)]);
            }
            else if (t.IsCanceled)
            {
                // 任务被取消，也视为错误
                var tempManager = new VariateManager();
                return errorHandler.Run(tempManager, [new StringLangValue("Task was canceled", position)]);
            }
            else
            {
                // 任务成功，直接返回结果
                return t.Result;
            }
        }, task.CancellationToken);

        return new TaskLangValue(catchTask, task.CancellationToken, position);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译器模式下暂不支持 Catch 方法（需要闭包支持）
        throw new NotSupportedException("Task.Catch 方法在编译器模式下暂不支持");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TaskLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // VM 模式下暂不支持 Catch 方法
        throw new NotSupportedException("Task.Catch 方法在 VM 模式下暂不支持");
    }
}
