using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 扩展方法声明语句的纯语法模型。
/// </summary>
/// <param name="targetTypeName">目标类型名称</param>
/// <param name="targetGenericParameters">目标泛型参数</param>
/// <param name="extensionMethods">扩展方法语法列表</param>
/// <param name="position">源代码位置信息</param>
public partial class ExtensionDeclaration(
    string targetTypeName,
    List<GenericParameter>? targetGenericParameters,
    List<FunctionSyntax> extensionMethods,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>目标类型名称（被扩展的类型）。</summary>
    public string TargetTypeName { get; } = targetTypeName;

    /// <summary>目标类型的泛型参数及约束。</summary>
    public List<GenericParameter>? TargetGenericParameters { get; } = targetGenericParameters;

    /// <summary>扩展方法语法列表。</summary>
    public List<FunctionSyntax> ExtensionMethods { get; } = extensionMethods;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitExtensionDeclaration(this);

    public override OldStatement? this[int index] => this;

    public override int Count => 0;
}
