using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Tuple;

// Tuple 转换方法包装器
public class TupleToArrayMethod : LangListToArrayMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleToTupleMethod : LangListToTupleMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleToDictMethod : LangListToDictMethod { public override Type TargetType => typeof(TupleLangValue); }
