using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class SetListStatement(List<OldID> ids, List<OldExpr> expr) : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        var results = expr.Select(item => item.Run(Manager)).ToList();

        for (var i = 0; i < results.Count; i++)
        {
            Manager.Set(ids[i], results[i]);
        }
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        for (var i = 0; i < ids.Count; i++)
        {
            var Value = expr[i];
            var Id = ids[i];
            Value.SetValueToIL(ilGenerator, local, Id.IdName);
        }
    }

    public override OldStatement this[int index] => this;
    public override int Count => 0;
}