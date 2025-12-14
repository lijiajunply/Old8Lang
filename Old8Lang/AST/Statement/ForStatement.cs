using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class ForStatement(
    SetStatement setStatement,
    LangExpression expression,
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
            var varExpr = expression.Run(manager);
            bool expr1;
            if (varExpr is BoolLangValue value)
            {
                expr1 = value.Value;
                // 优化：将临时布尔对象归还到对象池
                value.ReturnToPool();
            }
            else
                throw new TypeError(this, "期望布尔类型", $"实际得到了 {varExpr.GetType().Name}");
            
            if (expr1)
            {
                // 使用标志位替代异常处理，减少性能开销
                manager.BreakFlag = false;
                manager.ContinueFlag = false;
                
                blockStatement.Run(manager);
                
                // 处理break
                if (manager.BreakFlag)
                {
                    break;
                }
                
                // 处理continue，执行循环增量操作
                if (manager.ContinueFlag)
                {
                    statement.Run(manager);
                    continue;
                }
                
                // 正常执行，执行循环增量操作
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

        // 创建循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签，以便嵌套循环使用
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;
        
        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = continueLabel;

        // 跳转到循环开始
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件
        expression.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd); // 如果条件为false，跳转到循环结束
        
        blockStatement.GenerateIl(ilGenerator, local);

        // continue标签：执行循环迭代语句
        ilGenerator.MarkLabel(continueLabel);
        statement.GenerateIl(ilGenerator, local);

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到循环开始

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);
        
        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    }

    public override OldStatement this[int index] => blockStatement[index];

    public override int Count => blockStatement.Count;

    public override string ToString()
    {
        var sb = new StringBuilder($"for {setStatement}, {expression}, {statement}");
        sb.AppendLine();
        sb.Append($"{{{blockStatement}\n}}");
        return sb.ToString();
    }
}