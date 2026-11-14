using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 函数/异步函数声明的纯语法模型（返回类型注解沿用 <see cref="LangId.AssumptionType"/> 的历史表示）。
/// </summary>
/// <remarks>
/// 运行时可调用值由 Runtime 的工厂按需创建，并通过弱引用表保持同一语法节点的对象身份。
/// </remarks>
public sealed class FunctionSyntax(
    LangId? id,
    List<LangId> parameters,
    BlockStatement body,
    List<GenericParameter>? genericParameters,
    List<FunctionDecorator>? decorators,
    DocCommentInfo? docComment,
    bool isAsync,
    SourcePosition position = default) : IOldLangTree
{
    /// <summary>函数名与返回类型注解。</summary>
    public LangId? Id { get; } = id;

    /// <summary>参数列表。</summary>
    public List<LangId> Parameters { get; } = parameters;

    /// <summary>函数体。</summary>
    public BlockStatement Body { get; } = body;

    /// <summary>泛型参数与约束。</summary>
    public List<GenericParameter>? GenericParameters { get; } = genericParameters;

    /// <summary>装饰器列表（源码顺序）。</summary>
    public List<FunctionDecorator>? Decorators { get; } = decorators;

    /// <summary>结构化文档注释。</summary>
    public DocCommentInfo? DocComment { get; } = docComment;

    /// <summary>是否为 async 函数。</summary>
    public bool IsAsync { get; } = isAsync;

    public SourcePosition Position { get; } = position;

    public TResult Accept<TResult>(IVisitor<TResult> visitor) =>
        throw new NotSupportedException("FunctionSyntax 是函数声明的内部语法模型，不支持 Visitor 访问");

    public override string ToString()
    {
        var prefix = IsAsync ? "async func " : "func ";
        return $"{prefix}{Id}({string.Join(", ", Parameters)})";
    }
}
