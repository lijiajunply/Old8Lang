
// ReSharper disable CheckNamespace
namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// FuncLangValue - Visitor支持
/// </summary>
public partial class FuncLangValue
{
    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        return ((Old8Lang.AST.Visitor.IRuntimeVisitor<TResult>)visitor).VisitFuncLangValue(this);
    }

}
