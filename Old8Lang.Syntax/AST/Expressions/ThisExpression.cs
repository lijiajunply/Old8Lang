using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// This表达式类，用于表示当前类实例的引用
/// </summary>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <remarks>
/// 该类用于处理this关键字，只能在类的实例方法中使用。
/// this关键字引用当前对象实例，允许访问实例的字段和方法。
/// </remarks>
public partial class ThisExpression(SourcePosition position = default) : LangExpression(position)
{
    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitThisExpression(this);


    public override string ToString()
    {
        return "this";
    }
}
