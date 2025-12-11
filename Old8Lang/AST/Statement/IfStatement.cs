using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler;


namespace Old8Lang.AST.Statement;

/// <summary>
/// if语句
/// </summary>
public class IfStatement(
    IfChild ifChildBlock,
    List<IfChild?> elifBlock,
    BlockStatement? elseBlockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    public override void Run(VariateManager manager)
    {
        var r = true;
        manager.AddChildren();
        ifChildBlock.Run(manager, ref r);
        manager.RemoveChildren();
        foreach (var variable in elifBlock.OfType<IfChild>())
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
        ifChildBlock.GenerateConditionIl(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, labelElse);

        // if 部分
        ifChildBlock.GenerateIl(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Br, labelEnd);

        // 处理 elif 块
        ilGenerator.MarkLabel(labelElse);
        foreach (var elif in elifBlock.OfType<IfChild>())
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
                return ifChildBlock;
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
        sb.AppendLine($"if {ifChildBlock}");
        foreach (var elif in elifBlock.OfType<IfChild>())
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