using Old8Lang.Compiler;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 表示一个已经求值的值的表达式
/// 用于在需要传递表达式但已经有值的情况下使用
/// </summary>
public class ValueExpression(LangValueType value, SourcePosition position = default) : LangExpression(position)
{
    public override LangValueType Run(VariateManager manager)
    {
        // 直接返回存储的值，不做任何计算
        return value;
    }

    public override Type? OutputType(LocalManager local)
    {
        return value.OutputType(local);
    }
}
