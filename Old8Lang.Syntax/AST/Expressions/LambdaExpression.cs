using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// Lambda 表达式（普通或 async）的语法节点。
/// </summary>
/// <remarks>
/// 该节点只承载语法数据；求值/编译时由各后端通过 Runtime 工厂转换为
/// <c>FuncLangValue</c> 或 <c>AsyncFuncLangValue</c>。
/// </remarks>
/// <param name="id">返回类型注解的历史表示（IdName 为空，AssumptionType 为返回类型）</param>
/// <param name="parameters">参数列表</param>
/// <param name="body">函数体</param>
/// <param name="isAsync">是否为 async lambda</param>
/// <param name="position">源代码位置</param>
public sealed class LambdaExpression(
    LangId? id,
    List<LangId> parameters,
    BlockStatement body,
    bool isAsync,
    SourcePosition position = default) : LangExpression(position)
{
    /// <summary>返回类型注解的历史表示。</summary>
    public LangId? Id => id;

    /// <summary>参数列表。</summary>
    public List<LangId> Parameters => parameters;

    /// <summary>函数体。</summary>
    public BlockStatement Body => body;

    /// <summary>是否为 async lambda。</summary>
    public bool IsAsync => isAsync;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitLambdaExpression(this);

    public override string ToString()
    {
        var prefix = isAsync ? "async " : "";
        return $"{prefix}({string.Join(", ", parameters)}) -> {body}";
    }
}
