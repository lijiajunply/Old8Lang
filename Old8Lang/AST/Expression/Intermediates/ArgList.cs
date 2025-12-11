namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 参数列表
/// </summary>
/// <param name="args"></param>
/// <param name="position"></param>
public class ArgList(List<OldExpr> args, SourcePosition position = default) : OldExpr(position)
{
    public readonly List<OldExpr> Args = args;

    public override string ToString() => string.Join(", ", Args); // Old8Lang 风格的参数列表
    
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
}