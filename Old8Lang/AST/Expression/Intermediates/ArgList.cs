using Old8Lang.LangParser;
namespace Old8Lang.AST.Expression;

public class ArgList(List<OldExpr> args) : OldExpr
{
    public readonly List<OldExpr> Args = args;
}