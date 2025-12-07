using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ExprStatement(OldExpr expr) : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        expr.Run(Manager);
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        expr.LoadILValue(ilGenerator, local);
        // 对于表达式语句，我们不需要保留结果，所以如果有