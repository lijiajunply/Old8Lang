using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ClassInit(AnyValue anyValue) : OldStatement
{
    public override void Run(VariateManager Manager) => Manager.AddClassAndFunc(anyValue);

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        anyValue.LoadILValue(ilGenerator, local);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => anyValue.ToString();
}