using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Array;

// 基础方法
public class ArrayCountMethod : LangListCountMethod { public override Type TargetType => typeof(ArrayLangValue); }

// 查询和访问方法
public class ArrayFirstMethod : LangListFirstMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayFirstOrDefaultMethod : LangListFirstOrDefaultMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayLastMethod : LangListLastMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayLastOrDefaultMethod : LangListLastOrDefaultMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArraySkipMethod : LangListSkipMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayTakeMethod : LangListTakeMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayDistinctMethod : LangListDistinctMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayFindMethod : LangListFindMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayConcatMethod : LangListConcatMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayIndexOfMethod : LangListIndexOfMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayElementAtMethod : LangListElementAtMethod { public override Type TargetType => typeof(ArrayLangValue); }

// 高阶函数方法
public class ArrayFilterMethod : LangListFilterMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayMapMethod : LangListMapMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayAnyMethod : LangListAnyMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayAllMethod : LangListAllMethod { public override Type TargetType => typeof(ArrayLangValue); }

// 聚合方法
public class ArraySumMethod : LangListSumMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayAverageMethod : LangListAverageMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayMinMethod : LangListMinMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayMaxMethod : LangListMaxMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayReduceMethod : LangListReduceMethod { public override Type TargetType => typeof(ArrayLangValue); }

// 迭代方法
public class ArrayForEachMethod : LangListForEachMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayJoinMethod : LangListJoinMethod { public override Type TargetType => typeof(ArrayLangValue); }

// 集合操作方法
public class ArrayUnionMethod : LangListUnionMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayIntersectMethod : LangListIntersectMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayExceptMethod : LangListExceptMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayZipMethod : LangListZipMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayGroupByMethod : LangListGroupByMethod { public override Type TargetType => typeof(ArrayLangValue); }

// 排序和其他方法
public class ArraySortMethod : LangListSortMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayIsSortedMethod : LangListIsSortedMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayToStrMethod : LangListToStrMethod { public override Type TargetType => typeof(ArrayLangValue); }

// 高级组合方法
public class ArrayZip3Method : LangListZip3Method { public override Type TargetType => typeof(ArrayLangValue); }
public class ArraySelectManyMethod : LangListSelectManyMethod { public override Type TargetType => typeof(ArrayLangValue); }

// 带选择器的聚合方法
public class ArraySortByMethod : LangListSortByMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArraySumWithSelectorMethod : LangListSumWithSelectorMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayAverageWithSelectorMethod : LangListAverageWithSelectorMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayMinWithSelectorMethod : LangListMinWithSelectorMethod { public override Type TargetType => typeof(ArrayLangValue); }
public class ArrayMaxWithSelectorMethod : LangListMaxWithSelectorMethod { public override Type TargetType => typeof(ArrayLangValue); }
