using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

/// <summary>
/// while语句
/// </summary>
public class WhileStatement(OldExpr expr, BlockStatement blockStatement) : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        Manager.AddChildren();
        while (true)
        {
            var value = expr.Run(Manager);
            bool expr1;
            if (value is BoolValue varBool)
            {
                expr1 = varBool.Value;
            }
            else
            {
                throw new Exception($"Type Error: {value} is not Bool");
            }

            if (expr1)
            {
                blockStatement.Run(Manager);
            }
            else
            {
                Manager.RemoveChildren();
                return;
            }
        }
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        // 创建循环开始标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 跳转到循环开始
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件
        expr.LoadILValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd); // 如果 loopCounter >= 10，跳转到 loopEnd
        
        blockStatement.GenerateIL(ilGenerator, local);

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到 loopStart

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);
    }

    public override string ToString() => $"while({expr}){blockStatement}";
}