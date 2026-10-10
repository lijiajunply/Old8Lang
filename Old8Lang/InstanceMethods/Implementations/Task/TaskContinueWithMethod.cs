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
/// Task.ContinueWith(callback) - 任务完成后执行延续函数
/// </summary>
public class TaskContinueWithMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
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

        // 回调：编译好的委托 -> 函数值
        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(TaskContinueWithMethod).GetMethod(nameof(ContinueWithHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：语义与 <see cref="ExecuteInternal"/> 一致
    /// （回调返回 Task 时会等待该任务，因此这里同样需要 <c>Unwrap</c>）。
    /// </summary>
    /// <remarks>
    /// 返回原生 <c>Task&lt;object&gt;</c>，与 IL 模式下 Task 的既有表示保持一致，
    /// 以便结果可以继续被 <c>await</c> / 链式调用使用。
    /// </remarks>
    public static System.Threading.Tasks.Task<object> ContinueWithHelper(LangValueType instance, LangValueType callback)
    {
        if (instance is not TaskLangValue task)
        {
            throw new ArgumentException($"ContinueWith 方法的实例必须是 Task，实际是 {instance.GetType().Name}");
        }

        if (callback is not FuncLangValue callbackFunc)
        {
            throw new ArgumentException("callback 参数必须是函数类型");
        }

        var manager = new VariateManager();

        var continueTask = task.Task.ContinueWith(async t =>
        {
            if (t.IsFaulted)
            {
                throw (t.Exception?.InnerException ?? t.Exception)!;
            }

            var args = new List<LangExpression> { t.Result };
            var result = callbackFunc.Run(manager, args);

            if (result is TaskLangValue taskValue)
            {
                return IlValueBridge.Unwrap(await taskValue.AwaitAsync());
            }

            return IlValueBridge.Unwrap(result);
        }, task.CancellationToken).Unwrap();

        return continueTask;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(System.Threading.Tasks.Task<object>);
    }

    /// <summary>
    /// 虚拟机模式：接收者是 <see cref="TaskLangValue"/>，回调是 VM 函数对象。
    /// </summary>
    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not TaskLangValue task)
        {
            throw new ArgumentException("ContinueWith 方法的实例必须是 TaskLangValue 类型");
        }

        var callback = arguments[0];
        if (callback is not (ClosureValue or FunctionMetadata))
        {
            throw new ArgumentException("callback 参数必须是函数类型");
        }

        // VMContext.CurrentVM 是 [ThreadStatic]，延续在线程池线程上执行，必须先在 VM 线程上捕获。
        var vm = VMContext.CurrentVM
                 ?? throw new InvalidOperationException("VM 上下文未初始化");

        var continueTask = task.Task.ContinueWith(async t =>
        {
            if (t.IsFaulted)
            {
                throw (t.Exception?.InnerException ?? t.Exception)!;
            }

            var raw = vm.ExecuteFunctionObjectInWorker(callback, [t.Result]);
            var result = TaskMethodVmSupport.ToLangValue(raw);

            // 与解释器实现一致：回调返回 Task 时等待它，返回普通值时直接透传
            if (result is TaskLangValue taskValue)
            {
                return await taskValue.AwaitAsync();
            }

            return result;
        }, task.CancellationToken).Unwrap();

        return new TaskLangValue(continueTask, task.CancellationToken);
    }
}
