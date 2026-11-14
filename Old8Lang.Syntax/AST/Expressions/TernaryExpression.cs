using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 三元表达式
/// 语法：expression if condition else expression
/// </summary>
public partial class TernaryExpression(
    LangExpression condition,
    LangExpression trueExpression,
    LangExpression falseExpression,
    SourcePosition position = default)
    : LangExpression(position)
{
    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitTernaryExpression(this);


    
    /// <summary>
    /// 条件表达式
    /// </summary>
    public LangExpression Condition { get; } = condition;

    /// <summary>
    /// 条件为真时执行的表达式
    /// </summary>
    public LangExpression TrueExpression { get; } = trueExpression;

    /// <summary>
    /// 条件为假时执行的表达式
    /// </summary>
    public LangExpression FalseExpression { get; } = falseExpression;

    /// <summary>
    /// 将三元表达式转换为字符串表示
    /// </summary>
    /// <returns>三元表达式的字符串表示，格式为"condition ? trueExpression : falseExpression"</returns>
    public override string ToString()
    {
        return $"{Condition} ? {TrueExpression} : {FalseExpression}";
    }
}