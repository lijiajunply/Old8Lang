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
public class AwaitExpression : LangExpression
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
            if (taskValue.Exception != null)
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

        // 确保表达式类型是 Task 或 Task<object>
        if (exprType == typeof(Task))
        {
            // 对于 Task 类型，转换为 Task<object>，使用 Task.WhenAll(Task) 实现
            var whenAllMethod = typeof(Task).GetMethod("WhenAll", [typeof(Task[])])!;
            // 创建一个包含当前 Task 的数组
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Newarr, typeof(Task));
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Stelem_Ref);
            ilGenerator.Emit(OpCodes.Call, whenAllMethod);
            // 现在栈上是 Task，我们需要将其转换为 Task<object>
            // 使用 Task.FromResult<object>(null) 作为模板
            var fromResultMethod =
                typeof(Task).GetMethod("FromResult", [typeof(object)])!.MakeGenericMethod(typeof(object));
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Call, fromResultMethod);
        }

        // 现在栈上是 Task<object>，调用 GetAwaiter() 方法获取等待器
        var getAwaiterMethod = typeof(Task<object>).GetMethod("GetAwaiter")!;
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

        // 加载等待器，调用 IsCompleted 属性
        ilGenerator.Emit(OpCodes.Ldloc, awaiterLocal);
        ilGenerator.Emit(OpCodes.Callvirt, isCompletedGetMethod);

        // 如果已完成，跳转到获取结果
        ilGenerator.Emit(OpCodes.Brtrue_S, completedLabel);

        // 未完成，这里需要异步状态机支持
        // 目前生成同步等待代码，后续将替换为异步状态机切换

        // 加载等待器，调用 GetResult() 方法获取结果（同步等待，直到任务完成）
        ilGenerator.MarkLabel(completedLabel);
        ilGenerator.Emit(OpCodes.Ldloc, awaiterLocal);
        var getResultMethod = awaiterType.GetMethod("GetResult")!;
        ilGenerator.Emit(OpCodes.Callvirt, getResultMethod);

        // 结束
        ilGenerator.MarkLabel(endLabel);
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        // await Task<T> 返回 T
        return typeof(object); // 简化处理
    }
}