using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.Contains(item) - 使用通用 ILangList 实现
/// </summary>
public class ArrayContainsMethod : LangListContainsMethod
{
    public override Type TargetType => typeof(ArrayLangValue);
}
