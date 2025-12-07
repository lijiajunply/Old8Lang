using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST;

public abstract class OldStatement : IOldLangTree
{
    public abstract void Run(LangParser.VariateManager manager);

    public abstract void GenerateIl(ILGenerator ilGenerator, LocalManager local);

    public abstract OldStatement? this[int index] { get; }
    public abstract int Count { get; }
}