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
    }
}