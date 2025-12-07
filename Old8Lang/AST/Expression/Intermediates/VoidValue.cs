using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 构造函数
/// </summary>
/// <param name="position">位置信息</param>
public class VoidValue(SourcePosition position = default) : ValueType(position)
{
    public override object GetValue() => throw new InvalidOperationError(this, "尝试访问无效值（VoidValue）");
    
    public override ValueType Run(LangParser.VariateManager manager) => 
        throw new InvalidOperationError(this, "尝试运行无效值（VoidValue）");
    
    public override string ToString() => 
        throw new InvalidOperationError(this, "尝试将无效值（VoidValue）转换为字符串");
}