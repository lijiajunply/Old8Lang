using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using LangObject = Old8Lang.Runtime.LangObject;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// 比较运算助手类
/// 处理比较运算中的类型转换和IL代码生成
/// </summary>
internal static class ComparisonOpHelper
{
    /// <summary>
    /// 处理 object 与数值的比较：集合索引和动态调用会返回 object，
    /// 但其运行期值仍可能是任意 CLR 数值类型。
    /// </summary>
    /// <param name="leftType">左操作数类型</param>
    /// <param name="rightType">右操作数类型</param>
    /// <returns>如果是 object vs int 特殊情况返回 true</returns>
    internal static bool IsObjectVsNumericComparison(Type? leftType, Type? rightType)
    {
        return (leftType == typeof(object) && IsNumericType(rightType)) ||
               (IsNumericType(leftType) && rightType == typeof(object));
    }

    /// <summary>
    /// 相等比较是否必须走 <c>object.Equals</c>：任一侧的静态类型是 <see cref="object"/>。
    /// </summary>
    /// <remarks>
    /// 静态类型为 object 的值在运行期可能是装箱值类型、字符串或任意引用。
    /// 直接用 <c>ceq</c> 只会比较引用，`"success" == result`（result 是 object 槽位）
    /// 这类比较会得到 false。object.Equals 才能按值比较，也与解释器的语义一致。
    /// </remarks>
    internal static bool HasObjectOperand(Type? leftType, Type? rightType)
    {
        return leftType == typeof(object) || rightType == typeof(object);
    }

    /// <summary>
    /// 在 object 与数值混合比较时，按「发射一个操作数、立即装箱一个」的顺序压栈。
    /// </summary>
    /// <remarks>
    /// 不能先把两个操作数都压栈再统一 <c>box</c>：<c>box</c> 只作用于栈顶，
    /// 数值在左侧时会把引用类型的右操作数当成装箱目标，左侧整数保持未装箱。
    /// 随后 <c>callvirt IComparable.CompareTo(object)</c> 会把未装箱整数当作
    /// <c>this</c> 引用传入，生成的是无效 IL，运行期直接使进程崩溃而不是抛托管异常。
    /// 另外这里的装箱顺序必须与 <c>Box</c> 的目标类型严格对应，
    /// 因此按操作数位置逐个发射。
    /// </remarks>
    internal static void EmitBoxedOperands(
        LangExpression? left,
        LangExpression? right,
        Type? leftType,
        Type? rightType,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        if (leftType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, leftType);
        }

        right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        if (rightType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, rightType);
        }
    }

    internal static bool IsNumericType(Type? type) => type == typeof(int) || type == typeof(double) ||
                                                     type == typeof(float) || type == typeof(long) ||
                                                     type == typeof(short) || type == typeof(byte);

    /// <summary>
    /// 将基本类型转换为 LangValueType
    /// </summary>
    internal static void ConvertToLangValueType(ILGenerator ilGenerator, Type type)
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
        var gtLeftType = left?.Accept(new TypeInferenceVisitor(local));
        var gtRightType = right?.Accept(new TypeInferenceVisitor(local));

        // 检查是否是 LangObject（IL 模式的自定义类）运算符重载
        if (gtLeftType != null && typeof(LangObject).IsAssignableFrom(gtLeftType))
        {
            // 生成调用 _gt 方法的 IL 代码
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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

        // 处理 ForIn 循环中变量的特殊情况（object vs int）
        if (IsObjectVsNumericComparison(gtLeftType, gtRightType))
        {
            // 按位置装箱：数值在左侧时必须先装箱左操作数
            EmitBoxedOperands(left, right, gtLeftType, gtRightType, ilGenerator, local);

            // 使用 IComparable.CompareTo 方法进行比较
            var compareToMethod = typeof(IComparable).GetMethod("CompareTo", [typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, compareToMethod);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Cgt);
        }
        else
        {
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
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
        var ltLeftType = left?.Accept(new TypeInferenceVisitor(local));
        var ltRightType = right?.Accept(new TypeInferenceVisitor(local));

        // 检查是否是 LangObject（IL 模式的自定义类）运算符重载
        if (ltLeftType != null && typeof(LangObject).IsAssignableFrom(ltLeftType))
        {
            // 生成调用 _lt 方法的 IL 代码
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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

        // 处理 ForIn 循环中变量的特殊情况（object vs int）
        if (IsObjectVsNumericComparison(ltLeftType, ltRightType))
        {
            // 按位置装箱：数值在左侧时必须先装箱左操作数
            EmitBoxedOperands(left, right, ltLeftType, ltRightType, ilGenerator, local);

            // 使用 IComparable.CompareTo 方法进行比较
            var compareToMethod = typeof(IComparable).GetMethod("CompareTo", [typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, compareToMethod);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Clt);
        }
        else
        {
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
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
        var leftOpType = left?.Accept(new TypeInferenceVisitor(local));
        var rightOpType = right?.Accept(new TypeInferenceVisitor(local));

        // 检查是否是 LangObject（IL 模式的自定义类）运算符重载
        if (leftOpType != null && typeof(LangObject).IsAssignableFrom(leftOpType))
        {
            // 生成调用 _eq 方法的 IL 代码
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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

        // 如果都是字符串，使用字符串比较
        if (leftOpType == typeof(string) && rightOpType == typeof(string))
        {
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var equalsMethod = typeof(string).GetMethod("Equals", [typeof(string), typeof(string)])!;
            ilGenerator.Emit(OpCodes.Call, equalsMethod);
        }
        // 任一侧静态类型为 object：运行期按值比较（装箱值 / 字符串 / 任意引用）
        else if (HasObjectOperand(leftOpType, rightOpType))
        {
            // 按位置装箱：值类型操作数必须各自装箱
            EmitBoxedOperands(left, right, leftOpType, rightOpType, ilGenerator, local);

            // 使用 object.Equals 方法进行比较
            var equalsMethod = typeof(object).GetMethod("Equals", [typeof(object), typeof(object)])!;
            ilGenerator.Emit(OpCodes.Call, equalsMethod);
        }
        else
        {
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
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
        var leftOpType = left?.Accept(new TypeInferenceVisitor(local));
        var rightOpType = right?.Accept(new TypeInferenceVisitor(local));

        // 字符串按值比较，否则 Ceq 只比较引用
        if (leftOpType == typeof(string) && rightOpType == typeof(string))
        {
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var equalsMethod = typeof(string).GetMethod("Equals", [typeof(string), typeof(string)])!;
            ilGenerator.Emit(OpCodes.Call, equalsMethod);
        }
        // 任一侧静态类型为 object：运行期按值比较
        else if (HasObjectOperand(leftOpType, rightOpType))
        {
            EmitBoxedOperands(left, right, leftOpType, rightOpType, ilGenerator, local);
            var equalsMethod = typeof(object).GetMethod("Equals", [typeof(object), typeof(object)])!;
            ilGenerator.Emit(OpCodes.Call, equalsMethod);
        }
        else
        {
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            ilGenerator.Emit(OpCodes.Ceq);
        }

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
        var leftType = left?.Accept(new TypeInferenceVisitor(local));
        var rightType = right?.Accept(new TypeInferenceVisitor(local));

        // 检查是否是 LangObject（IL 模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _le 方法的 IL 代码
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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

        // object 与 int 混用（如未注解的函数参数）：装箱后按 IComparable 比较
        if (IsObjectVsNumericComparison(leftType, rightType))
        {
            // 按位置装箱：数值在左侧时必须先装箱左操作数
            EmitBoxedOperands(left, right, leftType, rightType, ilGenerator, local);
            var compareToMethod = typeof(IComparable).GetMethod("CompareTo", [typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, compareToMethod);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Cgt);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Xor);
            return typeof(bool);
        }

        left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
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
        var leftType = left?.Accept(new TypeInferenceVisitor(local));
        var rightType = right?.Accept(new TypeInferenceVisitor(local));

        // 检查是否是 LangObject（IL 模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _ge 方法的 IL 代码
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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
            left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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

        // object 与 int 混用（如未注解的函数参数）：装箱后按 IComparable 比较
        if (IsObjectVsNumericComparison(leftType, rightType))
        {
            // 按位置装箱：数值在左侧时必须先装箱左操作数
            EmitBoxedOperands(left, right, leftType, rightType, ilGenerator, local);
            var compareToMethod = typeof(IComparable).GetMethod("CompareTo", [typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, compareToMethod);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Clt);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Xor);
            return typeof(bool);
        }

        left?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        right?.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        ilGenerator.Emit(OpCodes.Clt);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor);
        return typeof(bool);
    }
}
