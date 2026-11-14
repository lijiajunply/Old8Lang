using System.Reflection.Emit;

namespace Old8Lang.AST.Statement;

/// <summary>
/// ReturnStatement 的编译期辅助实现（阶段 2 从语法节点迁出，语法节点不再引用 IL 类型）。
/// </summary>
internal static class ReturnStatementRuntimeExtensions
{
    /// <summary>
    /// 检查是否允许基本类型转换
    /// </summary>
    internal static bool IsBasicTypeConversionAllowed(Type expected, Type actual)
    {
        // 数值类型之间的转换
        var numericTypes = new[] { typeof(int), typeof(long), typeof(double), typeof(float), typeof(short), typeof(byte) };
        if (numericTypes.Contains(expected) && numericTypes.Contains(actual))
            return true;

        // 字符串转换
        if (expected == typeof(string))
            return true;

        return false;
    }

    internal static bool IsNumericType(Type type) => type == typeof(int) || type == typeof(double) ||
                                                     type == typeof(float) || type == typeof(long) ||
                                                     type == typeof(short) || type == typeof(byte);

    internal static void EmitNumericConversion(ILGenerator ilGenerator, Type targetType)
    {
        ilGenerator.Emit(targetType == typeof(double) ? OpCodes.Conv_R8 :
            targetType == typeof(float) ? OpCodes.Conv_R4 :
            targetType == typeof(long) ? OpCodes.Conv_I8 :
            targetType == typeof(short) ? OpCodes.Conv_I2 :
            targetType == typeof(byte) ? OpCodes.Conv_U1 : OpCodes.Conv_I4);
    }
}
