namespace Old8Lang.AST.Expression.Intermediates;

public class ArgList(List<OldExpr> args, SourcePosition position = default) : OldExpr(position)
{
    public readonly List<OldExpr> Args = args;
}