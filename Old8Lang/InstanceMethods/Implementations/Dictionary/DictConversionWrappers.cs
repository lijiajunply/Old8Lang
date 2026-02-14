using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Implementations.Generic;

namespace Old8Lang.InstanceMethods.Implementations.Dictionary;

// Dictionary 转换方法包装器
public class DictToArrayMethod : LangListToArrayMethod { public override Type TargetType => typeof(DictionaryLangValue); }
public class DictToTupleMethod : LangListToTupleMethod { public override Type TargetType => typeof(DictionaryLangValue); }
public class DictToDictMethod : LangListToDictMethod { public override Type TargetType => typeof(DictionaryLangValue); }
