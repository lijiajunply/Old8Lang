using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
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
        /// <returns>是否成功移除的BoolLangValue</returns>
        public BoolLangValue Remove(LangValueType key)
        {
            for (var i = 0; i < langValue.Value.Count; i++)
            {
                if (!langValue.Value[i].Key.Equal(key)) continue;
                langValue.Value.RemoveAt(i);
                return new BoolLangValue(true);
            }

            return new BoolLangValue(false);
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
        /// 使用另一个字典更新当前字典的键值对
        /// </summary>
        /// <param name="otherDictionary">包含更新内容的字典</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue Update(DictionaryLangValue otherDictionary)
        {
            foreach (var (key, value) in otherDictionary.Value)
            {
                // 查找并更新现有的键值对
                var keyFound = false;
                for (int i = 0; i < langValue.Value.Count; i++)
                {
                    var (existingKey, existingValue) = langValue.Value[i];
                    if (existingKey.Equal(key))
                    {
                        // 更新现有键的值
                        langValue.Value[i] = (existingKey, value);
                        keyFound = true;
                        break;
                    }
                }

                // 如果键不存在，添加新的键值对
                if (!keyFound)
                {
                    langValue.Value.Add((key, value));
                }
            }

            return new VoidLangValue();
        }

        /// <summary>
        /// 更新指定键的值
        /// </summary>
        /// <param name="key">要更新的键</param>
        /// <param name="value">新的值</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue Update(StringLangValue key, LangValueType value)
        {
            // 查找并更新现有的键值对
            var keyFound = false;
            for (int i = 0; i < langValue.Value.Count; i++)
            {
                var (existingKey, existingValue) = langValue.Value[i];
                if (existingKey.Equal(key))
                {
                    // 更新现有键的值
                    langValue.Value[i] = (existingKey, value);
                    keyFound = true;
                    break;
                }
            }

            // 如果键不存在，添加新的键值对
            if (!keyFound)
            {
                langValue.Value.Add((key, value));
            }

            return new VoidLangValue();
        }

        /// <summary>
        /// 清空字典中的所有键值对
        /// </summary>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue Clear()
        {
            langValue.Value.Clear();
            return new VoidLangValue();
        }

        /// <summary>
        /// 创建字典的独立副本
        /// </summary>
        /// <returns>新的字典副本</returns>
        public DictionaryLangValue Clone()
        {
            var newDict = new DictionaryLangValue();
            foreach (var (key, value) in langValue.Value)
            {
                newDict.Value.Add((key, value));
            }
            return newDict;
        }

        /// <summary>
        /// 根据键安全获取字典中的值，如果键不存在则返回默认值
        /// </summary>
        /// <param name="key">要查找的键</param>
        /// <param name="defaultValue">键不存在时的默认值</param>
        /// <returns>对应的值或默认值</returns>
        public LangValueType TryGet(LangValueType key, LangValueType defaultValue)
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
        /// 使用函数转换字典中的所有值
        /// </summary>
        /// <param name="func">转换函数，接收值作为参数</param>
        /// <returns>包含转换后值的新字典</returns>
        public DictionaryLangValue Map(FuncLangValue func)
        {
            var newDict = new DictionaryLangValue();

            // 尝试获取当前的 VariateManager，如果没有则创建新的
            var manager = ExecutionContext.GetCurrentManager();
            if (manager == null)
            {
                // 如果没有找到外部 manager，创建新的
                manager = new VariateManager();
            }

            foreach (var (key, value) in langValue.Value)
            {
                try
                {
                    var result = func.Run(manager, new List<LangExpression> { value });
                    newDict.Value.Add((key, result));
                }
                catch
                {
                    // 如果转换失败，保留原值
                    newDict.Value.Add((key, value));
                }
            }

            return newDict;
        }

        /// <summary>
        /// 使用条件过滤字典的键值对
        /// </summary>
        /// <param name="predicate">过滤函数，接收键值对作为参数，返回布尔值</param>
        /// <returns>包含满足条件键值对的新字典</returns>
        public DictionaryLangValue Filter(FuncLangValue predicate)
        {
            var newDict = new DictionaryLangValue();

            // 尝试获取当前的 VariateManager，如果没有则创建新的
            var manager = ExecutionContext.GetCurrentManager();
            if (manager == null)
            {
                // 如果没有找到外部 manager，创建新的
                manager = new VariateManager();
            }

            foreach (var (key, value) in langValue.Value)
            {
                try
                {
                    var result = predicate.Run(manager, new List<LangExpression> { key, value });
                    if (result is BoolLangValue boolResult && boolResult.Value)
                    {
                        newDict.Value.Add((key, value));
                    }
                }
                catch
                {
                    // 如果过滤函数失败，保留该项
                    newDict.Value.Add((key, value));
                }
            }

            return newDict;
        }

        /// <summary>
        /// 对字典中的每个键值对执行指定的操作
        /// </summary>
        /// <param name="action">要执行的操作函数，接收键值对作为参数</param>
        /// <returns>VoidLangValue，表示操作完成</returns>
        public VoidLangValue ForEach(FuncLangValue action)
        {
            // 获取当前的 VariateManager
            var manager = ExecutionContext.GetCurrentManager();
            if (manager == null)
            {
                // 如果没有找到外部 manager，创建新的
                manager = new VariateManager();
            }

            // 检查 action 是否是 lambda（lambda 通常 Id 为 null 且没有 Method）
            bool isLambda = action.Id == null && action.Method == null;

            if (isLambda)
            {
                // 对于 lambda，我们需要避免使用闭包机制（深拷贝）
                // 而是直接传递原始 manager 以支持外部变量访问
                foreach (var (key, value) in langValue.Value)
                {
                    try
                    {
                        // 直接执行 lambda 的主体，不创建闭包
                        // 保存当前作用域
                        var savedScopes = new List<Dictionary<string, LangValueType>>(manager.Scopes);

                        try
                        {
                            // 添加新的作用域层级
                            manager.AddChildren();
                            manager.IsFunc = true;

                            // 将参数添加到当前作用域
                            if (action.Ids?.Count >= 2)
                            {
                                // 两个参数：键和值
                                var keyId = action.Ids[0];
                                var valueId = action.Ids[1];
                                if (keyId != null && valueId != null)
                                {
                                    manager.Set(keyId, key);
                                    manager.Set(valueId, value);
                                }
                            }
                            else if (action.Ids?.Count == 1)
                            {
                                // 一个参数：只传递值
                                var valueId = action.Ids[0];
                                if (valueId != null)
                                {
                                    manager.Set(valueId, value);
                                }
                            }

                            // 执行 lambda 主体
                            action.BlockStatement?.Run(manager);
                        }
                        finally
                        {
                            // 恢复作用域
                            manager.Scopes.Clear();
                            manager.Scopes.AddRange(savedScopes);
                            manager.IsFunc = false;
                            manager.IsReturn = false;
                            manager.Result = new VoidLangValue();
                        }
                    }
                    catch
                    {
                        // 忽略执行错误，继续处理下一项
                    }
                }
            }
            else
            {
                // 对于非 lambda（原生方法），使用正常的 Run 调用
                foreach (var (key, value) in langValue.Value)
                {
                    try
                    {
                        action.Run(manager, new List<LangExpression> { key, value });
                    }
                    catch
                    {
                        // 忽略执行错误，继续处理下一项
                    }
                }
            }

            return new VoidLangValue();
        }
    }
}