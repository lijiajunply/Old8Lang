using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Tuple;

/// <summary>
/// Tuple.Any(predicate?) - 使用通用 ILangList 实现
/// </summary>
public class TupleAnyMethod : LangListAnyMethod
{
    public override Type TargetType => typeof(TupleLangValue);
}
