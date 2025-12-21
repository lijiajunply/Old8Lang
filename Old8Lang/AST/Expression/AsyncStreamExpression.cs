using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using System.Reflection.Emit;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 异步流表达式
/// 表示 async { block } 语法，创建一个异步流（异步生成器）
/// </summary>
public class AsyncStreamExpression : LangExpression
{
    private readonly BlockStatement Block;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="block">异步流体</param>
    /// <param name="position">源代码位置</param>
    public AsyncStreamExpression(BlockStatement block, SourcePosition position = default)
        : base(position)
    {
        Block = block;
    }

    /// <summary>
    /// 解释执行：创建并返回异步流值
    /// </summary>
    public override LangValueType Run(VariateManager manager)
    {
        // 创建一个异步函数来包装 async { } 块
        // 将块语句包装为异步函数：async () -> { block }
        var asyncFunc = new AsyncFuncLangValue(
            id: null,
            ids: new List<LangId>(), // 无参数
            blockStatement: Block,
            position: Position
        );

        // 运行异步函数以捕获闭包
        // 对于包含 yield 的异步函数，Run() 会返回 AsyncGeneratorLangValue
        var result = asyncFunc.Run(manager);

        // 如果返回的是 AsyncGeneratorLangValue，直接包装为 AsyncStreamLangValue
        if (result is AsyncGeneratorLangValue asyncGen)
        {
            return new AsyncStreamLangValue(asyncGen, Position);
        }

        // 如果不是生成器（不包含 yield），仍然包装为 AsyncStreamLangValue
        // 但需要传递 AsyncFuncLangValue
        if (result is AsyncFuncLangValue asyncFuncValue)
        {
            return new AsyncStreamLangValue(asyncFuncValue, Position);
        }

        // 不应该到达这里
        throw new InvalidOperationException(
            $"Unexpected result type from AsyncFuncLangValue.Run(): {result.GetType().Name}");
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 对于异步流，生成类似于异步函数的 IL 代码
        // 目前实现一个简化版本
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        return typeof(AsyncStreamLangValue);
    }

    public override string ToString()
    {
        return $"async {{ {Block} }}";
    }
}