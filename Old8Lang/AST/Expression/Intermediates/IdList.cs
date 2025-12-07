using Old8Lang.LangParser;
namespace Old8Lang.AST.Expression;

public class IdList(List<OldID> args) : OldExpr
{
    public readonly List<OldID> Args = args;
}