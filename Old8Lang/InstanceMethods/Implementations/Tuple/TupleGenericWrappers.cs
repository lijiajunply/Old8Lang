using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Tuple;

// 查询和访问方法
public class TupleFirstMethod : LangListFirstMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleFirstOrDefaultMethod : LangListFirstOrDefaultMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleLastMethod : LangListLastMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleSkipMethod : LangListSkipMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleTakeMethod : LangListTakeMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleDistinctMethod : LangListDistinctMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleFindMethod : LangListFindMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleConcatMethod : LangListConcatMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleIndexOfMethod : LangListIndexOfMethod { public override Type TargetType => typeof(TupleLangValue); }
