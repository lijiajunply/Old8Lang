using Old8Lang;

namespace Old8Lang.AST.Expression.Intermediates;

public class IdList(List<OldId> args, SourcePosition position = default) : OldExpr(position)
{
    public readonly List<OldId> Args = args;
}