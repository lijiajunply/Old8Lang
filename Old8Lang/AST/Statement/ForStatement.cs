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
        var loopCounter = ilGenerator.DeclareLocal(typeof(int)); // 循环计数器

        // 初始化循环计数器
        ilGenerator.Emit(OpCodes.Ldc_I4_0); // 加载常量 0
        ilGenerator.Emit(OpCodes.Stloc, loopCounter.LocalIndex); // 存储到 loopCounter

        // 创建循环开始标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 跳转到循环开始
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件 (loopCounter < 10)
        ilGenerator.Emit(OpCodes.Ldloc, loopCounter.LocalIndex); // 加载 loopCounter
        ilGenerator.Emit(OpCodes.Ldc_I4, 10); // 加载常量 10
        ilGenerator.Emit(OpCodes.Bge, loopEnd); // 如果 loopCounter >= 10，跳转到 loopEnd

        // 打印当前计数器值
        ilGenerator.Emit(OpCodes.Ldloc, loopCounter.LocalIndex); // 加载 loopCounter
        statement.GenerateIL(ilGenerator, local);

        // 增加循环计数器
        ilGenerator.Emit(OpCodes.Ldloc, loopCounter.LocalIndex); // 加载 loopCounter
        ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载常量 1
        ilGenerator.Emit(OpCodes.Add); // 执行加法
        ilGenerator.Emit(OpCodes.Stloc, loopCounter.LocalIndex); // 存储回 loopCounter

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到 loopStart

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);

        // 完成方法
        ilGenerator.Emit(OpCodes.Ret);
    }

    public override string ToString()
    {
        var sb = new StringBuilder($"for({setStatement} ; {expr} ; {statement})");
        sb.Append("\n{" + blockStatement + "\n}");
        return sb.ToString();
    }
}