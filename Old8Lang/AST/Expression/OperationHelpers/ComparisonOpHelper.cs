using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// 比较运算助手类
/// 处理比较运算中的类型转换和IL代码生成
/// </summary>
internal static class ComparisonOpHelper
{
    /// <summary>
    /// 处理 object vs int 的特殊比较情况
    /// 将 int 装箱后使用 IComparable.CompareTo 或 object.Equals
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="leftType">左操作数类型</param>
    /// <param name="rightType">右操作数类型</param>
    /// <returns>如果是 object vs int 特殊情况返回 true</returns>
    private static bool IsObjectVsIntComparison(Type? leftType, Type? rightType)
    {
        return (leftType == typeof(object) && rightType == typeof(int)) ||
               (leftType == typeof(int) && rightType == typeof(object));
    }

    /// <summary>
    /// 装箱 int 类型为 object
    /// </summary>
    private static void BoxIntIfNeeded(ILGenerator ilGenerator, Type? leftType, Type? rightType)
    {
        if (leftType == typeof(int))
        {
            ilGenerator.Emit(OpCodes.Box, typeof(int));
        }
        if (rightType == typeof(int))
        {
            ilGenerator.Emit(OpCodes.Box, typeof(int));
        }
    }

    /// <summary>
    /// 生成大于比较 IL 代码
    /// </summary>
    public static Type GenerateGreaterThan(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);

        // 获取操作数类型以进行特殊处理
        var gtLeftType = left?.OutputType(local);
        var gtRightType = right?.OutputType(local);

        // 处理 ForIn 循环中变量的特殊情况（object vs int）
        if (IsObjectVsIntComparison(gtLeftType, gtRightType))
        {
            // 装箱 int
            BoxIntIfNeeded(ilGenerator, gtLeftType, gtRightType);

            // 使用 IComparable.CompareTo 方法进行比较
            var compareToMethod = typeof(IComparable).GetMethod("CompareTo", [typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, compareToMethod);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Cgt);
        }
        else
        {
            ilGenerator.Emit(OpCodes.Cgt);
        }

        return typeof(bool);
    }

    /// <summary>
    /// 生成小于比较 IL 代码
    /// </summary>
    public static Type GenerateLessThan(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);

        // 获取操作数类型以进行特殊处理
        var ltLeftType = left?.OutputType(local);
        var ltRightType = right?.OutputType(local);

        // 处理 ForIn 循环中变量的特殊情况（object vs int）
        if (IsObjectVsIntComparison(ltLeftType, ltRightType))
        {
            // 装箱 int
            BoxIntIfNeeded(ilGenerator, ltLeftType, ltRightType);

            // 使用 IComparable.CompareTo 方法进行比较
            var compareToMethod = typeof(IComparable).GetMethod("CompareTo", [typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, compareToMethod);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Clt);
        }
        else
        {
            ilGenerator.Emit(OpCodes.Clt);
        }

        return typeof(bool);
    }

    /// <summary>
    /// 生成等于比较 IL 代码
    /// </summary>
    public static Type GenerateEquals(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);

        // 获取操作数类型以进行特殊处理
        var leftOpType = left?.OutputType(local);
        var rightOpType = right?.OutputType(local);

        // 如果都是字符串，使用字符串比较
        if (leftOpType == typeof(string) && rightOpType == typeof(string))
        {
            var equalsMethod = typeof(string).GetMethod("Equals", [typeof(string), typeof(string)])!;
            ilGenerator.Emit(OpCodes.Call, equalsMethod);
        }
        // 处理 ForIn 循环中变量的特殊情况（object vs int）
        else if (IsObjectVsIntComparison(leftOpType, rightOpType))
        {
            // 装箱 int
            BoxIntIfNeeded(ilGenerator, leftOpType, rightOpType);

            // 使用 object.Equals 方法进行比较
            var equalsMethod = typeof(object).GetMethod("Equals", [typeof(object), typeof(object)])!;
            ilGenerator.Emit(OpCodes.Call, equalsMethod);
        }
        else
        {
            // 其他类型使用 Ceq 指令
            ilGenerator.Emit(OpCodes.Ceq);
        }

        return typeof(bool);
    }

    /// <summary>
    /// 生成不等于比较 IL 代码
    /// </summary>
    public static Type GenerateNotEquals(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ceq);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor);
        return typeof(bool);
    }

    /// <summary>
    /// 生成小于等于比较 IL 代码
    /// 实现方式：!(left > right)
    /// </summary>
    public static Type GenerateLessThanEquals(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Cgt);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor);
        return typeof(bool);
    }

    /// <summary>
    /// 生成大于等于比较 IL 代码
    /// 实现方式：!(left < right)
    /// </summary>
    public static Type GenerateGreaterThanEquals(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Clt);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor);
        return typeof(bool);
    }
}
