using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

public partial class SwitchStatement(
    LangExpression switchExpression,
    List<CaseStatement> switchCaseList,
    BlockStatement? defaultBlockStatement = null,
    SourcePosition position = default)
    : OldStatement(position)
{
    internal LangExpression SwitchExpression => switchExpression;
    internal List<CaseStatement> SwitchCaseList => switchCaseList;
    internal BlockStatement? DefaultBlockStatement => defaultBlockStatement;

    internal bool TryGenerateIntSwitch(ILGenerator ilGenerator, LocalBuilder switchValueLocal, Label defaultLabel,
        List<Label> caseLabels)
    {
        // 1. 检查是否所有 case 都是整数常量
        var caseValues = new List<(int Value, int Index)>();
        for (int i = 0; i < switchCaseList.Count; i++)
        {
            var oldCase = switchCaseList[i];
            if (oldCase.Expression is IntLangValue intVal)
            {
                caseValues.Add((intVal.Value, i));
            }
            else
            {
                // 存在非整数常量，无法优化为 switch 指令
                return false;
            }
        }

        if (caseValues.Count == 0) return false;

        // 2. 分析值的范围和密度
        int min = caseValues.Min(x => x.Value);
        int max = caseValues.Max(x => x.Value);
        long range = (long)max - min + 1;

        // 如果范围过大（超过 2048）或密度过低（小于 50%），则不使用 switch 指令
        // 2048 是一个折中的值，保证跳转表不会过大
        if (range > 2048 || (double)caseValues.Count / range < 0.5)
        {
            return false;
        }

        // 3. 构建跳转表
        var jumpTable = new Label[range];
        // 初始化所有项为 defaultLabel
        for (int i = 0; i < range; i++)
        {
            jumpTable[i] = defaultLabel;
        }

        // 填充 case 标签
        // 注意：如果有重复的 case 值，后面的会覆盖前面的（这与 C# switch 行为一致，或者是编译错误，但在 AST 层面我们只生成代码）
        // Old8Lang 目前没有明确禁止重复 case，运行时行为是匹配第一个。
        // 为了保持一致性，我们应该只处理第一个出现的 case 值，忽略后续重复的。
        var processedValues = new HashSet<int>();
        foreach (var (val, index) in caseValues)
        {
            if (processedValues.Add(val))
            {
                jumpTable[val - min] = caseLabels[index];
            }
        }

        // 4. 生成 IL
        // 加载 switch 值
        ilGenerator.Emit(OpCodes.Ldloc, switchValueLocal);
        // 减去最小值
        ilGenerator.Emit(OpCodes.Ldc_I4, min);
        ilGenerator.Emit(OpCodes.Sub);
        // 生成 switch 指令
        ilGenerator.Emit(OpCodes.Switch, jumpTable);
        // 如果不在范围内，跳转到 default
        ilGenerator.Emit(OpCodes.Br, defaultLabel);

        return true;
    }

    public override OldStatement this[int index] => switchCaseList[index];

    public override int Count => switchCaseList.Count;
}

public partial class CaseStatement(
    LangExpression expression,
    BlockStatement blockStatement,
    SourcePosition position = default)
    : OldStatement(position)
{

    public LangExpression Expression { get; } = expression;
    public BlockStatement BlockStatement { get; } = blockStatement;

    public override OldStatement this[int index] => BlockStatement[index];

    public override int Count => BlockStatement.Count;
}