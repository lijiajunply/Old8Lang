using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.Error;

namespace Old8Lang.Compiler;

/// <summary>
/// 类型转换类，负责生成各种类型转换的IL指令
/// </summary>
/// <remarks>
/// 该类是编译器生成IL代码时的重要组件，支持多种类型转换：
/// - 基本类型到字符串的转换
/// - 字符串到基本类型的转换
/// - 数值类型之间的转换
/// - 布尔值转换
/// - 字符类型转换
/// - 复杂类型转换（数组、列表等）
/// - 值类型到引用类型的转换（装箱）
/// - 引用类型到值类型的转换（拆箱）
/// - 引用类型到引用类型的转换
/// </remarks>
public static class TypeConversion
{
    /// <summary>
    /// 生成类型转换的IL指令
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="expression">表达式对象，用于错误报告</param>
    /// <remarks>
    /// 该方法根据源类型和目标类型的不同，调用不同的转换方法生成IL指令。
    /// 如果无法进行类型转换，会生成抛出InvalidCastException的IL指令。
    /// </remarks>
    public static void GenerateTypeConversionIl(ILGenerator ilGenerator, Type sourceType, Type targetType,
        LangExpression expression)
    {
        // 如果源类型和目标类型相同，无需转换
        if (sourceType == targetType)
        {
            return;
        }

        // 处理基本类型到字符串的转换
        if (targetType == typeof(string))
        {
            GenerateToFromStringConversionIl(ilGenerator, sourceType);
            return;
        }

        // 处理字符串到基本类型的转换
        if (sourceType == typeof(string))
        {
            GenerateFromStringConversionIl(ilGenerator, targetType, expression);
            return;
        }

        // 处理数值类型之间的转换
        if (IsNumericType(sourceType) && IsNumericType(targetType))
        {
            GenerateNumericConversionIl(ilGenerator, sourceType, targetType);
            return;
        }

        // 处理布尔值转换
        if (sourceType == typeof(bool) || targetType == typeof(bool))
        {
            GenerateBoolConversionIl(ilGenerator, sourceType, targetType);
            return;
        }

        // 处理字符类型转换
        if (sourceType == typeof(char) || targetType == typeof(char))
        {
            GenerateCharConversionIl(ilGenerator, sourceType, targetType);
            return;
        }

        // 处理复杂类型转换：数组转列表
        if (sourceType.IsArray && targetType == typeof(List<object>))
        {
            ilGenerator.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor([typeof(IEnumerable<object>)])!);
            return;
        }

        // 处理复杂类型转换：列表转数组
        if (sourceType == typeof(List<object>) && targetType.IsArray)
        {
            ilGenerator.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod("ToArray")!);
            return;
        }

        // 处理值类型到引用类型的转换（装箱）
        if (sourceType.IsValueType && !targetType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, sourceType);
            return;
        }

        // 处理引用类型到值类型的转换（拆箱）
        if (!sourceType.IsValueType && targetType.IsValueType)
        {
            // 添加null检查，避免拆箱null值导致异常
            var nullLabel = ilGenerator.DefineLabel();
            var endLabel = ilGenerator.DefineLabel();

            // 保存栈顶值到临时变量
            var tempVar = ilGenerator.DeclareLocal(sourceType);
            ilGenerator.Emit(OpCodes.Stloc, tempVar);

            // 检查是否为null
            ilGenerator.Emit(OpCodes.Ldloc, tempVar);
            ilGenerator.Emit(OpCodes.Brfalse, nullLabel);

            // 不为null，执行拆箱
            ilGenerator.Emit(OpCodes.Ldloc, tempVar);
            ilGenerator.Emit(OpCodes.Unbox_Any, targetType);
            ilGenerator.Emit(OpCodes.Br, endLabel);

            // 为null，抛出异常
            ilGenerator.MarkLabel(nullLabel);
            ilGenerator.Emit(OpCodes.Ldstr, $"无法将null转换为值类型 '{targetType.Name}'");
            ilGenerator.Emit(OpCodes.Newobj, typeof(InvalidCastException).GetConstructor([typeof(string)])!);
            ilGenerator.Emit(OpCodes.Throw);

            ilGenerator.MarkLabel(endLabel);
            return;
        }

        // 处理复杂类型转换：列表转列表（相同类型，无需转换）
        if (sourceType == typeof(List<object>) && targetType == typeof(List<object>))
        {
            return;
        }

        // 处理复杂类型转换：数组转数组（这里只处理相同元素类型的数组转换）
        if (sourceType.IsArray && targetType.IsArray)
        {
            return;
        }

        // 其他情况：尝试使用Convert.ChangeType进行值类型之间的转换
        if (sourceType.IsValueType && targetType.IsValueType)
        {
            var convertMethod = typeof(Convert).GetMethod($"To{targetType.Name}", [sourceType]);
            if (convertMethod != null)
            {
                ilGenerator.Emit(OpCodes.Call, convertMethod);
                return;
            }
            else
            {
                // 尝试使用ChangeType
                ilGenerator.Emit(OpCodes.Box, sourceType);
                ilGenerator.Emit(OpCodes.Ldtoken, targetType);
                ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);
                ilGenerator.Emit(OpCodes.Call,
                    typeof(Convert).GetMethod("ChangeType", [typeof(object), typeof(Type)])!);
                ilGenerator.Emit(OpCodes.Unbox_Any, targetType);
                return;
            }
        }

        // 处理引用类型到引用类型的转换
        if (!sourceType.IsValueType && !targetType.IsValueType)
        {
            // 检查类型是否兼容
            if (targetType.IsAssignableFrom(sourceType))
            {
                // 类型兼容，直接转换
                return;
            }
            else
            {
                // 类型不兼容，尝试使用Isinst指令进行类型检查和转换
                ilGenerator.Emit(OpCodes.Isinst, targetType);
                return;
            }
        }

        // 所有情况都不匹配，生成抛出异常的IL指令
        ilGenerator.Emit(OpCodes.Ldstr, $"不支持从类型 '{sourceType.Name}' 转换为类型 '{targetType.Name}'");
        ilGenerator.Emit(OpCodes.Newobj, typeof(InvalidCastException).GetConstructor([typeof(string)])!);
        ilGenerator.Emit(OpCodes.Throw);
    }

    /// <summary>
    /// 判断是否为数值类型
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>如果是数值类型则返回true，否则返回false</returns>
    /// <remarks>
    /// 支持的数值类型包括：int, double, long, float, short, byte
    /// </remarks>
    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(double) || type == typeof(long) ||
               type == typeof(float) || type == typeof(short) || type == typeof(byte);
    }

    /// <summary>
    /// 生成基本类型到字符串的转换IL指令
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="sourceType">源类型</param>
    /// <remarks>
    /// 该方法根据源类型的不同，选择合适的ToString方法生成IL指令。
    /// 对于值类型调用object.ToString()的情况，需要先进行装箱操作。
    /// </remarks>
    private static void GenerateToFromStringConversionIl(ILGenerator ilGenerator, Type sourceType)
    {
        var toStringMethod = sourceType switch
        {
            { } t when t == typeof(int) => typeof(Convert).GetMethod("ToString", [typeof(int)])!,
            { } t when t == typeof(double) => typeof(Convert).GetMethod("ToString", [typeof(double)])!,
            { } t when t == typeof(bool) => typeof(Convert).GetMethod("ToString", [typeof(bool)])!,
            { } t when t == typeof(char) => typeof(Convert).GetMethod("ToString", [typeof(char)])!,
            _ => typeof(object).GetMethod("ToString", Type.EmptyTypes)!
        };

        if (sourceType.IsValueType && toStringMethod.DeclaringType == typeof(object))
        {
            // 值类型调用object.ToString()需要先装箱
            ilGenerator.Emit(OpCodes.Box, sourceType);
        }

        ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
    }

    /// <summary>
    /// 生成字符串到基本类型的转换IL指令
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="source">表达式对象，用于错误报告</param>
    /// <remarks>
    /// 该方法根据目标类型的不同，选择合适的Convert方法生成IL指令。
    /// 如果不支持从字符串转换为目标类型，会抛出InvalidOperationError异常。
    /// </remarks>
    private static void GenerateFromStringConversionIl(ILGenerator ilGenerator, Type targetType, LangExpression source)
    {
        var convertMethod = targetType switch
        {
            { } t when t == typeof(int) => typeof(Convert).GetMethod("ToInt32", [typeof(string)])!,
            { } t when t == typeof(double) => typeof(Convert).GetMethod("ToDouble", [typeof(string)])!,
            { } t when t == typeof(bool) => typeof(Convert).GetMethod("ToBoolean", [typeof(string)])!,
            { } t when t == typeof(char) => typeof(Convert).GetMethod("ToChar", [typeof(string)])!,
            _ => throw new InvalidOperationError(source, $"不支持从字符串转换为类型 '{targetType.Name}'")
        };

        ilGenerator.Emit(OpCodes.Call, convertMethod);
    }

    /// <summary>
    /// 生成数值类型之间的转换IL指令
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <remarks>
    /// 该方法根据源类型和目标类型的不同，生成不同的转换IL指令。
    /// 对于常见的数值类型转换，直接使用IL转换指令；
    /// 对于其他数值类型转换，使用Convert类的方法。
    /// </remarks>
    private static void GenerateNumericConversionIl(ILGenerator ilGenerator, Type sourceType, Type targetType)
    {
        if (sourceType == typeof(int) && targetType == typeof(double))
        {
            // int转double
            ilGenerator.Emit(OpCodes.Conv_R8);
        }
        else if (sourceType == typeof(double) && targetType == typeof(int))
        {
            // double转int
            ilGenerator.Emit(OpCodes.Conv_I4);
        }
        else if (sourceType == typeof(char) && targetType == typeof(int))
        {
            // char转int
            ilGenerator.Emit(OpCodes.Conv_I4);
        }
        else if (sourceType == typeof(int) && targetType == typeof(char))
        {
            // int转char
            ilGenerator.Emit(OpCodes.Conv_U2);
        }
        else if (sourceType == typeof(double) && targetType == typeof(char))
        {
            // double转char
            ilGenerator.Emit(OpCodes.Conv_U2);
        }
        else if (sourceType == typeof(char) && targetType == typeof(double))
        {
            // char转double
            ilGenerator.Emit(OpCodes.Conv_R8);
        }
        else
        {
            // 其他数值类型转换，使用Convert类
            var convertMethod = typeof(Convert).GetMethod($"To{targetType.Name}", [sourceType]);
            if (convertMethod != null)
            {
                ilGenerator.Emit(OpCodes.Call, convertMethod);
            }
        }
    }

    /// <summary>
    /// 生成布尔值转换的IL指令
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <remarks>
    /// 该方法处理布尔值与其他类型之间的转换：
    /// - 布尔值转int：true->1, false->0
    /// - 布尔值转double：true->1.0, false->0.0
    /// - 布尔值转char：true->'\x01', false->'\x00'
    /// - 数值转布尔值：非零即真
    /// </remarks>
    private static void GenerateBoolConversionIl(ILGenerator ilGenerator, Type sourceType, Type targetType)
    {
        if (sourceType == typeof(bool))
        {
            // 布尔值转其他类型
            if (targetType == typeof(int))
            {
                // bool转int：true->1, false->0
                // 布尔值在栈上是1(true)或0(false)，直接转换为int即可
            }
            else if (targetType == typeof(double))
            {
                // bool转double：true->1.0, false->0.0
                ilGenerator.Emit(OpCodes.Conv_R8);
            }
            else if (targetType == typeof(char))
            {
                // bool转char：true->'\x01', false->'\x00'
                ilGenerator.Emit(OpCodes.Conv_U2);
            }
        }
        else
        {
            // 其他类型转布尔值
            if (IsNumericType(sourceType))
            {
                // 数值转bool：非零即真
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Cgt);
            }
        }
    }

    /// <summary>
    /// 生成字符类型转换的IL指令
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <remarks>
    /// 该方法处理字符类型与其他类型之间的转换：
    /// - char转bool：非'\x00'即真
    /// - 数值转char：使用Conv_U2指令
    /// </remarks>
    private static void GenerateCharConversionIl(ILGenerator ilGenerator, Type sourceType, Type targetType)
    {
        if (sourceType == typeof(char))
        {
            // char转其他类型
            if (targetType == typeof(bool))
            {
                // char转bool：非'\x00'即真
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Cgt);
            }
        }
        else if (targetType == typeof(char))
        {
            // 其他类型转char
            if (IsNumericType(sourceType))
            {
                // 数值转char
                ilGenerator.Emit(OpCodes.Conv_U2);
            }
        }
    }
}