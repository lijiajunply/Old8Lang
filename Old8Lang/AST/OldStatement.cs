using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST;

public abstract class OldStatement : OldLangTree
{
    public abstract void Run(Old8Lang.LangParser.VariateManager Manager);

    public abstract void GenerateIL(ILGenerator ilGenerator, LocalManager local);

    public abstract OldStatement? this[int index] { get; }
    public abstract int Count { get; }
}