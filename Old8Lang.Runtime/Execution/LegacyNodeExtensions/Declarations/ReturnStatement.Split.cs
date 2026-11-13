using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// ReturnStatement 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class ReturnStatement
{
    internal static void EmitNumericConversion(ILGenerator ilGenerator, Type targetType)
    {
        ilGenerator.Emit(targetType == typeof(double) ? OpCodes.Conv_R8 :
            targetType == typeof(float) ? OpCodes.Conv_R4 :
            targetType == typeof(long) ? OpCodes.Conv_I8 :
            targetType == typeof(short) ? OpCodes.Conv_I2 :
            targetType == typeof(byte) ? OpCodes.Conv_U1 : OpCodes.Conv_I4);
    }
}
