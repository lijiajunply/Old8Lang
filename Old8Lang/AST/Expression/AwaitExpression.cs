using Old8Lang.AST;
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

        // 同步等待 Task 完成
        return taskValue.Await();
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式暂不支持）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedError(Position, "编译模式暂不支持 await");
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type? OutputType(LocalManager local)
    {
        // await Task<T> 返回 T
        return typeof(object); // 简化处理
    }
}
