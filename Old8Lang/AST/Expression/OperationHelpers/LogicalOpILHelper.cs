using System;
using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// 逻辑运算符 IL 代码生成助手类
/// </summary>
/// <remarks>
/// 该类负责生成逻辑运算符（&&、||、^）的IL代码。
/// 逻辑运算符用于布尔值的逻辑运算，支持短路求值。
///
/// 支持的运算符：
/// - AND (&&): 逻辑与运算，短路求值（左操作数为false时不计算右操作数）
/// - OR (||): 逻辑或运算，短路求值（左操作数为true时不计算右操作数）
/// - XOR (^): 逻辑异或运算，不支持短路求值
/// </remarks>
public static class LogicalOpILHelper
{
    /// <summary>
    /// 生成逻辑 AND 运算符（&&）的IL代码
    /// </summary>
    /// <param name="left">左操作数表达式</param>
    /// <param name="right">右操作数表达式</param>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>运算结果的类型（bool）</returns>
    /// <remarks>
    /// 实现短路求值逻辑：
    /// - 如果左操作数为false，则跳过右操作数，直接返回false
    /// - 如果左操作数为true，则计算右操作数，返回右操作数的值
    ///
    /// IL代码结构：
    /// <code>
    /// [加载左操作数]
    /// brfalse falseLabel  // 如果左操作数为false，跳转到falseLabel
    /// [加载右操作数]
    /// br endLabel         // 跳转到endLabel
    /// falseLabel:
    /// ldc.i4.0            // 加载false
    /// endLabel:
    /// </code>
    /// </remarks>
    public static Type GenerateAnd(
        LangExpression left,
        LangExpression right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        // 实现短路求值：如果左操作数为false，则跳过右操作数
        var endLabel = ilGenerator.DefineLabel();
        var falseLabel = ilGenerator.DefineLabel();

        // 加载左操作数
        left.LoadIlValue(ilGenerator, local);
        // 如果左操作数为false，跳转到falseLabel
        ilGenerator.Emit(OpCodes.Brfalse, falseLabel);
        // 加载右操作数
        right.LoadIlValue(ilGenerator, local);
        // 右操作数已经在栈上，直接跳转到endLabel
        ilGenerator.Emit(OpCodes.Br, endLabel);
        // 左操作数为false的情况
        ilGenerator.MarkLabel(falseLabel);
        ilGenerator.Emit(OpCodes.Ldc_I4_0); // 加载false
        // 结果为true或false的情况
        ilGenerator.MarkLabel(endLabel);
        return typeof(bool);
    }

    /// <summary>
    /// 生成逻辑 OR 运算符（||）的IL代码
    /// </summary>
    /// <param name="left">左操作数表达式</param>
    /// <param name="right">右操作数表达式</param>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>运算结果的类型（bool）</returns>
    /// <remarks>
    /// 实现短路求值逻辑：
    /// - 如果左操作数为true，则跳过右操作数，直接返回true
    /// - 如果左操作数为false，则计算右操作数，返回右操作数的值
    ///
    /// IL代码结构：
    /// <code>
    /// [加载左操作数]
    /// brtrue trueLabel    // 如果左操作数为true，跳转到trueLabel
    /// [加载右操作数]
    /// br endLabel         // 跳转到endLabel
    /// trueLabel:
    /// ldc.i4.1            // 加载true
    /// endLabel:
    /// </code>
    /// </remarks>
    public static Type GenerateOr(
        LangExpression left,
        LangExpression right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        // 实现短路求值：如果左操作数为true，则跳过右操作数
        var endLabel = ilGenerator.DefineLabel();
        var trueLabel = ilGenerator.DefineLabel();

        // 加载左操作数
        left.LoadIlValue(ilGenerator, local);
        // 如果左操作数为true，跳转到trueLabel
        ilGenerator.Emit(OpCodes.Brtrue, trueLabel);
        // 加载右操作数
        right.LoadIlValue(ilGenerator, local);
        // 右操作数已经在栈上，直接跳转到endLabel
        ilGenerator.Emit(OpCodes.Br, endLabel);
        // 左操作数为true的情况
        ilGenerator.MarkLabel(trueLabel);
        ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载true
        // 结果为true或false的情况
        ilGenerator.MarkLabel(endLabel);
        return typeof(bool);
    }

    /// <summary>
    /// 生成逻辑 XOR 运算符（^）的IL代码
    /// </summary>
    /// <param name="left">左操作数表达式</param>
    /// <param name="right">右操作数表达式</param>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>运算结果的类型（bool）</returns>
    /// <remarks>
    /// 逻辑异或运算，不支持短路求值（需要计算两个操作数）。
    /// 当且仅当两个操作数的值不同时，结果为true。
    ///
    /// IL代码结构：
    /// <code>
    /// [加载左操作数]
    /// [加载右操作数]
    /// xor                 // 执行异或运算
    /// </code>
    /// </remarks>
    public static Type GenerateXor(
        LangExpression left,
        LangExpression right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        left.LoadIlValue(ilGenerator, local);
        right.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Xor);
        return typeof(bool);
    }
}
