using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Array;

// 查询和访问方法
public class ArrayFirstMethod : LangListFirstMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayFirstOrDefaultMethod : LangListFirstOrDefaultMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayLastMethod : LangListLastMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArraySkipMethod : LangListSkipMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayTakeMethod : LangListTakeMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayDistinctMethod : LangListDistinctMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayFindMethod : LangListFindMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayConcatMethod : LangListConcatMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayIndexOfMethod : LangListIndexOfMethod { public override Type TargetType => typeof(ArrayLangValue); }

// 高阶函数方法
public class ArrayFilterMethod : LangListFilterMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayMapMethod : LangListMapMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayAnyMethod : LangListAnyMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayAllMethod : LangListAllMethod { public override Type TargetType => typeof(ArrayLangValue); }
