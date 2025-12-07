using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Statement;

public class SetListStatement(List<OldId> ids, List<OldExpr> expr, SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        var results = expr.Select(item => item.Run(manager)).ToList();

        for (var i = 0; i < results.Count; i++)
        {
            manager.Set(ids[i], results[i]);
        }
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        for (var i = 0; i < ids.Count; i++)
        {
            var value = expr[i];
            var id = ids[i];
            value.SetValueToIl(ilGenerator, local, id.IdName);
        }
    }

    public override OldStatement this[int index] => this;
    public override int Count => 0;
}