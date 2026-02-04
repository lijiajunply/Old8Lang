using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Tuple;

/// <summary>
/// Tuple.Filter(predicate) - 使用通用 ILangList 实现
/// </summary>
public class TupleFilterMethod : LangListFilterMethod
{
    public override Type TargetType => typeof(TupleLangValue);
}
