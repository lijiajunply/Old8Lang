using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Specialization;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 泛型实例化表达式
/// 例如: Box&lt;int>(), map&lt;string>(arr, func)
/// </summary>
public partial class GenericInstanceExpression : LangExpression
{
    /// <summary>
    /// 基础表达式（通常是标识符）
    /// 例如: Box, map
    /// </summary>
    public LangExpression BaseExpression { get; }

    /// <summary>
    /// 类型参数列表（类型名称字符串）
    /// 例如: ["int"], ["string", "Person"]
    /// </summary>
    public List<string> TypeArguments { get; }

    /// <summary>
    /// 调用参数（如果是函数调用）
    /// 例如: Box&lt;int>() 时为空列表，map&lt;string>(arr, func) 时为 [arr, func]
    /// </summary>
    public List<LangExpression>? CallArguments { get; }

    /// <summary>
    /// 构造函数（泛型类实例化）
    /// </summary>
    public GenericInstanceExpression(
        LangExpression baseExpression,
        List<string> typeArguments,
        SourcePosition position = default)
    {
        BaseExpression = baseExpression;
        TypeArguments = typeArguments;
        CallArguments = null;
        Position = position;
    }

    /// <summary>
    /// 构造函数（泛型函数调用）
    /// </summary>
    public GenericInstanceExpression(
        LangExpression baseExpression,
        List<string> typeArguments,
        List<LangExpression> callArguments,
        SourcePosition position = default)
    {
        BaseExpression = baseExpression;
        TypeArguments = typeArguments;
        CallArguments = callArguments;
        Position = position;
    }

    /// <summary>
    /// 是否为函数调用
    /// </summary>
    public bool IsFunctionCall => CallArguments is not null;






    public override string ToString()
    {
        var typeArgsStr = string.Join(", ", TypeArguments);
        if (IsFunctionCall)
        {
            var argsStr = string.Join(", ", CallArguments!.Select(a => a.ToString()));
            return $"{BaseExpression}<{typeArgsStr}>({argsStr})";
        }

        return $"{BaseExpression}<{typeArgsStr}>";
    }
}
