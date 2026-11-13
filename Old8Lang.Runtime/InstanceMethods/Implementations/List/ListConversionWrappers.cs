using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.List;

// List 转换方法包装器
public class ListToListMethod : LangListToListMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListToTupleMethod : LangListToTupleMethod { public override Type TargetType => typeof(ListLangValue); }
public class ListToDictMethod : LangListToDictMethod { public override Type TargetType => typeof(ListLangValue); }
