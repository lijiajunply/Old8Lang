using System.Runtime.CompilerServices;
using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;
/// <summary>
/// 表示值
/// </summary>
public class OldValue : OldExpr
{
    public object Value { get; set; }

    public virtual OldValue PLUS(OldValue otherValue) => new OldValue();

    public virtual OldValue MINUS(OldValue otherValue) => new OldValue();

    public virtual OldValue TIMES(OldValue otherValue) => new OldValue();

    public virtual OldValue DIVIDE(OldValue otherValue) => new OldValue();

}