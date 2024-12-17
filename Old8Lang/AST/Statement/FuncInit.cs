using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class FuncInit(FuncValue a) : OldStatement
{
    public readonly FuncValue FuncValue = a;

    public override void Run(VariateManager Manager)
    {
        Manager.AddClassAndFunc(FuncValue);
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedException();
    }

    public override string ToString() => FuncValue.ToString();
}