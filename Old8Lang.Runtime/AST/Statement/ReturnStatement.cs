using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Statement;

public partial class ReturnStatement(LangExpression returnExpression, SourcePosition position = default) : OldStatement(position)
{
    public LangExpression Expression => returnExpression;

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

    public override OldStatement? this[int index] => null;

    public override int Count => 0;

    public override string ToString() => $"return {returnExpression}";
}
