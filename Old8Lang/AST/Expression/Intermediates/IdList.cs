namespace Old8Lang.AST.Expression.Intermediates;

public class IdList(List<LangId> args, SourcePosition position = default) : OldExpr(position)
{
    public readonly List<LangId> Args = args;

    public override string ToString() => string.Join(", ", Args); // Old8Lang 风格的 ID 列表
}