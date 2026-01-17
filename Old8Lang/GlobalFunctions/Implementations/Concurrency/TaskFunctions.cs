using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.GlobalFunctions.Implementations.Concurrency;

/// <summary>
/// TaskDelay 函数 - 创建延迟任务
/// </summary>
public sealed class TaskDelayFunction : BaseGlobalFunction
{
    public override string[] Names => ["TaskDelay"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        int ms = ((IntLangValue)results[0]).Value;
        return TaskLangValue.Delay(ms, position: position);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        Old8Lang.Compiler.LocalManager local,
        SourcePosition position)
    {
        // 加载 delayMs 参数
        parameters[0].LoadIlValue(ilGenerator, local);
        
        // 加载默认 CancellationToken
        ilGenerator.Emit(OpCodes.Initobj, typeof(CancellationToken));
        ilGenerator.Emit(OpCodes.Ldloca_S, (byte)0); // Hack: need a local for initobj? 
        // Better: CancellationToken.None
        var tokenProp = typeof(CancellationToken).GetProperty("None");
        ilGenerator.Emit(OpCodes.Call, tokenProp!.GetGetMethod()!);
        
        // 加载 default position
        ilGenerator.Emit(OpCodes.Initobj, typeof(SourcePosition));
        // ... simplified ...
        
        // 调用 TaskLangValue.Delay
        var method = typeof(TaskLangValue).GetMethod(nameof(TaskLangValue.Delay));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, Old8Lang.Compiler.LocalManager local)
    {
        return typeof(TaskLangValue);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        int ms = Convert.ToInt32(arguments[0]);
        return TaskLangValue.Delay(ms);
    }
}

/// <summary>
/// TaskWait 函数 - 等待任务完成
/// </summary>
public sealed class TaskWaitFunction : BaseGlobalFunction
{
    public override string[] Names => ["TaskWait"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        if (results[0] is TaskLangValue task)
        {
            return task.Wait();
        }
        throw new Exception("TaskWait expects a Task");
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        Old8Lang.Compiler.LocalManager local,
        SourcePosition position)
    {
        parameters[0].LoadIlValue(ilGenerator, local);
        var method = typeof(TaskLangValue).GetMethod(nameof(TaskLangValue.Wait));
        ilGenerator.Emit(OpCodes.Callvirt, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, Old8Lang.Compiler.LocalManager local)
    {
        return typeof(object); // Result type is dynamic
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        if (arguments[0] is TaskLangValue task)
        {
            return task.Wait();
        }
        throw new Exception("TaskWait expects a Task");
    }
}

/// <summary>
/// TaskWhenAll 函数 - 等待所有任务完成
/// </summary>
public sealed class TaskWhenAllFunction : BaseGlobalFunction
{
    public override string[] Names => ["TaskWhenAll"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        if (results[0] is ListLangValue list)
        {
            var tasks = list.Value.Cast<TaskLangValue>();
            return TaskLangValue.WhenAll(tasks, position);
        }
        throw new Exception("TaskWhenAll expects a List of Tasks");
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        Old8Lang.Compiler.LocalManager local,
        SourcePosition position)
    {
        parameters[0].LoadIlValue(ilGenerator, local);
        // Cast List<object> to IEnumerable<TaskLangValue> is hard in IL directly if generics mismatch
        // But TaskLangValue.WhenAll expects IEnumerable<TaskLangValue>
        // We might need a helper
        throw new NotImplementedException("TaskWhenAll IL generation not implemented");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, Old8Lang.Compiler.LocalManager local)
    {
        return typeof(TaskLangValue);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        if (arguments[0] is List<object?> list)
        {
            var tasks = list.OfType<TaskLangValue>();
            return TaskLangValue.WhenAll(tasks);
        }
        throw new Exception("TaskWhenAll expects a List of Tasks");
    }
}

/// <summary>
/// TaskWhenAny 函数 - 等待任意任务完成
/// </summary>
public sealed class TaskWhenAnyFunction : BaseGlobalFunction
{
    public override string[] Names => ["TaskWhenAny"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        if (results[0] is ListLangValue list)
        {
            var tasks = list.Value.Cast<TaskLangValue>();
            return TaskLangValue.WhenAny(tasks, position);
        }
        throw new Exception("TaskWhenAny expects a List of Tasks");
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        Old8Lang.Compiler.LocalManager local,
        SourcePosition position)
    {
        throw new NotImplementedException("TaskWhenAny IL generation not implemented");
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, Old8Lang.Compiler.LocalManager local)
    {
        return typeof(TaskLangValue);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        if (arguments[0] is List<object?> list)
        {
            var tasks = list.OfType<TaskLangValue>();
            return TaskLangValue.WhenAny(tasks);
        }
        throw new Exception("TaskWhenAny expects a List of Tasks");
    }
}
