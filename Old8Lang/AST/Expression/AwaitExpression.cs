using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// await 表达式
/// 用于等待异步操作完成并获取结果
/// </summary>
public partial class AwaitExpression : LangExpression
{
    public readonly LangExpression Expression;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AwaitExpression(LangExpression expression, SourcePosition position = default)
        : base(position)
    {
        Expression = expression;
    }

    /// <summary>
    /// 解释执行：等待 Task 完成并返回结果
    /// </summary>
    public override LangValueType Run(VariateManager manager)
    {
        // 执行表达式，期望得到 TaskLangValue
        var result = Expression.Run(manager);

        if (result is not TaskLangValue taskValue)
        {
            throw new TypeError(
                this,
                $"await 只能用于 Task 类型，实际类型为 {result.TypeToString()}"
            );
        }

        // 检查任务是否已完成，如果已完成直接返回结果
        if (taskValue.IsCompleted)
        {
            if (taskValue.Exception is not null)
            {
                throw taskValue.Exception;
            }

            return taskValue.Result!;
        }

        // 对于未完成的任务，直接异步等待并获取结果
        // TaskLangValue.AwaitAsync() 内部已经处理了线程安全
        try
        {
            var awaitTask = taskValue.AwaitAsync();
            var taskResult = awaitTask.GetAwaiter().GetResult();
            return taskResult;
        }
        catch (AggregateException aggEx)
        {
            // 展开聚合异常，抛出内部异常
            throw aggEx.InnerException ?? aggEx;
        }
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式）
    /// 实现真正的异步等待，使用 .NET 的 await 模式
    /// 支持 Task 和 Task&lt;object&gt; 两种类型
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 加载表达式的值
        Expression.LoadIlValue(ilGenerator, local);

        // 获取表达式的输出类型
        var exprType = Expression.OutputType(local);
        
        // 检查是否在异步状态机中
        if (local.AsyncStateMachineGenerator != null && Old8Lang.Compiler.Compiler.EnableAsyncStateMachineAwait)
        {
            GenerateAsyncAwait(ilGenerator, local, exprType);
            return;
        }

        // --- 同步等待逻辑 (不在异步函数中) ---

        // 确保表达式类型是 Task 或 Task<object>
        if (exprType == typeof(Task))
        {
            // 对于 Task 类型，简化处理：直接调用 GetAwaiter() 和 GetResult()
            // Task.GetAwaiter() 返回 TaskAwaiter（非泛型）
            // 我们需要等待任务完成，然后返回 null

            // 栈上现在是 Task 对象
            // 保存 Task 到局部变量
            var taskLocal = ilGenerator.DeclareLocal(typeof(Task));
            ilGenerator.Emit(OpCodes.Stloc, taskLocal);

            // 加载 Task，调用 GetAwaiter()
            ilGenerator.Emit(OpCodes.Ldloc, taskLocal);
            var taskGetAwaiterMethod = typeof(Task).GetMethod("GetAwaiter")!;
            ilGenerator.Emit(OpCodes.Callvirt, taskGetAwaiterMethod);

            // 获取 TaskAwaiter（非泛型）
            var taskAwaiterType = taskGetAwaiterMethod.ReturnType;
            var taskAwaiterLocal = ilGenerator.DeclareLocal(taskAwaiterType);
            ilGenerator.Emit(OpCodes.Stloc, taskAwaiterLocal);

            // 调用 GetResult() 等待完成（同步等待）
            ilGenerator.Emit(OpCodes.Ldloca, taskAwaiterLocal);
            var taskGetResultMethod = taskAwaiterType.GetMethod("GetResult")!;
            ilGenerator.Emit(OpCodes.Call, taskGetResultMethod);

            // Task.GetResult() 返回 void，我们需要返回 null（object）
            ilGenerator.Emit(OpCodes.Ldnull);

            // 直接返回，跳过后续的 Task<object> 处理
            return;
        }

        // 现在栈上是 Task<object>，调用 GetAwaiter() 方法获取等待器
        // 注意：这里假设如果是泛型 Task，一定是 Task<object>，或者至少有 GetAwaiter
        // 如果是 Task<T>，GetAwaiter 返回 TaskAwaiter<T>
        var getAwaiterMethod = exprType.GetMethod("GetAwaiter")!;
        ilGenerator.Emit(OpCodes.Callvirt, getAwaiterMethod);

        // 获取等待器类型
        var awaiterType = getAwaiterMethod.ReturnType;

        // 将等待器保存到局部变量
        var awaiterLocal = ilGenerator.DeclareLocal(awaiterType);
        ilGenerator.Emit(OpCodes.Stloc, awaiterLocal);

        // 检查等待器是否已完成
        var isCompletedProperty = awaiterType.GetProperty("IsCompleted")!;
        var isCompletedGetMethod = isCompletedProperty.GetGetMethod()!;

        // 定义标签
        var completedLabel = ilGenerator.DefineLabel();
        var endLabel = ilGenerator.DefineLabel();

        // 加载等待器地址，调用 IsCompleted 属性
        // TaskAwaiter<T> 是结构体，需要使用 Ldloca 和 Call
        ilGenerator.Emit(OpCodes.Ldloca, awaiterLocal);
        ilGenerator.Emit(OpCodes.Call, isCompletedGetMethod);

        // 如果已完成，跳转到获取结果
        ilGenerator.Emit(OpCodes.Brtrue_S, completedLabel);

        // 未完成，这里需要异步状态机支持
        // 目前生成同步等待代码，后续将替换为异步状态机切换

        // 加载等待器地址，调用 GetResult() 方法获取结果（同步等待，直到任务完成）
        ilGenerator.MarkLabel(completedLabel);
        ilGenerator.Emit(OpCodes.Ldloca, awaiterLocal);
        var getResultMethod = awaiterType.GetMethod("GetResult")!;
        ilGenerator.Emit(OpCodes.Call, getResultMethod);

        // 结束
        ilGenerator.MarkLabel(endLabel);
    }

    /// <summary>
    /// 生成异步状态机相关的等待代码
    /// </summary>
    private void GenerateAsyncAwait(ILGenerator il, LocalManager local, Type exprType)
    {
        var generator = local.AsyncStateMachineGenerator!;
        var stateIndex = generator.GetStateIndex(this);
        
        // 1. 获取 Awaiter
        // 栈上已有 Task 对象
        
        // 如果类型是 object，尝试转换为 Task<object>
        if (exprType == typeof(object))
        {
            il.Emit(OpCodes.Castclass, typeof(Task<object>));
            exprType = typeof(Task<object>);
        }

        var getAwaiterMethod = exprType.GetMethod("GetAwaiter")!;
        if (getAwaiterMethod == null) throw new InvalidOperationException($"Cannot find GetAwaiter on {exprType.FullName}");
        
        il.Emit(OpCodes.Callvirt, getAwaiterMethod);
        
        var awaiterType = getAwaiterMethod.ReturnType;
        var awaiterLocal = il.DeclareLocal(awaiterType);
        il.Emit(OpCodes.Stloc, awaiterLocal);
        
        // 2. 检查 IsCompleted
        var isCompletedProperty = awaiterType.GetProperty("IsCompleted")!;
        if (isCompletedProperty == null) throw new InvalidOperationException($"Cannot find IsCompleted property on {awaiterType.FullName}");
        
        var isCompletedGetMethod = isCompletedProperty.GetGetMethod() ?? awaiterType.GetMethod("get_IsCompleted");
        
        if (isCompletedGetMethod == null)
        {
             // Fallback: try to find on interface or explicit impl?
             // TaskAwaiter struct has public IsCompleted.
             throw new InvalidOperationException($"Cannot find IsCompleted getter on {awaiterType.FullName}");
        }
        
        il.Emit(OpCodes.Ldloca, awaiterLocal);
        il.Emit(OpCodes.Call, isCompletedGetMethod);
        
        var fastPathLabel = il.DefineLabel();
        il.Emit(OpCodes.Brtrue, fastPathLabel);
        
        // 3. 挂起 (Yield)
        generator.EmitAwaitYield(il, stateIndex, awaiterLocal);
        
        // 4. 恢复 (Resume) - 也是 Switch 的跳转目标
        generator.EmitAwaitResume(il, stateIndex, awaiterLocal);
        
        // 5. 快速路径 / 恢复后执行
        il.MarkLabel(fastPathLabel);
        
        // 6. 获取结果
        il.Emit(OpCodes.Ldloca, awaiterLocal);
        var getResultMethod = awaiterType.GetMethod("GetResult")!;
        if (getResultMethod == null) throw new InvalidOperationException($"Cannot find GetResult on {awaiterType.FullName}");
        
        il.Emit(OpCodes.Call, getResultMethod);
        
        // 如果 GetResult 返回 void (例如 Task)，我们需要压入 null 以保持表达式值的一致性
        if (getResultMethod.ReturnType == typeof(void))
        {
            il.Emit(OpCodes.Ldnull);
        }
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        // await Task<T> 返回 T
        var exprType = Expression.OutputType(local);
        if (exprType == typeof(Task)) return typeof(object); // Task 返回 void -> null
        
        // 检查是否是泛型 Task<T>
        if (exprType.IsGenericType && exprType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return exprType.GetGenericArguments()[0];
        }
        
        // 鸭子类型：查找 GetAwaiter().GetResult() 返回类型
        var getAwaiter = exprType.GetMethod("GetAwaiter");
        if (getAwaiter != null)
        {
            var awaiterType = getAwaiter.ReturnType;
            var getResult = awaiterType.GetMethod("GetResult");
            if (getResult != null)
            {
                var resultType = getResult.ReturnType;
                if (resultType == typeof(void)) return typeof(object);
                return resultType;
            }
        }
        
        return typeof(object);
    }
}
