namespace Old8Lang.AST.Expression;

public class IdList(List<OldID> args) : OldExpr
{
    public List<OldID> Args => args;
}