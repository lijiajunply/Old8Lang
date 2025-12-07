using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Statement;

public class SetStatement(OldId id, OldExpr value, SourcePosition position = default) : OldStatement(position)
{
    public readonly OldId Id = id;
    public readonly OldExpr Value = value;

    public override void Run(VariateManager manager)
    {
        var result = Value.Run(manager);
        manager.Set(Id, result);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        Value.SetValueToIl(ilGenerator, local,Id.IdName);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"var {Id} = {Value};";
}