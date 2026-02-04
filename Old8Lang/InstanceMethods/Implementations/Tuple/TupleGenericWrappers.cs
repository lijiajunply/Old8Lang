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

// 聚合方法
public class TupleSumMethod : LangListSumMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleAverageMethod : LangListAverageMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleMinMethod : LangListMinMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleMaxMethod : LangListMaxMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleReduceMethod : LangListReduceMethod { public override Type TargetType => typeof(TupleLangValue); }

// 迭代方法
public class TupleForEachMethod : LangListForEachMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleJoinMethod : LangListJoinMethod { public override Type TargetType => typeof(TupleLangValue); }

// 集合操作方法
public class TupleUnionMethod : LangListUnionMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleIntersectMethod : LangListIntersectMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleExceptMethod : LangListExceptMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleZipMethod : LangListZipMethod { public override Type TargetType => typeof(TupleLangValue); }
public class TupleGroupByMethod : LangListGroupByMethod { public override Type TargetType => typeof(TupleLangValue); }
