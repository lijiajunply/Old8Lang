using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 三元表达式
/// 语法：expression if condition else expression
/// </summary>
public class TernaryExpression(
    OldExpr condition,
    OldExpr trueExpression,
    OldExpr falseExpression,
    SourcePosition position = default)
    : OldExpr(position)
{
    /// <summary>
    /// 条件表达式
    /// </summary>
    public OldExpr Condition { get; } = condition;

    /// <summary>
    /// 条件为真时执行的表达式
    /// </summary>
    public OldExpr TrueExpression { get; } = trueExpression;

    /// <summary>
    /// 条件为假时执行的表达式
    /// </summary>
    public OldExpr FalseExpression { get; } = falseExpression;

    public override LangValueType Run(VariateManager manager)
    {
        // 执行条件判断
        var conditionValue = Condition.Run(manager) as BoolLangValue ??
                             throw new InvalidOperationError(
                                 this,
                                 "三元条件表达式的条件必须是Bool类型");

        // 根据条件结果返回相应的表达式值
        return conditionValue.Value
            ? TrueExpression.Run(manager)
            : FalseExpression.Run(manager);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 条件表达式
        Condition.LoadIlValue(ilGenerator, local);

        // 创建条件分支
        var elseLabel = ilGenerator.DefineLabel();
        var endLabel = ilGenerator.DefineLabel();

        // 如果条件为假，跳转到else分支
        ilGenerator.Emit(OpCodes.Brfalse, elseLabel);

        // 条件为真时执行的表达式
        TrueExpression.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        // 条件为假时执行的表达式
        ilGenerator.MarkLabel(elseLabel);
        FalseExpression.LoadIlValue(ilGenerator, local);

        // 结束标签
        ilGenerator.MarkLabel(endLabel);
    }

    public override Type? OutputType(LocalManager local)
    {
        // 三元表达式的输出类型是两个分支类型的公共父类型
        var trueType = TrueExpression.OutputType(local);
        var falseType = FalseExpression.OutputType(local);

        if (trueType == falseType)
        {
            return trueType;
        }

        // 如果类型不同，返回object
        return typeof(object);
    }

    public override string ToString()
    {
        return $"{TrueExpression} if {Condition} else {FalseExpression}";
    }
}