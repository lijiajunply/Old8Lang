using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using LangObject = Old8Lang.Runtime.LangObject;

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
    /// 将基本类型转换为 LangValueType
    /// </summary>
    private static void ConvertToLangValueType(ILGenerator ilGenerator, Type type)
    {
        if (type == typeof(int))
        {
            // new IntLangValue(value)
            var ctor = typeof(IntLangValue).GetConstructor([typeof(int)])!;
            ilGenerator.Emit(OpCodes.Newobj, ctor);
        }
        else if (type == typeof(double))
        {
            // new DoubleLangValue(value)
            var ctor = typeof(DoubleLangValue).GetConstructor([typeof(double)])!;
            ilGenerator.Emit(OpCodes.Newobj, ctor);
        }
        else if (type == typeof(string))
        {
            // new StringLangValue(value)
            var ctor = typeof(StringLangValue).GetConstructor([typeof(string)])!;
            ilGenerator.Emit(OpCodes.Newobj, ctor);
        }
        else if (type == typeof(bool))
        {
            // new BoolLangValue(value)
            var ctor = typeof(BoolLangValue).GetConstructor([typeof(bool)])!;
            ilGenerator.Emit(OpCodes.Newobj, ctor);
        }
        else if (type == typeof(char))
        {
            // new CharLangValue(value)
            var ctor = typeof(CharLangValue).GetConstructor([typeof(char)])!;
            ilGenerator.Emit(OpCodes.Newobj, ctor);
        }
        // 如果已经是 LangValueType，不需要转换
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
        // 获取操作数类型
        var gtLeftType = left?.OutputType(local);
        var gtRightType = right?.OutputType(local);

        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (gtLeftType != null && typeof(LangObject).IsAssignableFrom(gtLeftType))
        {
            // 生成调用 _gt 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (gtRightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, gtRightType);
            }

            // 调用 _gt 方法: LangObject._gt(object)
            var gtMethod = typeof(LangObject).GetMethod("_gt")!;
            ilGenerator.Emit(OpCodes.Callvirt, gtMethod);
            return typeof(bool);
        }

        // 检查是否是 AnyLangValue（解释器模式）运算符重载
        if (gtLeftType == typeof(AnyLangValue) || gtLeftType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 Greater 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (gtRightType != null && !typeof(LangValueType).IsAssignableFrom(gtRightType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, gtRightType);
            }

            // 调用 Greater 方法: AnyLangValue.Greater(LangValueType)
            var greaterMethod = typeof(LangValueType).GetMethod("Greater", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, greaterMethod);
            return typeof(bool);
        }

        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);

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
        // 获取操作数类型
        var ltLeftType = left?.OutputType(local);
        var ltRightType = right?.OutputType(local);

        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (ltLeftType != null && typeof(LangObject).IsAssignableFrom(ltLeftType))
        {
            // 生成调用 _lt 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (ltRightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, ltRightType);
            }

            // 调用 _lt 方法: LangObject._lt(object)
            var ltMethod = typeof(LangObject).GetMethod("_lt")!;
            ilGenerator.Emit(OpCodes.Callvirt, ltMethod);
            return typeof(bool);
        }

        // 检查是否是 AnyLangValue 运算符重载
        if (ltLeftType == typeof(AnyLangValue) || ltLeftType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 Less 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (ltRightType != null && !typeof(LangValueType).IsAssignableFrom(ltRightType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, ltRightType);
            }

            // 调用 Less 方法: AnyLangValue.Less(LangValueType)
            var lessMethod = typeof(LangValueType).GetMethod("Less", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, lessMethod);
            return typeof(bool);
        }

        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);

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
        // 获取操作数类型
        var leftOpType = left?.OutputType(local);
        var rightOpType = right?.OutputType(local);

        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (leftOpType != null && typeof(LangObject).IsAssignableFrom(leftOpType))
        {
            // 生成调用 _eq 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (rightOpType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, rightOpType);
            }

            // 调用 _eq 方法: LangObject._eq(object)
            var eqMethod = typeof(LangObject).GetMethod("_eq")!;
            ilGenerator.Emit(OpCodes.Callvirt, eqMethod);
            return typeof(bool);
        }

        // 检查是否是 AnyLangValue 运算符重载
        if (leftOpType == typeof(AnyLangValue) || leftOpType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 Equals 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (rightOpType != null && !typeof(LangValueType).IsAssignableFrom(rightOpType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, rightOpType);
            }

            // 调用 Equals 方法: AnyLangValue.Equals(LangValueType)
            var equalsMethod = typeof(LangValueType).GetMethod("Equals", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, equalsMethod);
            return typeof(bool);
        }

        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);

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
        // 获取操作数类型
        var leftType = left?.OutputType(local);
        var rightType = right?.OutputType(local);

        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _le 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (rightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, rightType);
            }

            // 调用 _le 方法: LangObject._le(object)
            var leMethod = typeof(LangObject).GetMethod("_le")!;
            ilGenerator.Emit(OpCodes.Callvirt, leMethod);
            return typeof(bool);
        }

        // 检查是否是 AnyLangValue 运算符重载
        if (leftType == typeof(AnyLangValue) || leftType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 LessEqual 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (rightType != null && !typeof(LangValueType).IsAssignableFrom(rightType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, rightType);
            }

            // 调用 LessEqual 方法: AnyLangValue.LessEqual(LangValueType)
            var lessEqualMethod = typeof(LangValueType).GetMethod("LessEqual", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, lessEqualMethod);
            return typeof(bool);
        }

        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Cgt);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor);
        return typeof(bool);
    }

    /// <summary>
    /// 生成大于等于比较 IL 代码
    /// 实现方式：!(left &lt; right)
    /// </summary>
    public static Type GenerateGreaterThanEquals(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        // 获取操作数类型
        var leftType = left?.OutputType(local);
        var rightType = right?.OutputType(local);

        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _ge 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (rightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, rightType);
            }

            // 调用 _ge 方法: LangObject._ge(object)
            var geMethod = typeof(LangObject).GetMethod("_ge")!;
            ilGenerator.Emit(OpCodes.Callvirt, geMethod);
            return typeof(bool);
        }

        // 检查是否是 AnyLangValue 运算符重载
        if (leftType == typeof(AnyLangValue) || leftType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 GreaterEqual 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (rightType != null && !typeof(LangValueType).IsAssignableFrom(rightType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, rightType);
            }

            // 调用 GreaterEqual 方法: AnyLangValue.GreaterEqual(LangValueType)
            var greaterEqualMethod = typeof(LangValueType).GetMethod("GreaterEqual", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, greaterEqualMethod);
            return typeof(bool);
        }

        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Clt);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor);
        return typeof(bool);
    }
}
