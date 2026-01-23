using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// 空值合并运算符（??）IL 代码生成助手类
/// </summary>
/// <remarks>
/// 该类负责生成空值合并运算符（??）的IL代码。
/// 空值合并运算符用于在左侧值为null时返回右侧值，否则返回左侧值。
///
/// 生成的IL代码逻辑：
/// 1. 对于值类型：直接返回左侧值（值类型不能为null）
/// 2. 对于引用类型：
///    - 加载左侧值并复制一份
///    - 检查是否为null（与null比较）
///    - 如果为null，弹出左侧值，加载右侧值
///    - 如果不为null，保留左侧值
/// </remarks>
public static class NullishCoalescingILHelper
{
    /// <summary>
    /// 生成空值合并运算符（??）的IL代码
    /// </summary>
    /// <param name="left">左操作数表达式（可能为null的值）</param>
    /// <param name="right">右操作数表达式（当左侧为null时使用的备选值）</param>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="leftType">左操作数的类型</param>
    /// <returns>运算结果的类型（与左操作数类型相同）</returns>
    /// <remarks>
    /// 该方法实现了C#的空值合并运算符（??）的IL代码生成逻辑：
    /// - 如果左侧值不为null，返回左侧值
    /// - 如果左侧值为null，返回右侧值
    ///
    /// IL代码结构（引用类型）：
    /// <code>
    /// [加载左侧值]
    /// dup              // 复制栈顶值
    /// ldnull
    /// ceq              // 比较是否为null
    /// brtrue rightLabel // 如果为null，跳转到加载右侧值
    /// br endLabel      // 不为null，跳过右侧值加载
    /// rightLabel:
    /// pop              // 弹出左侧值
    /// [加载右侧值]
    /// endLabel:
    /// </code>
    /// </remarks>
    public static Type GenerateNullishCoalescing(
        LangExpression left,
        LangExpression right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type leftType)
    {
        // 值类型不能为null，直接返回左侧值
        if (leftType.IsValueType)
        {
            left.LoadIlValue(ilGenerator, local);
            return leftType;
        }

        // 引用类型，加载左侧值
        left.LoadIlValue(ilGenerator, local);

        // 检查左侧值是否为null
        ilGenerator.Emit(OpCodes.Dup); // 复制左侧值到栈顶
        ilGenerator.Emit(OpCodes.Ldnull);
        ilGenerator.Emit(OpCodes.Ceq);

        // 如果左侧值为null，跳转到加载右侧值的标签
        var rightLabel = ilGenerator.DefineLabel();
        var endLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brtrue, rightLabel);

        // 左侧值不为null，直接返回（栈上已有左侧值）
        ilGenerator.Emit(OpCodes.Br, endLabel);

        // 左侧值为null，弹出栈上的左侧值，加载右侧值
        ilGenerator.MarkLabel(rightLabel);
        ilGenerator.Emit(OpCodes.Pop); // 弹出栈上的左侧值
        right.LoadIlValue(ilGenerator, local);

        // 结束标签
        ilGenerator.MarkLabel(endLabel);

        // 返回左侧值或右侧值的类型
        return leftType;
    }
}
