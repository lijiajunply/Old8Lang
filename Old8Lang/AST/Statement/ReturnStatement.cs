using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Statement;

public class ReturnStatement(OldExpr returnExpr, SourcePosition position = default) : OldStatement(position)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    public override void Run(VariateManager manager)
    {
        manager.Result = returnExpr.Run(manager);
        manager.IsReturn = true;
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
        {
            // 检查是否在finally块中使用了return语句，这在.NET IL中是不允许的
            if (local.IsInFinallyBlock)
            {
                throw new Error.CompilerException("在finally块中不允许使用return语句", Position);
            }
            
            // 确保返回表达式有值
            returnExpr.LoadIlValue(ilGenerator, local);
            
            ilGenerator.Emit(OpCodes.Ret);
        }

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public Type OutputType(LocalManager local) => returnExpr.OutputType(local)!;

    public override string ToString() => $"return {returnExpr}";
}