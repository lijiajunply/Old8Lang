using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// DictionaryLangValue类型的扩展方法类，提供字典操作功能
/// </summary>
[Serializable]
public static class DictionaryValueFuncStatic
{
    extension(DictionaryLangValue langValue)
    {
        /// <summary>
        /// 向字典中添加键值对
        /// </summary>
        /// <param name="value1">键</param>
        /// <param name="value2">值</param>
        /// <returns>添加的键值对作为TupleLangValue</returns>
        public TupleLangValue Add(LangValueType value1, LangValueType value2)
        {
            langValue.Value.Add((value1, value2));
            return new TupleLangValue(value1, value2);
        }

        /// <summary>
        /// 根据键获取字典中的值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <returns>对应的值</returns>
        public LangValueType GetValue(LangValueType key)
        {
            return langValue.Value.First(x => x.Key.Equal(key)).Value;
        }

        /// <summary>
        /// 根据键从字典中移除键值对
        /// </summary>
        /// <param name="key">要移除的键</param>
        /// <returns>被移除的值</returns>
        /// <exception cref="KeyError">当键不存在时抛出</exception>
        public LangValueType Remove(LangValueType key)
        {
            for (var i = 0; i < langValue.Value.Count; i++)
            {
                if (!langValue.Value[i].Key.Equal(key)) continue;
                var a = langValue.Value[i].Value;
                langValue.Value.RemoveAt(i);
                return a;
            }

            throw new KeyError(langValue, "键不存在");
        }

        /// <summary>
        /// 获取字典中键值对的数量
        /// </summary>
        /// <returns>包含数量的IntLangValue</returns>
        public IntLangValue Count()
        {
            return new IntLangValue(langValue.Value.Count);
        }

        /// <summary>
        /// 检查字典是否包含指定的键
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>包含检查结果的BoolLangValue</returns>
        public BoolLangValue ContainsKey(LangValueType key)
        {
            return new BoolLangValue(langValue.Value.Any(x => x.Key.Equal(key)));
        }

        /// <summary>
        /// 检查字典是否包含指定的值
        /// </summary>
        /// <param name="value">要检查的值</param>
        /// <returns>包含检查结果的BoolLangValue</returns>
        public BoolLangValue ContainsValue(LangValueType value)
        {
            return new BoolLangValue(langValue.Value.Any(x => x.Value.Equal(value)));
        }

        /// <summary>
        /// 获取字典的所有键
        /// </summary>
        /// <returns>包含所有键的列表</returns>
        public ListLangValue Keys()
        {
            var keys = new List<LangValueType>();
            foreach (var (key, _) in langValue.Value)
            {
                keys.Add(key);
            }
            return new ListLangValue(keys);
        }

        /// <summary>
        /// 获取字典的所有值
        /// </summary>
        /// <returns>包含所有值的列表</returns>
        public ListLangValue Values()
        {
            var values = new List<LangValueType>();
            foreach (var (_, value) in langValue.Value)
            {
                values.Add(value);
            }
            return new ListLangValue(values);
        }
    }
}