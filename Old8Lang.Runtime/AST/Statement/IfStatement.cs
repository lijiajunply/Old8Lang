using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;


namespace Old8Lang.AST.Statement;

/// <summary>
/// if语句
/// </summary>
public partial class IfStatement(
    IfChild ifChildBlock,
    List<IfChild?> elifBlock,
    BlockStatement? elseBlockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{
    public IEnumerable<IfChild> Children
    {
        get
        {
            yield return ifChildBlock;
            foreach (var item in elifBlock.OfType<IfChild>())
            {
                yield return item;
            }
        }
    }

    public BlockStatement? ElseBlock => elseBlockStatement;

    public override void Run(VariateManager manager)
    {
        var r = true;

        // 保存原始的 IsFunc 状态
        var originalIsFunc = manager.IsFunc;

        // 处理 if 块
        manager.AddChildren();
        // 在 if 语句块中，临时禁用函数上下文，允许修改外部变量
        manager.IsFunc = false;
        ifChildBlock.Run(manager, ref r);
        manager.RemoveChildren();

        // 处理 elif 块
        foreach (var variable in elifBlock.OfType<IfChild>())
        {
            manager.AddChildren();
            // 在 elif 语句块中，临时禁用函数上下文，允许修改外部变量
            manager.IsFunc = false;
            variable.Run(manager, ref r);
            manager.RemoveChildren();
        }

        // 恢复原始的 IsFunc 状态
        manager.IsFunc = originalIsFunc;

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

            if (index == 1 + elifBlock.Count)
            {
                return elseBlockStatement;
            }

            return elifBlock[index - 1];
        }
    }

    public override int Count => 1 + elifBlock.Count + (elseBlockStatement is null ? 0 : 1);

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"if {ifChildBlock}");
        foreach (var elif in elifBlock.OfType<IfChild>())
        {
            sb.AppendLine($"elif {elif}");
        }

        if (elseBlockStatement is not null)
        {
            sb.AppendLine($"else {elseBlockStatement}");
        }

        return sb.ToString();
    }
}