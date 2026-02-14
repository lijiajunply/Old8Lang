using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Array;

// Array 转换方法包装器
public class ArrayToArrayMethod : LangListToArrayMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayToTupleMethod : LangListToTupleMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayToDictMethod : LangListToDictMethod { public override Type TargetType => typeof(ArrayLangValue); }
