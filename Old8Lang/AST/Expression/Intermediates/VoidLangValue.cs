using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 静态值，不可访问
/// </summary>
/// <param name="position">位置信息</param>
public class VoidLangValue(SourcePosition position = default) : LangValueType(position)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    public override object GetValue() => throw new InvalidOperationError(this, "尝试访问无效值（VoidValue）");
    
    public override LangValueType Run(LangParser.VariateManager manager) => 
        throw new InvalidOperationError(this, "尝试运行无效值（VoidValue）");
    
    public override string ToString() => 
        throw new InvalidOperationError(this, "尝试将无效值（VoidValue）转换为字符串");
}