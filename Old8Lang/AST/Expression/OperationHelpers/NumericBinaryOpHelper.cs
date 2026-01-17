using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using LangObject = Old8Lang.Runtime.LangObject;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// 数值二元运算助手类
/// 处理数值运算中的类型转换和IL代码生成
/// </summary>
internal static class NumericBinaryOpHelper
{
    /// <summary>
    /// 加载左右操作数并进行类型转换
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="leftType">左操作数类型（可能被修改）</param>
    /// <param name="rightType">右操作数类型（可能被修改）</param>
    /// <param name="allowObjectUnboxing">是否允许object类型自动拆箱为int</param>
    public static void LoadAndConvertOperands(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        ref Type? leftType,
        ref Type? rightType,
        bool allowObjectUnboxing = true)
    {
        // 加载左操作数
        left?.LoadIlValue(ilGenerator, local);

        // 如果左操作数是object类型且允许拆箱，拆箱为int
        if (allowObjectUnboxing && leftType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            leftType = typeof(int);
        }

        // 加载右操作数
        right?.LoadIlValue(ilGenerator, local);

        // 如果右操作数是object类型且允许拆箱，拆箱为int
        if (allowObjectUnboxing && rightType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            rightType = typeof(int);
        }
    }

    /// <summary>
    /// 处理double类型的转换
    /// 如果任一操作数是double，确保两个操作数都转换为double
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="leftType">左操作数类型</param>
    /// <param name="rightType">右操作数类型</param>
    /// <returns>如果进行了转换返回true</returns>
    public static bool ConvertToDoubleIfNeeded(
        ILGenerator ilGenerator,
        Type? leftType,
        Type? rightType)
    {
        if (leftType == typeof(double) || rightType == typeof(double))
        {
            // 确保两个操作数都是double类型
            // 注意：栈上顺序是 [left, right]，需要先转换right，再转换left

            // 如果右操作数是int，转换为double
            if (rightType == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Conv_R8);
            }

            // 如果左操作数是int，需要交换顺序后转换
            if (leftType == typeof(int))
            {
                // 由于栈是后进先出，现在栈顶是right
                // 我们需要转换left，所以需要保存right，转换left，再恢复right
                var rightTemp = ilGenerator.DeclareLocal(typeof(double));
                ilGenerator.Emit(OpCodes.Stloc, rightTemp);  // 保存right
                ilGenerator.Emit(OpCodes.Conv_R8);            // 转换left
                ilGenerator.Emit(OpCodes.Ldloc, rightTemp);   // 恢复right
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 生成加法运算IL代码
    /// </summary>
    public static Type GenerateAddition(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type? rightType)
    {
        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _add 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (rightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, rightType);
            }

            // 调用 _add 方法: LangObject._add(object)
            var addMethod = typeof(LangObject).GetMethod("_add")!;
            ilGenerator.Emit(OpCodes.Callvirt, addMethod);
            return typeof(object);
        }

        // 检查是否是 AnyLangValue（解释器模式）运算符重载
        if (leftType == typeof(AnyLangValue) || leftType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 Plus 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (rightType != null && !typeof(LangValueType).IsAssignableFrom(rightType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, rightType);
            }

            // 调用 Plus 方法: AnyLangValue.Plus(LangValueType)
            var plusMethod = typeof(LangValueType).GetMethod("Plus", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, plusMethod);
            return typeof(LangValueType);
        }

        LoadAndConvertOperands(left, right, ilGenerator, local, ref leftType, ref rightType);

        if (ConvertToDoubleIfNeeded(ilGenerator, leftType, rightType))
        {
            ilGenerator.Emit(OpCodes.Add);
            return typeof(double);
        }

        // 整数加法
        ilGenerator.Emit(OpCodes.Add);
        return typeof(int);
    }

    /// <summary>
    /// 生成减法运算IL代码
    /// </summary>
    public static Type GenerateSubtraction(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type? rightType)
    {
        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _sub 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (rightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, rightType);
            }

            // 调用 _sub 方法: LangObject._sub(object)
            var subMethod = typeof(LangObject).GetMethod("_sub")!;
            ilGenerator.Emit(OpCodes.Callvirt, subMethod);
            return typeof(object);
        }

        // 检查是否是 AnyLangValue（解释器模式）运算符重载
        if (leftType == typeof(AnyLangValue) || leftType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 Minus 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (rightType != null && !typeof(LangValueType).IsAssignableFrom(rightType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, rightType);
            }

            // 调用 Minus 方法: AnyLangValue.Minus(LangValueType)
            var minusMethod = typeof(LangValueType).GetMethod("Minus", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, minusMethod);
            return typeof(LangValueType);
        }

        LoadAndConvertOperands(left, right, ilGenerator, local, ref leftType, ref rightType);

        if (ConvertToDoubleIfNeeded(ilGenerator, leftType, rightType))
        {
            ilGenerator.Emit(OpCodes.Sub);
            return typeof(double);
        }

        ilGenerator.Emit(OpCodes.Sub);
        return typeof(int);
    }

    /// <summary>
    /// 生成乘法运算IL代码
    /// 处理特殊情况：两个object类型的乘法需要使用Convert.ToInt32
    /// </summary>
    public static Type GenerateMultiplication(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type? rightType,
        Operation operation)
    {
        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _mul 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (rightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, rightType);
            }

            // 调用 _mul 方法: LangObject._mul(object)
            var mulMethod = typeof(LangObject).GetMethod("_mul")!;
            ilGenerator.Emit(OpCodes.Callvirt, mulMethod);
            return typeof(object);
        }

        // 先加载操作数但不自动拆箱
        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);

        // 处理不同类型的乘法
        if (leftType == typeof(double) || rightType == typeof(double))
        {
            // 确保两个操作数都是double类型
            if (leftType == typeof(int))
            {
                // 栈顶是right，需要交换
                var rightTemp = ilGenerator.DeclareLocal(typeof(double));
                ilGenerator.Emit(OpCodes.Stloc, rightTemp);
                ilGenerator.Emit(OpCodes.Conv_R8);
                ilGenerator.Emit(OpCodes.Ldloc, rightTemp);
            }

            if (rightType == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Conv_R8);
            }

            ilGenerator.Emit(OpCodes.Mul);
            return typeof(double);
        }

        // 特殊处理：如果两个操作数都是object类型，使用Convert.ToInt32进行安全转换
        if (leftType == typeof(object) && rightType == typeof(object))
        {
            // 栈上有: [left_value, right_value]
            // 先保存right到临时变量
            var rightTemp = ilGenerator.DeclareLocal(typeof(object));
            ilGenerator.Emit(OpCodes.Stloc, rightTemp);

            // 转换left为int
            var toInt32Method = typeof(Convert).GetMethod("ToInt32", [typeof(object)])!;
            ilGenerator.Emit(OpCodes.Call, toInt32Method);

            // 加载right并转换
            ilGenerator.Emit(OpCodes.Ldloc, rightTemp);
            ilGenerator.Emit(OpCodes.Call, toInt32Method);

            ilGenerator.Emit(OpCodes.Mul);
            return typeof(int);
        }

        // 确保操作数是int类型
        // EnsureIntType(ilGenerator, leftType, rightType);

        ilGenerator.Emit(OpCodes.Mul);
        return typeof(int);
    }

    /// <summary>
    /// 生成除法运算IL代码
    /// </summary>
    public static Type GenerateDivision(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type? rightType)
    {
        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _div 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (rightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, rightType);
            }

            // 调用 _div 方法: LangObject._div(object)
            var divMethod = typeof(LangObject).GetMethod("_div")!;
            ilGenerator.Emit(OpCodes.Callvirt, divMethod);
            return typeof(object);
        }

        // 检查是否是 AnyLangValue（解释器模式）运算符重载
        if (leftType == typeof(AnyLangValue) || leftType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 Divide 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (rightType != null && !typeof(LangValueType).IsAssignableFrom(rightType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, rightType);
            }

            // 调用 Divide 方法: AnyLangValue.Divide(LangValueType)
            var divideMethod = typeof(LangValueType).GetMethod("Divide", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, divideMethod);
            return typeof(LangValueType);
        }

        if (leftType == typeof(double) || rightType == typeof(double))
        {
            // 确保两个操作数都是double类型
            left?.LoadIlValue(ilGenerator, local);
            if (leftType == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Conv_R8);
            }

            right?.LoadIlValue(ilGenerator, local);
            if (rightType == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Conv_R8);
            }

            ilGenerator.Emit(OpCodes.Div);
            return typeof(double);
        }

        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Div);
        return typeof(int);
    }

    /// <summary>
    /// 生成取模运算IL代码
    /// </summary>
    public static Type GenerateModulo(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type? rightType)
    {
        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _mod 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (rightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, rightType);
            }

            // 调用 _mod 方法: LangObject._mod(object)
            var modMethod = typeof(LangObject).GetMethod("_mod")!;
            ilGenerator.Emit(OpCodes.Callvirt, modMethod);
            return typeof(object);
        }

        // 检查是否是 AnyLangValue（解释器模式）运算符重载
        if (leftType == typeof(AnyLangValue) || leftType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 Mod 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (rightType != null && !typeof(LangValueType).IsAssignableFrom(rightType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, rightType);
            }

            // 调用 Mod 方法: AnyLangValue.Mod(LangValueType)
            var modMethod = typeof(LangValueType).GetMethod("Mod", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, modMethod);
            return typeof(LangValueType);
        }

        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);

        // 获取操作数类型
        var modLeftType = left?.OutputType(local);
        var modRightType = right?.OutputType(local);

        // 处理 ForIn 循环中变量的特殊情况（object vs int）
        if ((modLeftType == typeof(object) && modRightType == typeof(int)) ||
            (modLeftType == typeof(int) && modRightType == typeof(object)))
        {
            // 对于 object vs int，拆箱 object 到 int
            if (modLeftType == typeof(object))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            }
            if (modRightType == typeof(object))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            }
            ilGenerator.Emit(OpCodes.Rem);
            return typeof(int);
        }

        if (modLeftType == typeof(double) || modRightType == typeof(double))
        {
            // 确保两个操作数都是double类型
            if (modLeftType == typeof(int))
            {
                // 栈顶是right，需要交换
                var rightTemp = ilGenerator.DeclareLocal(typeof(double));
                ilGenerator.Emit(OpCodes.Stloc, rightTemp);
                ilGenerator.Emit(OpCodes.Conv_R8);
                ilGenerator.Emit(OpCodes.Ldloc, rightTemp);
            }

            if (modRightType == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Conv_R8);
            }

            ilGenerator.Emit(OpCodes.Rem);
            return typeof(double);
        }

        ilGenerator.Emit(OpCodes.Rem);
        return typeof(int);
    }

    /// <summary>
    /// 生成幂运算IL代码
    /// </summary>
    public static Type GeneratePower(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type? rightType)
    {
        // 检查是否是 LangObject（编译器模式的自定义类）运算符重载
        if (leftType != null && typeof(LangObject).IsAssignableFrom(leftType))
        {
            // 生成调用 _pow 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数是值类型，需要装箱
            if (rightType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, rightType);
            }

            // 调用 _pow 方法: LangObject._pow(object)
            var langPowMethod = typeof(LangObject).GetMethod("_pow")!;
            ilGenerator.Emit(OpCodes.Callvirt, langPowMethod);
            return typeof(object);
        }

        // 检查是否是 AnyLangValue（解释器模式）运算符重载
        if (leftType == typeof(AnyLangValue) || leftType?.IsSubclassOf(typeof(AnyLangValue)) == true)
        {
            // 生成调用 Power 方法的 IL 代码
            left?.LoadIlValue(ilGenerator, local);
            right?.LoadIlValue(ilGenerator, local);

            // 如果右操作数不是 LangValueType，需要装箱
            if (rightType != null && !typeof(LangValueType).IsAssignableFrom(rightType))
            {
                // 将基本类型转换为 LangValueType
                ConvertToLangValueType(ilGenerator, rightType);
            }

            // 调用 Power 方法: AnyLangValue.Power(LangValueType)
            var powerMethod = typeof(LangValueType).GetMethod("Power", [typeof(LangValueType)])!;
            ilGenerator.Emit(OpCodes.Callvirt, powerMethod);
            return typeof(LangValueType);
        }

        left?.LoadIlValue(ilGenerator, local);
        right?.LoadIlValue(ilGenerator, local);

        // 确保两个操作数都是double类型，因为Math.Pow需要double参数
        if (leftType == typeof(int))
        {
            // 栈顶是right，需要交换
            var rightTemp = ilGenerator.DeclareLocal(rightType!);
            ilGenerator.Emit(OpCodes.Stloc, rightTemp);
            ilGenerator.Emit(OpCodes.Conv_R8);
            ilGenerator.Emit(OpCodes.Ldloc, rightTemp);
        }

        if (rightType == typeof(int))
        {
            ilGenerator.Emit(OpCodes.Conv_R8);
        }

        // 调用Math.Pow方法
        var powMethod = typeof(Math).GetMethod("Pow", [typeof(double), typeof(double)])!;
        ilGenerator.Emit(OpCodes.Call, powMethod);

        // 如果两个操作数都是int类型，返回int类型
        if (leftType == typeof(int) && rightType == typeof(int))
        {
            ilGenerator.Emit(OpCodes.Conv_I4);
            return typeof(int);
        }

        // 否则返回double类型
        return typeof(double);
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
}
