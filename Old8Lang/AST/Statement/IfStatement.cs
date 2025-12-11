using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler;


namespace Old8Lang.AST.Statement;

/// <summary>
/// if语句
/// </summary>
public class IfStatement(
    OldIf ifBlock,
    List<OldIf?> elifBlock,
    BlockStatement? elseBlockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    public override void Run(VariateManager manager)
    {
        var r = true;
        manager.AddChildren();
        ifBlock.Run(manager, ref r);
        manager.RemoveChildren();
        foreach (var variable in elifBlock.OfType<OldIf>())
        {
            manager.AddChildren();
            variable.Run(manager, ref r);
            manager.RemoveChildren();
        }

        if (r)
            elseBlockStatement?.Run(manager);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var labelElse = ilGenerator.DefineLabel();
        var labelEnd = ilGenerator.DefineLabel();

        // 处理 if 块
        ifBlock.GenerateConditionIl(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, labelElse);

        // if 部分
        ifBlock.GenerateIl(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Br, labelEnd);

        // 处理 elif 块
        ilGenerator.MarkLabel(labelElse);
        foreach (var elif in elifBlock.OfType<OldIf>())
        {
            var nextElif = ilGenerator.DefineLabel();
            elif.GenerateConditionIl(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Brfalse, nextElif);

            // elif 部分
            elif.GenerateIl(ilGenerator, local);
            ilGenerator.Emit(OpCodes.Br, labelEnd);

            ilGenerator.MarkLabel(nextElif);
        }

        // 处理 else 块
        elseBlockStatement?.GenerateIl(ilGenerator, local);

        // 结束标签
        ilGenerator.MarkLabel(labelEnd);
    }

    public override OldStatement? this[int index]
    {
        get
        {
            if (index == 0)
            {
                return ifBlock;
            }

            if (index == elifBlock.Count)
            {
                return elseBlockStatement;
            }

            return elifBlock[index];
        }
    }

    public override int Count => 1 + elifBlock.Count + (elseBlockStatement == null ? 0 : 1);

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"if {ifBlock}");
        foreach (var elif in elifBlock.OfType<OldIf>())
        {
            sb.AppendLine($"elif {elif}");
        }

        if (elseBlockStatement != null)
        {
            sb.AppendLine($"else {elseBlockStatement}");
        }

        return sb.ToString();
    }
}