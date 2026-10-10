using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
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
/// Task.Catch(errorHandler) - 捕获任务异常并执行错误处理函数
/// </summary>
public class TaskCatchMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
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

        // 保存当前的 manager 用于回调执行
        var capturedManager = manager;

        // 创建一个新的任务，在原任务失败时执行错误处理
        var catchTask = task.Task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                // 任务失败，执行错误处理函数
                var exception = t.Exception?.InnerException ?? t.Exception;
                var errorMessage = exception?.Message ?? "Unknown error";

                var args = new List<LangExpression> { new StringLangValue(errorMessage, position) };
                return errorHandler.Run(capturedManager, args);
            }
            else if (t.IsCanceled)
            {
                // 任务被取消，也视为错误
                var args = new List<LangExpression> { new StringLangValue("Task was canceled", position) };
                return errorHandler.Run(capturedManager, args);
            }
            else
            {
                // 任务成功，直接返回结果
                return t.Result;
            }
        }, task.CancellationToken);

        var resultTask = new TaskLangValue(catchTask, task.CancellationToken, position);

        // 设置 ExternalManager 以支持链式调用
        resultTask.ExternalManager = capturedManager;

        return resultTask;
    }

    /// <summary>
    /// IL 模式：接收者是原生 <see cref="System.Threading.Tasks.Task"/>（<c>Task.Delay</c>）或
    /// <c>Task&lt;object&gt;</c>（<c>Task.Run</c> / 本方法），回调是编译好的 .NET 委托。
    /// 两者先用 <see cref="IlValueBridge"/> 转换成 Old8Lang 值，再交给 helper 执行。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        // 接收者：原生 Task -> TaskLangValue
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        // 错误处理函数：编译好的委托 -> 函数值
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(TaskCatchMethod).GetMethod(nameof(CatchHelper),
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
    public static System.Threading.Tasks.Task<object> CatchHelper(LangValueType instance, LangValueType errorHandler)
    {
        if (instance is not TaskLangValue task)
        {
            throw new ArgumentException($"Catch 方法的实例必须是 Task，实际是 {instance.GetType().Name}");
        }

        if (errorHandler is not FuncLangValue errorHandlerFunc)
        {
            throw new ArgumentException("errorHandler 参数必须是函数类型");
        }

        var manager = new VariateManager();

        var catchTask = task.Task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                var exception = t.Exception?.InnerException ?? t.Exception;
                var errorMessage = exception?.Message ?? "Unknown error";

                var args = new List<LangExpression> { new StringLangValue(errorMessage) };
                return IlValueBridge.Unwrap(errorHandlerFunc.Run(manager, args));
            }

            if (t.IsCanceled)
            {
                var args = new List<LangExpression> { new StringLangValue("Task was canceled") };
                return IlValueBridge.Unwrap(errorHandlerFunc.Run(manager, args));
            }

            return (object)t.Result;
        }, task.CancellationToken);

        return catchTask;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(System.Threading.Tasks.Task<object>);
    }

    /// <summary>
    /// 虚拟机模式：接收者是 <see cref="TaskLangValue"/>，错误处理函数是 VM 函数对象。
    /// </summary>
    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not TaskLangValue task)
        {
            throw new ArgumentException("Catch 方法的实例必须是 TaskLangValue 类型");
        }

        var errorHandler = arguments[0];
        if (errorHandler is not (ClosureValue or FunctionMetadata))
        {
            throw new ArgumentException("errorHandler 参数必须是函数类型");
        }

        // VMContext.CurrentVM 是 [ThreadStatic]，延续在线程池线程上执行，必须先在 VM 线程上捕获。
        var vm = VMContext.CurrentVM
                 ?? throw new InvalidOperationException("VM 上下文未初始化");

        var catchTask = task.Task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                // 与解释器实现一致：把异常消息作为字符串传给错误处理函数
                var exception = t.Exception?.InnerException ?? t.Exception;
                var errorMessage = exception?.Message ?? "Unknown error";

                var raw = vm.ExecuteFunctionObjectInWorker(errorHandler, [new StringLangValue(errorMessage)]);
                return TaskMethodVmSupport.ToLangValue(raw);
            }

            if (t.IsCanceled)
            {
                var raw = vm.ExecuteFunctionObjectInWorker(errorHandler, [new StringLangValue("Task was canceled")]);
                return TaskMethodVmSupport.ToLangValue(raw);
            }

            // 任务成功，直接透传结果
            return t.Result;
        }, task.CancellationToken);

        return new TaskLangValue(catchTask, task.CancellationToken);
    }
}
