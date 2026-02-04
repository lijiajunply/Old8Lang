using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.List;

/// <summary>
/// List 类型的通用方法包装器
/// 将 Generic 目录下的 ILangList 通用方法包装为 ListLangValue 专用方法
/// </summary>

// 查询和访问方法
public class ListFirstMethod : LangListFirstMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListFirstOrDefaultMethod : LangListFirstOrDefaultMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListLastMethod : LangListLastMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListLastOrDefaultMethod : LangListLastOrDefaultMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListSkipMethod : LangListSkipMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListTakeMethod : LangListTakeMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListDistinctMethod : LangListDistinctMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListFindMethod : LangListFindMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListConcatMethod : LangListConcatMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListIndexOfMethod : LangListIndexOfMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListElementAtMethod : LangListElementAtMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListContainsMethod : LangListContainsMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListCountMethod : LangListCountMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListReverseMethod : LangListReverseMethod { public override Type TargetType => typeof(ListLangValue); }

// 高阶函数方法
public class ListFilterMethod : LangListFilterMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListMapMethod : LangListMapMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListAnyMethod : LangListAnyMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListAllMethod : LangListAllMethod { public override Type TargetType => typeof(ListLangValue); }

// 聚合方法
public class ListSumMethod : LangListSumMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListAverageMethod : LangListAverageMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListMinMethod : LangListMinMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListMaxMethod : LangListMaxMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListReduceMethod : LangListReduceMethod { public override Type TargetType => typeof(ListLangValue); }

// 迭代方法
public class ListForEachMethod : LangListForEachMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListJoinMethod : LangListJoinMethod { public override Type TargetType => typeof(ListLangValue); }

// 集合操作方法
public class ListUnionMethod : LangListUnionMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListIntersectMethod : LangListIntersectMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListExceptMethod : LangListExceptMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListZipMethod : LangListZipMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListGroupByMethod : LangListGroupByMethod { public override Type TargetType => typeof(ListLangValue); }

// 排序和其他方法
public class ListSortMethod : LangListSortMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListIsSortedMethod : LangListIsSortedMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListToStrMethod : LangListToStrMethod { public override Type TargetType => typeof(ListLangValue); }
