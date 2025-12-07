using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang;

namespace Old8Lang.AST.Statement;

public class ForStatement(
    SetStatement setStatement,
    OldExpr expr,
    OldStatement statement,
    BlockStatement blockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        manager.AddChildren();
        setStatement.Run(manager);
        while (true)
        {
            var varExpr = expr.Run(manager);
            bool expr1;
            if (varExpr is BoolValue value)
                expr1 = value.Value;
            else
                throw new TypeError(this, "期望布尔类型", $"实际得到了 {varExpr.GetType().Name}");
            if (expr1)
            {
                blockStatement.Run(manager);
                statement.Run(manager);
            }
            else
                break;
        }

        manager.RemoveChildren();
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        setStatement.GenerateIl(ilGenerator, local);

        // 创建循环开始标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 跳转到循环开始
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件
        expr.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd); // 如果 loopCounter >= 10，跳转到 loopEnd
        
        blockStatement.GenerateIl(ilGenerator, local);

        statement.GenerateIl(ilGenerator, local);

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到 loopStart

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);
    }

    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;

    public override string ToString()
    {
        var sb = new StringBuilder($"for({setStatement} ; {expr} ; {statement})");
        sb.Append("\n{" + blockStatement + "\n}");
        return sb.ToString();
    }
}