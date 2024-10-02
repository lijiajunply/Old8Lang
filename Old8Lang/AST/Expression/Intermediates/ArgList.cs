namespace Old8Lang.AST.Expression;

public class ArgList(List<OldExpr> args) : OldExpr
{
    public List<OldExpr> Args => args;
}