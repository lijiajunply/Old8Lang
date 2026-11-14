using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// 条件 / 逻辑运算的操作数求值助手。
/// </summary>
/// <remarks>
/// 静态类型为 <c>object</c> 的表达式（未标注类字段上的方法调用、字典索引结果等）
/// 在运行期可能是装箱的 <c>bool</c>。如果直接把它交给 <c>brfalse</c> / <c>xor</c>，
/// CLR 会按「引用是否为空」判断——装箱值永远非空，于是条件恒真、取反恒假。
/// 这里统一在发射处把动态值转成原生 <c>bool</c>，与解释器的严格布尔语义一致。
/// </remarks>
internal static class ConditionIlHelper
{
    /// <summary>
    /// 发射「求值表达式并得到原生 bool」的 IL。
    /// </summary>
    public static void EmitBoolOperand(LangExpression? expression, ILGenerator ilGenerator, LocalManager local)
    {
        if (expression is null)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            return;
        }

        var operandType = expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        expression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

        // 只处理静态类型为 object 的动态值：其它类型沿用原有的 brfalse / xor 语义，
        // 避免改变 `if 0`、字符串等既有的真值判断行为。
        if (operandType != typeof(object))
        {
            return;
        }

        // object 可能是装箱 bool 或 BoolLangValue：交给运行期按语言语义转换
        ilGenerator.Emit(OpCodes.Call, typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.ToBoolean))!);
    }
}
