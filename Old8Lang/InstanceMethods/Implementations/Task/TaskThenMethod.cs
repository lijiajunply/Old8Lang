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
/// Task.Then(callback) - 任务完成后执行回调函数
/// </summary>
public class TaskThenMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
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

        var helperMethod = typeof(TaskThenMethod).GetMethod(nameof(ThenHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：语义与 <see cref="ExecuteInternal"/> 一致。
    /// </summary>
    /// <remarks>
    /// 返回原生 <c>Task&lt;object&gt;</c>（而不是 <see cref="TaskLangValue"/>），
    /// 与 IL 模式下 <c>Task.Delay</c> / <c>Task.Run</c> 的表示保持一致：
    /// 这样结果可以直接被 <c>await</c>、<c>Task.WhenAll</c>、<c>TaskAwaitMethod</c> 使用，
    /// 也能作为接收者继续链式调用（<see cref="IlValueBridge.Wrap"/> 会把它转回
    /// <see cref="TaskLangValue"/>）。
    /// </remarks>
    public static System.Threading.Tasks.Task<object> ThenHelper(LangValueType instance, LangValueType callback)
    {
        if (instance is not TaskLangValue task)
        {
            throw new ArgumentException($"Then 方法的实例必须是 Task，实际是 {instance.GetType().Name}");
        }

        if (callback is not FuncLangValue callbackFunc)
        {
            throw new ArgumentException("callback 参数必须是函数类型");
        }

        // IL 模式下 lambda 已经被编译成委托并包装成函数值，这里只需要一个可用的
        // manager 占位（IlDelegateFuncLangValue 不会使用它访问作用域）。
        var manager = new VariateManager();

        var thenTask = task.Task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                throw (t.Exception?.InnerException ?? t.Exception)!;
            }

            var args = new List<LangExpression> { t.Result };
            // 返回值转回 IL 模式的原生表示，保持与 Task<object> 的表示一致
            return IlValueBridge.Unwrap(callbackFunc.Run(manager, args));
        }, task.CancellationToken);

        return thenTask;
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(System.Threading.Tasks.Task<object>);
    }

    /// <summary>
    /// 虚拟机模式：接收者是 <see cref="TaskLangValue"/>，回调是 VM 函数对象
    /// （<see cref="ClosureValue"/> 或 <see cref="FunctionMetadata"/>）。
    /// </summary>
    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not TaskLangValue task)
        {
            throw new ArgumentException("Then 方法的实例必须是 TaskLangValue 类型");
        }

        var callback = arguments[0];
        if (callback is not (ClosureValue or FunctionMetadata))
        {
            throw new ArgumentException("callback 参数必须是函数类型");
        }

        // VMContext.CurrentVM 是 [ThreadStatic]，而 ContinueWith 的延续在线程池线程上执行，
        // 必须在调用点（VM 线程）先捕获虚拟机实例，回调里再用它执行函数对象。
        var vm = VMContext.CurrentVM
                 ?? throw new InvalidOperationException("VM 上下文未初始化");

        var thenTask = task.Task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                throw (t.Exception?.InnerException ?? t.Exception)!;
            }

            // 用 worker 虚拟机执行：CallFunctionObject 复用当前 VM 的求值栈，跨线程不安全。
            var raw = vm.ExecuteFunctionObjectInWorker(callback, [t.Result]);
            return TaskMethodVmSupport.ToLangValue(raw);
        }, task.CancellationToken);

        return new TaskLangValue(thenTask, task.CancellationToken);
    }
}
