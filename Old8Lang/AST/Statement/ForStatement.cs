using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ForStatement(
    SetStatement setStatement,
    Operation expr,
    OldStatement statement,
    BlockStatement blockStatement)
    : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        Manager.AddChildren();
        setStatement.Run(Manager);
        while (true)
        {
            var varExpr = expr.Run(Manager);
            bool expr1;
            if (varExpr is BoolValue value)
                expr1 = value.Value;
            else
                break;
            if (expr1)
            {
                blockStatement.Run(Manager);
                statement.Run(Manager);
            }
            else
                break;
        }

        Manager.RemoveChildren();
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        setStatement.GenerateIL(ilGenerator, local);

        // 创建循环开始标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 跳转到循环开始
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件
        expr.LoadILValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd); // 如果 loopCounter >= 10，跳转到 loopEnd
        
        blockStatement.GenerateIL(ilGenerator, local);

        statement.GenerateIL(ilGenerator, local);

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到 loopStart

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"for({setStatement} ; {expr} ; {statement})");
        sb.Append("\n{" + blockStatement + "\n}");
        return sb.ToString();
    }
}