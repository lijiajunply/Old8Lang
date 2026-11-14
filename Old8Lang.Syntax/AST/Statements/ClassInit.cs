using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 类定义语句：承载纯语法模型，运行时 TypeTemplate 由 <c>ClassInitRuntimeExtensions.GetTypeTemplate</c> 按需创建。
/// </summary>
/// <param name="syntax">类语法模型</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
public partial class ClassInit(ClassSyntax syntax, SourcePosition position = default)
    : OldStatement(position), IBlockDeclaration
{
    /// <summary>类语法模型（类名、父类、接口、泛型、类体与文档注释）。</summary>
    public readonly ClassSyntax Syntax = syntax;

    /// <summary>类名。</summary>
    public string ClassName => Syntax.ClassName;

    /// <summary>类体声明。</summary>
    public BlockStatement Body => Syntax.Body;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitClassInit(this);

    /// <summary>获取指定索引处的语句（实现OldStatement接口）。</summary>
    public override OldStatement this[int index] => this;

    /// <summary>获取语句数量（实现OldStatement接口）。</summary>
    public override int Count => 0;

    /// <summary>将类定义转换为字符串表示。</summary>
    public override string ToString() => Syntax.ToString();
}
