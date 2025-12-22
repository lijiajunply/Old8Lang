using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 三元表达式
/// 语法：expression if condition else expression
/// </summary>
public partial class TernaryExpression(
    LangExpression condition,
    LangExpression trueExpression,
    LangExpression falseExpression,
    SourcePosition position = default)
    : LangExpression(position)
{
    
    /// <summary>
    /// 条件表达式
    /// </summary>
    public LangExpression Condition { get; } = condition;

    /// <summary>
    /// 条件为真时执行的表达式
    /// </summary>
    public LangExpression TrueExpression { get; } = trueExpression;

    /// <summary>
    /// 条件为假时执行的表达式
    /// </summary>
    public LangExpression FalseExpression { get; } = falseExpression;

    /// <summary>
    /// 执行三元表达式，根据条件结果返回相应的表达式值
    /// </summary>
    /// <param name="manager">变量管理器，用于管理执行环境中的变量</param>
    /// <returns>条件为真时返回TrueExpression的结果，否则返回FalseExpression的结果</returns>
    /// <exception cref="InvalidOperationError">当条件不是Bool类型时抛出</exception>
    public override LangValueType Run(VariateManager manager)
    {
        // 执行条件判断
        var condition = Condition.Run(manager);
        if (condition is not BoolLangValue boolValue)
        {
            throw new InvalidOperationError(this, "三元条件表达式的条件必须是Bool类型");
        }

        // 根据条件结果返回相应的表达式值
        return boolValue.Value
            ? TrueExpression.Run(manager)
            : FalseExpression.Run(manager);
    }

    /// <summary>
    /// 生成将三元表达式结果加载到栈上的IL指令
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器，用于生成IL代码</param>
    /// <param name="local">局部变量管理器，用于管理局部变量</param>
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

    /// <summary>
    /// 获取三元表达式的输出类型
    /// </summary>
    /// <param name="local">局部变量管理器，用于管理局部变量</param>
    /// <returns>如果两个分支类型相同则返回该类型，否则返回object类型</returns>
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

    /// <summary>
    /// 将三元表达式转换为字符串表示
    /// </summary>
    /// <returns>三元表达式的字符串表示，格式为"condition ? trueExpression : falseExpression"</returns>
    public override string ToString()
    {
        return $"{Condition} ? {TrueExpression} : {FalseExpression}";
    }
}