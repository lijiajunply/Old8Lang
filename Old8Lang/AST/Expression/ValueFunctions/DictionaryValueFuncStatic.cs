using System.Linq;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

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

        /// <summary>
        /// 根据键获取字典中的值，如果键不存在则返回默认值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <param name="defaultValue">键不存在时的默认值</param>
        /// <returns>对应的值或默认值</returns>
        public LangValueType GetOrElse(LangValueType key, LangValueType defaultValue)
        {
            foreach (var (k, v) in langValue.Value)
            {
                if (k.Equal(key))
                {
                    return v;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 将另一个字典合并到当前字典中，如果有重复键，当前字典的值优先
        /// </summary>
        /// <param name="otherDictionary">要合并的另一个字典</param>
        /// <returns>合并后的新字典</returns>
        public DictionaryLangValue Merge(DictionaryLangValue otherDictionary)
        {
            // 创建一个新字典，使用默认构造函数
            var newDict = new DictionaryLangValue();

            // 复制当前字典的所有键值对
            foreach (var (key, value) in langValue.Value)
            {
                newDict.Value.Add((key, value));
            }

            // 添加另一个字典的键值对，跳过重复的键
            foreach (var (key, value) in otherDictionary.Value)
            {
                var keyExists = false;
                foreach (var (existingKey, _) in newDict.Value)
                {
                    if (existingKey.Equal(key))
                    {
                        keyExists = true;
                        break;
                    }
                }

                if (!keyExists)
                {
                    newDict.Value.Add((key, value));
                }
            }

            return newDict;
        }

        /// <summary>
        /// 更新字典中指定键的值，如果键不存在则添加
        /// </summary>
        /// <param name="key">要更新的键</param>
        /// <param name="newValue">新的值</param>
        /// <returns>更新后的字典</returns>
        public DictionaryLangValue Update(LangValueType key, LangValueType newValue)
        {
            // 创建一个新字典，使用默认构造函数
            var newDict = new DictionaryLangValue();

            // 复制当前字典的所有键值对
            var keyUpdated = false;
            foreach (var (existingKey, existingValue) in langValue.Value)
            {
                if (existingKey.Equal(key))
                {
                    newDict.Value.Add((key, newValue)); // 更新值
                    keyUpdated = true;
                }
                else
                {
                    newDict.Value.Add((existingKey, existingValue)); // 保持原值
                }
            }

            // 如果键不存在，添加新的键值对
            if (!keyUpdated)
            {
                newDict.Value.Add((key, newValue));
            }

            return newDict;
        }
    }
}