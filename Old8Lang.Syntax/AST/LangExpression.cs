using Old8Lang.AST.Visitor;

namespace Old8Lang.AST;

public abstract class LangExpression : IOldLangTree
{
    /// <inheritdoc />
    public SourcePosition Position { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">源代码位置信息</param>
    protected LangExpression(SourcePosition position = default)
    {
        Position = position;
    }

    /// <inheritdoc />
    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);
}
