using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Task;

/// <summary>
/// Task.Finally(finallyHandler) - 无论任务成功或失败都执行的清理函数
/// </summary>
public class TaskFinallyMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
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

        // 保存当前的 manager 用于回调执行
        var capturedManager = manager;

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
                finallyHandler.Run(capturedManager, []);
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

        var resultTask = new TaskLangValue(finallyTask, task.CancellationToken, position);

        // 设置 ExternalManager 以支持链式调用
        resultTask.ExternalManager = capturedManager;

        return resultTask;
    }

    /// <summary>
    /// IL 模式：接收者是原生 <see cref="System.Threading.Tasks.Task"/>（<c>Task.Delay</c>）或
    /// <c>Task&lt;object&gt;</c>（<c>Task.Run</c> / 本方法），清理函数是编译好的 .NET 委托。
    /// 两者先用 <see cref="IlValueBridge"/> 转换成 Old8Lang 值，再交给 helper 执行。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        // 接收者：原生 Task -> TaskLangValue
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        // 清理函数：编译好的委托 -> 函数值
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(TaskFinallyMethod).GetMethod(nameof(FinallyHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：语义与 <see cref="ExecuteInternal"/> 一致。
    /// </summary>
    /// <remarks>
    /// 返回原生 <c>Task&lt;object&gt;</c>，与 IL 模式下 Task 的既有表示保持一致，
    /// 以便结果可以继续被 <c>await</c> / 链式调用使用。
    /// </remarks>
    public static System.Threading.Tasks.Task<object> FinallyHelper(LangValueType instance, LangValueType finallyHandler)
    {
        if (instance is not TaskLangValue task)
        {
            throw new ArgumentException($"Finally 方法的实例必须是 Task，实际是 {instance.GetType().Name}");
        }

        if (finallyHandler is not FuncLangValue finallyHandlerFunc)
        {
            throw new ArgumentException("finallyHandler 参数必须是函数类型");
        }

        var manager = new VariateManager();

        var finallyTask = task.Task.ContinueWith(t =>
        {
            LangValueType result;
            Exception? exception = null;

            if (t.IsFaulted)
            {
                exception = t.Exception?.InnerException ?? t.Exception;
                result = new VoidLangValue();
            }
            else if (t.IsCanceled)
            {
                exception = new OperationCanceledException("Task was canceled");
                result = new VoidLangValue();
            }
            else
            {
                result = t.Result;
            }

            // 执行清理函数（不传递参数）；清理函数自身抛出的异常优先
            finallyHandlerFunc.Run(manager, []);

            if (exception != null)
            {
                throw exception;
            }

            return IlValueBridge.Unwrap(result);
        }, task.CancellationToken);

        return finallyTask;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(System.Threading.Tasks.Task<object>);
    }

    /// <summary>
    /// 虚拟机模式：接收者是 <see cref="TaskLangValue"/>，清理函数是 VM 函数对象。
    /// </summary>
    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not TaskLangValue task)
        {
            throw new ArgumentException("Finally 方法的实例必须是 TaskLangValue 类型");
        }

        var finallyHandler = arguments[0];
        if (finallyHandler is not (ClosureValue or FunctionMetadata))
        {
            throw new ArgumentException("finallyHandler 参数必须是函数类型");
        }

        // VMContext.CurrentVM 是 [ThreadStatic]，延续在线程池线程上执行，必须先在 VM 线程上捕获。
        var vm = VMContext.CurrentVM
                 ?? throw new InvalidOperationException("VM 上下文未初始化");

        var finallyTask = task.Task.ContinueWith(t =>
        {
            LangValueType result;
            Exception? exception = null;

            if (t.IsFaulted)
            {
                exception = t.Exception?.InnerException ?? t.Exception;
                result = new VoidLangValue();
            }
            else if (t.IsCanceled)
            {
                exception = new OperationCanceledException("Task was canceled");
                result = new VoidLangValue();
            }
            else
            {
                result = t.Result;
            }

            // 执行清理函数（不传递参数）；清理函数自身抛出的异常优先
            vm.ExecuteFunctionObjectInWorker(finallyHandler, []);

            if (exception != null)
            {
                throw exception;
            }

            return result;
        }, task.CancellationToken);

        return new TaskLangValue(finallyTask, task.CancellationToken);
    }
}
