namespace Old8Lang.AST.Expression.Intermediates;

public class ArgList(List<OldExpr> args) : OldExpr
{
    public readonly List<OldExpr> Args = args;
}