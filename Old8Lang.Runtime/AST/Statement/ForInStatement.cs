using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.AsyncGeneration;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

public partial class ForInStatement(
    LangId id,
    LangExpression expression,
    OldStatement body,
    SourcePosition position = default,
    List<LangId>? additionalIds = null) : OldStatement(position)
{
    // 公共属性，用于外部访问
    public LangId Id => id;
    public LangExpression Expression => expression;
    public OldStatement Body => body;

    // 获取所有标识符，包括主标识符和附加标识符
    internal List<LangId> AllIds
    {
        get => [id, .. field];
    } = additionalIds ?? [];

    /// <summary>
    /// 获取循环绑定的全部标识符（首个标识符 + 附加标识符）
    /// </summary>
    /// <remarks>
    /// 形如 "for key, value in dict" 的循环会绑定两个标识符，
    /// 各执行模式需要据此决定是否对迭代元素做解构。
    /// </remarks>
    public IReadOnlyList<LangId> Ids => AllIds;







    public override OldStatement this[int index] => body[index]!;

    public override int Count => body.Count;
}