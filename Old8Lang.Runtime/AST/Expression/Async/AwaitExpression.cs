using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// await 表达式
/// 用于等待异步操作完成并获取结果
/// </summary>
public partial class AwaitExpression : LangExpression
{
    public readonly LangExpression Expression;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AwaitExpression(LangExpression expression, SourcePosition position = default)
        : base(position)
    {
        Expression = expression;
    }


}