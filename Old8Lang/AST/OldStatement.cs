using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST;

public abstract class OldStatement : OldLangTree
{
    public virtual void Run(VariateManager Manager){}
    public abstract void GenerateIL(ILGenerator ilGenerator,LocalManager local);
}