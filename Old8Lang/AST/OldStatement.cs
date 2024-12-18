using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST;

public abstract class OldStatement : OldLangTree
{
    public abstract void Run(VariateManager Manager);

    public abstract void GenerateIL(ILGenerator ilGenerator, LocalManager local);

    public abstract OldStatement? this[int index] { get; }
    public abstract int Count { get; }
}