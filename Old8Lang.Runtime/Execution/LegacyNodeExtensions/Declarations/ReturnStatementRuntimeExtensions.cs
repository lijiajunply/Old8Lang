using System.Reflection.Emit;
using Old8Lang.AST.Expression;

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
        var expectedUnderlying = Nullable.GetUnderlyingType(expected) ?? expected;
        var actualUnderlying = Nullable.GetUnderlyingType(actual) ?? actual;

        if (expectedUnderlying == actualUnderlying)
            return true;

        if (NeedsRuntimeConversion(expected, actual))
            return true;

        // 数值类型之间的转换（含全部 CLR 数值类型）
        if (IsNumericType(expectedUnderlying) && IsNumericType(actualUnderlying))
            return true;

        return false;
    }

    internal static bool NeedsRuntimeConversion(Type expected, Type actual)
    {
        if (expected == actual)
            return false;

        var expectedUnderlying = Nullable.GetUnderlyingType(expected) ?? expected;
        var actualIsNullable = Nullable.GetUnderlyingType(actual) is not null;

        if (actualIsNullable && expected != typeof(object))
            return true;

        if (expectedUnderlying == typeof(string) ||
            expectedUnderlying == typeof(bool) ||
            expectedUnderlying == typeof(char) ||
            IsNumericType(expectedUnderlying))
        {
            if (actual == typeof(object) || typeof(LangValueType).IsAssignableFrom(actual))
                return true;

            if (expectedUnderlying == typeof(string))
                return true;

            if (actual == typeof(string) &&
                (expectedUnderlying == typeof(bool) || IsNumericType(expectedUnderlying)))
                return true;

            if (expectedUnderlying == typeof(bool) && actual == typeof(int))
                return true;
        }

        return false;
    }

    internal static bool IsNumericType(Type type) =>
        type == typeof(int) || type == typeof(double) ||
        type == typeof(float) || type == typeof(long) ||
        type == typeof(short) || type == typeof(byte) ||
        type == typeof(uint) || type == typeof(ulong) ||
        type == typeof(ushort) || type == typeof(sbyte) ||
        type == typeof(decimal);

    internal static bool IsUnsignedIntegerType(Type type) =>
        type == typeof(uint) || type == typeof(ulong) ||
        type == typeof(ushort) || type == typeof(byte) ||
        type == typeof(char);

    internal static void EmitNumericConversion(ILGenerator ilGenerator, Type sourceType, Type targetType)
    {
        if (sourceType == targetType)
            return;

        if (targetType == typeof(decimal))
        {
            var toDecimal = typeof(Convert).GetMethod(nameof(Convert.ToDecimal), [sourceType])
                ?? throw new InvalidOperationException($"不支持从 {sourceType.Name} 转换为 decimal");
            ilGenerator.Emit(OpCodes.Call, toDecimal);
            return;
        }

        if (sourceType == typeof(decimal))
        {
            var methodName = targetType switch
            {
                _ when targetType == typeof(double) => nameof(Convert.ToDouble),
                _ when targetType == typeof(float) => nameof(Convert.ToSingle),
                _ when targetType == typeof(long) => nameof(Convert.ToInt64),
                _ when targetType == typeof(ulong) => nameof(Convert.ToUInt64),
                _ when targetType == typeof(int) => nameof(Convert.ToInt32),
                _ when targetType == typeof(uint) => nameof(Convert.ToUInt32),
                _ when targetType == typeof(short) => nameof(Convert.ToInt16),
                _ when targetType == typeof(ushort) => nameof(Convert.ToUInt16),
                _ when targetType == typeof(sbyte) => nameof(Convert.ToSByte),
                _ when targetType == typeof(byte) => nameof(Convert.ToByte),
                _ => throw new InvalidOperationException($"不支持从 decimal 转换为 {targetType.Name}")
            };
            var fromDecimal = typeof(Convert).GetMethod(methodName, [typeof(decimal)])!;
            ilGenerator.Emit(OpCodes.Call, fromDecimal);
            return;
        }

        if (IsUnsignedIntegerType(sourceType) && (targetType == typeof(double) || targetType == typeof(float)))
        {
            ilGenerator.Emit(OpCodes.Conv_R_Un);
            ilGenerator.Emit(targetType == typeof(double) ? OpCodes.Conv_R8 : OpCodes.Conv_R4);
            return;
        }

        ilGenerator.Emit(
            targetType == typeof(double) ? OpCodes.Conv_R8 :
            targetType == typeof(float) ? OpCodes.Conv_R4 :
            targetType == typeof(long) ? OpCodes.Conv_I8 :
            targetType == typeof(ulong) ? OpCodes.Conv_U8 :
            targetType == typeof(uint) ? OpCodes.Conv_U4 :
            targetType == typeof(short) ? OpCodes.Conv_I2 :
            targetType == typeof(ushort) || targetType == typeof(char) ? OpCodes.Conv_U2 :
            targetType == typeof(sbyte) ? OpCodes.Conv_I1 :
            targetType == typeof(byte) ? OpCodes.Conv_U1 :
            OpCodes.Conv_I4);
    }

    internal static void EmitNumericConversion(ILGenerator ilGenerator, Type targetType) =>
        EmitNumericConversion(ilGenerator, typeof(int), targetType);
}
