using Old8Lang.AST.Expression;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 类/接口/混入声明的纯语法模型。
/// </summary>
/// <remarks>
/// 运行时 <c>TypeTemplate</c> 由 Runtime 工厂按需创建，并从 <see cref="Body"/> 收集成员。
/// </remarks>
public sealed class ClassSyntax(
    string className,
    BlockStatement body,
    string? parentClassName = null,
    bool isMixin = false,
    List<string>? mixinNames = null,
    List<string>? implementsNames = null,
    bool isInterface = false,
    bool isAbstract = false,
    List<GenericParameter>? genericParameters = null,
    List<string>? parentGenericTypeParameters = null,
    DocCommentInfo? docComment = null,
    SourcePosition position = default,
    SourcePosition namePosition = default) : IOldLangTree
{
    /// <summary>类名。</summary>
    public string ClassName { get; } = className;

    /// <summary>类名标识符的源码位置（未提供时回退到声明起始位置）。</summary>
    public SourcePosition NamePosition { get; } = namePosition.Line > 0 ? namePosition : position;

    /// <summary>类体（字段、方法、嵌套类等声明）。</summary>
    public BlockStatement Body { get; } = body;

    /// <summary>父类名。</summary>
    public string? ParentClassName { get; } = parentClassName;

    /// <summary>是否为 mixin。</summary>
    public bool IsMixin { get; } = isMixin;

    /// <summary>混入的类名列表。</summary>
    public List<string> MixinNames { get; } = mixinNames ?? [];

    /// <summary>实现的接口名列表（接口声明时为父接口）。</summary>
    public List<string> ImplementsNames { get; } = implementsNames ?? [];

    /// <summary>是否为接口。</summary>
    public bool IsInterface { get; } = isInterface;

    /// <summary>是否为抽象类。</summary>
    public bool IsAbstract { get; } = isAbstract;

    /// <summary>泛型参数与约束。</summary>
    public List<GenericParameter>? GenericParameters { get; } = genericParameters;

    /// <summary>父类的泛型实参。</summary>
    public List<string>? ParentGenericTypeParameters { get; } = parentGenericTypeParameters;

    /// <summary>结构化文档注释。</summary>
    public DocCommentInfo? DocComment { get; } = docComment;

    public SourcePosition Position { get; } = position;

    public TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor) =>
        throw new NotSupportedException("ClassSyntax 是类声明的内部语法模型，不支持 Visitor 访问");

    public override string ToString()
    {
        var prefix = IsInterface ? "interface" : IsMixin ? "mixin" : IsAbstract ? "abstract class" : "class";
        return $"{prefix} {ClassName} {{ ... }}";
    }
}
