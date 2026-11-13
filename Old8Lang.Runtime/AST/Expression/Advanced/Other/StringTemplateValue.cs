using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 字符串插值
/// </summary>
/// <param name="list"></param>
/// <param name="position"></param>
public partial class StringTemplateValue(List<LangExpression> list, SourcePosition position = default) : LangValueType(position)
{
    /// <summary>
    /// 字符串模板中的表达式列表
    /// </summary>
    public List<LangExpression> ExpressionList { get; } = list;

}