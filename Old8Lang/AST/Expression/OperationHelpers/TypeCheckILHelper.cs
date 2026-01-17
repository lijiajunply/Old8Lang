using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// 类型检查和转换 IL 代码生成助手类
/// </summary>
/// <remarks>
/// 该类负责生成类型检查（is、is not）和类型转换（as）运算符的IL代码。
///
/// 支持的运算符：
/// - AS (as): 安全类型转换，失败时返回默认值
/// - IS (is): 类型检查，检查值是否为指定类型
/// - IS NOT (is not): 否定类型检查，检查值是否不是指定类型
/// </remarks>
public static class TypeCheckILHelper
{
    /// <summary>
    /// 类型名称到 .NET 类型的映射
    /// </summary>
    private static readonly Dictionary<string, Type?> TypeNameMap = new()
    {
        { "int", typeof(int) },
        { "double", typeof(double) },
        { "string", typeof(string) },
        { "bool", typeof(bool) },
        { "char", typeof(char) },
        { "list", typeof(List<object>) },
        { "array", typeof(object[]) },
        { "dictionary", typeof(Dictionary<object, object>) },
        { "null", null } // null 类型特殊处理
    };

    /// <summary>
    /// 根据类型名称获取对应的 .NET 类型
    /// </summary>
    /// <param name="typeName">类型名称（Old8Lang类型名）</param>
    /// <returns>.NET 类型，如果是 null 或未识别的自定义类型则返回 null</returns>
    private static Type? GetTargetType(string typeName)
    {
        return TypeNameMap.GetValueOrDefault(typeName);
    }

    /// <summary>
    /// 生成类型转换运算符（as）的IL代码
    /// </summary>
    /// <param name="left">左操作数表达式（要转换的值）</param>
    /// <param name="right">右操作数表达式（目标类型标识符）</param>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="leftType">左操作数的类型</param>
    /// <param name="operation">操作表达式（用于错误报告）</param>
    /// <returns>转换后的类型</returns>
    /// <remarks>
    /// as 运算符执行安全类型转换：
    /// - 如果右侧是 LangId，提取类型名称并转换为 .NET 类型
    /// - 使用 TypeConversion.GenerateTypeConversionIl 生成实际的转换IL代码
    /// - 如果转换失败，通常返回默认值（由 TypeConversion 处理）
    ///
    /// IL代码结构：
    /// <code>
    /// [加载左侧值]
    /// [调用 TypeConversion.GenerateTypeConversionIl 生成转换指令]
    /// </code>
    /// </remarks>
    public static Type GenerateAsOperator(
        LangExpression left,
        LangExpression right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Operation operation)
    {
        // 处理类型转换操作：left as right
        // 右侧应该是一个类型标识符，如 int, double, string 等
        if (right is LangId rightLangId)
        {
            var typeName = rightLangId.IdName;
            // 加载左侧值
            left.LoadIlValue(ilGenerator, local);

            // 根据类型名称生成转换指令
            var targetType = GetTargetType(typeName) ?? typeof(object);

            // 确保leftType不为null
            leftType ??= typeof(object);

            // 特殊处理：List<T> as array 应该返回 T[] 而不是 object[]
            if (typeName == "array" &&
                leftType.IsGenericType &&
                leftType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // 获取List的元素类型
                var elementType = leftType.GetGenericArguments()[0];
                var arrayType = elementType.MakeArrayType();

                // 统一处理类型转换
                TypeConversion.GenerateTypeConversionIl(ilGenerator, leftType, targetType, operation);

                // 返回实际的数组类型
                return arrayType;
            }

            // 统一处理类型转换
            TypeConversion.GenerateTypeConversionIl(ilGenerator, leftType, targetType, operation);

            return targetType;
        }

        // 非LangId类型，返回object类型
        left.LoadIlValue(ilGenerator, local);
        return typeof(object);
    }

    /// <summary>
    /// 生成类型检查运算符（is）的IL代码
    /// </summary>
    /// <param name="left">左操作数表达式（要检查的值）</param>
    /// <param name="right">右操作数表达式（目标类型标识符）</param>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="leftType">左操作数的类型</param>
    /// <returns>检查结果的类型（bool）</returns>
    /// <remarks>
    /// is 运算符检查值是否为指定类型：
    /// - 对于 null 类型：检查值是否为 null
    /// - 对于值类型：使用 isinst 指令检查是否是装箱后的该类型
    /// - 对于引用类型：直接使用 isinst 指令
    ///
    /// IL代码结构（引用类型）：
    /// <code>
    /// [加载左侧值]
    /// isinst targetType    // 尝试转换为目标类型，失败返回 null
    /// ldnull
    /// cgt.un               // 比较是否不为 null (即转换成功)
    /// </code>
    ///
    /// IL代码结构（值类型）：
    /// <code>
    /// [加载左侧值]
    /// isinst object        // 检查是否为 object
    /// dup
    /// brfalse isValueTypeLabel
    /// pop
    /// [重新加载左侧值]
    /// isinst targetType
    /// ldnull
    /// cgt.un
    /// br endLabel
    /// isValueTypeLabel:
    /// pop
    /// ldc.i4.0             // false
    /// endLabel:
    /// </code>
    /// </remarks>
    public static Type GenerateIsOperator(
        LangExpression left,
        LangExpression right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType)
    {
        // 处理类型检查操作：left is right
        // 右侧应该是一个类型标识符，如 int, double, string 等
        if (right is LangId rightLangId)
        {
            var typeName = rightLangId.IdName;
            var targetType = GetTargetType(typeName);

            // 加载左侧值
            left.LoadIlValue(ilGenerator, local);

            if (targetType is null && typeName == "null")
            {
                // 检查是否为 null
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Ceq);
            }
            else if (targetType is not null)
            {
                GenerateTypeCheck(left, ilGenerator, local, leftType, targetType, isNegated: false);
            }
            else
            {
                // 对于自定义类型，暂不支持，返回 false
                ilGenerator.Emit(OpCodes.Pop); // 弹出左侧值
                ilGenerator.Emit(OpCodes.Ldc_I4_0); // false
            }

            return typeof(bool);
        }

        // 非LangId类型，返回false
        left.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        return typeof(bool);
    }

    /// <summary>
    /// 生成否定类型检查运算符（is not）的IL代码
    /// </summary>
    /// <param name="left">左操作数表达式（要检查的值）</param>
    /// <param name="right">右操作数表达式（目标类型标识符）</param>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="leftType">左操作数的类型</param>
    /// <returns>检查结果的类型（bool）</returns>
    /// <remarks>
    /// is not 运算符检查值是否不是指定类型：
    /// - 对于 null 类型：检查值是否不为 null
    /// - 对于其他类型：执行 is 检查后取反
    ///
    /// IL代码结构（在 is 检查的基础上）：
    /// <code>
    /// [is 运算符的IL代码]
    /// ldc.i4.1             // 加载 true
    /// xor                  // 异或取反
    /// </code>
    /// </remarks>
    public static Type GenerateIsNotOperator(
        LangExpression left,
        LangExpression right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType)
    {
        // 处理否定类型检查操作：left is not right
        // 右侧应该是一个类型标识符，如 int, double, string 等
        if (right is LangId rightLangId)
        {
            var typeName = rightLangId.IdName;
            var targetType = GetTargetType(typeName);

            // 加载左侧值
            left.LoadIlValue(ilGenerator, local);

            if (targetType is null && typeName == "null")
            {
                // 检查是否不为 null
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Ceq);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor); // 取反
            }
            else if (targetType is not null)
            {
                GenerateTypeCheck(left, ilGenerator, local, leftType, targetType, isNegated: true);
            }
            else
            {
                // 对于自定义类型，暂不支持，返回 true (不是该类型)
                ilGenerator.Emit(OpCodes.Pop); // 弹出左侧值
                ilGenerator.Emit(OpCodes.Ldc_I4_1); // true
            }

            return typeof(bool);
        }

        // 非LangId类型，返回true
        left.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        return typeof(bool);
    }

    /// <summary>
    /// 生成类型检查的核心IL代码
    /// </summary>
    /// <param name="left">左操作数表达式</param>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="leftType">左操作数的类型</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="isNegated">是否取反（is not 运算符）</param>
    /// <remarks>
    /// 该方法生成实际的类型检查IL代码，处理值类型和引用类型的不同情况。
    /// </remarks>
    private static void GenerateTypeCheck(
        LangExpression left,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type targetType,
        bool isNegated)
    {
        // 确保左侧值是 object 类型以便进行类型检查
        if (leftType is not null && leftType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, leftType);
        }

        if (targetType.IsValueType)
        {
            // 对于值类型，检查是否是装箱后的该类型
            GenerateValueTypeCheck(left, ilGenerator, local, leftType, targetType, isNegated);
        }
        else
        {
            // 对于引用类型，使用 isinst 指令
            GenerateReferenceTypeCheck(ilGenerator, targetType, isNegated);
        }
    }

    /// <summary>
    /// 生成值类型检查的IL代码
    /// </summary>
    private static void GenerateValueTypeCheck(
        LangExpression left,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type targetType,
        bool isNegated)
    {
        ilGenerator.Emit(OpCodes.Isinst, typeof(object));
        ilGenerator.Emit(OpCodes.Dup);
        var isValueTypeLabel = ilGenerator.DefineLabel();
        var endLabel = ilGenerator.DefineLabel();

        ilGenerator.Emit(OpCodes.Brfalse, isValueTypeLabel); // 如果是 null，跳转

        // 不是 null，检查具体类型
        ilGenerator.Emit(OpCodes.Pop); // 弹出栈顶
        left.LoadIlValue(ilGenerator, local);
        if (leftType is not null && leftType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, leftType);
        }

        ilGenerator.Emit(OpCodes.Isinst, targetType);
        ilGenerator.Emit(OpCodes.Ldnull);
        ilGenerator.Emit(OpCodes.Cgt_Un); // 不等于 null 则为 true

        if (isNegated)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Xor); // 取反
        }

        ilGenerator.Emit(OpCodes.Br, endLabel);

        ilGenerator.MarkLabel(isValueTypeLabel);
        ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(isNegated ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0); // 根据是否取反返回 true/false

        ilGenerator.MarkLabel(endLabel);
    }

    /// <summary>
    /// 生成引用类型检查的IL代码
    /// </summary>
    private static void GenerateReferenceTypeCheck(
        ILGenerator ilGenerator,
        Type targetType,
        bool isNegated)
    {
        // 对于引用类型，使用 isinst 指令
        ilGenerator.Emit(OpCodes.Isinst, targetType);
        ilGenerator.Emit(OpCodes.Ldnull);
        ilGenerator.Emit(OpCodes.Cgt_Un); // 不等于 null 则为 true

        if (isNegated)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Xor); // 取反
        }
    }
}