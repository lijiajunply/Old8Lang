using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Reflection.Emit;

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
            return taskValue.AwaitAsync().GetAwaiter().GetResult();
        }
        catch (AggregateException aggEx)
        {
            throw aggEx.InnerException ?? aggEx;
        }
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 加载表达式的值（应该是Task<object>类型）
        Expression.LoadIlValue(ilGenerator, local);
        
        // 调用GetAwaiter()方法获取等待器
        ilGenerator.Emit(OpCodes.Callvirt, typeof(Task<object>).GetMethod("GetAwaiter")!);
        
        // 获取等待器的结果类型
        var awaiterType = typeof(Task<object>).GetMethod("GetAwaiter")!.ReturnType;
        
        // 调用GetResult()方法获取结果
        ilGenerator.Emit(OpCodes.Callvirt, awaiterType.GetMethod("GetResult")!);
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
