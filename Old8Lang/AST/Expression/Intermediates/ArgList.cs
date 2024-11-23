namespace Old8Lang.AST.Expression;

public class ArgList(List<OldExpr> args) : OldExpr
{
    public readonly List<OldExpr> Args = args;
}