using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Array;

/// <summary>
/// Array.Reverse() - 使用通用 ILangList 实现
/// </summary>
public class ArrayReverseMethod : LangListReverseMethod
{
    public override Type TargetType => typeof(ArrayLangValue);
}
