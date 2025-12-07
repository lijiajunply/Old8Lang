namespace Old8Lang.AST.Expression.Intermediates;

public class IdList(List<OldId> args) : OldExpr
{
    public readonly List<OldId> Args = args;
}