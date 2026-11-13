using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// yield语句，用于生成器函数中暂停执行并返回值
/// </summary>
public partial class YieldStatement(LangExpression yieldExpression, SourcePosition position = default) : OldStatement(position)
{

    /// <summary>
    /// yield表达式
    /// </summary>
    public LangExpression YieldExpression { get; init; } = yieldExpression;

    /// <summary>
    /// 获取子语句
    /// </summary>
    /// <param name="index">索引</param>
    /// <returns>子语句</returns>
    public override OldStatement? this[int index] => null;

    /// <summary>
    /// 获取语句数量
    /// </summary>
    public override int Count => 0;

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>yield语句的字符串表示</returns>
    public override string ToString() => $"yield {YieldExpression}";
}