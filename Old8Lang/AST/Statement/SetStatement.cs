using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;
using ValueType = Old8Lang.AST.Expression.ValueType;

namespace Old8Lang.AST.Statement;

public class SetStatement(OldID id, OldExpr value) : OldStatement
{
    public readonly OldID Id = id;
    public readonly OldExpr Value = value;

    public override void Run(VariateManager Manager)
    {
        var result = Value.Run(Manager);
        Manager.Set(Id, result);
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        Value.SetValueToIL(ilGenerator, local,Id.IdName);
    }

    public override string ToString() => $"var {Id} = {Value};";
}