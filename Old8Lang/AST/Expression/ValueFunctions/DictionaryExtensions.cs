namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// Dictionary&lt;object, object?> 类型的扩展方法类
/// 为虚拟机模式中的原生 C# Dictionary 提供 Old8Lang 风格的方法
/// </summary>
public static class DictionaryExtensions
{
    extension(Dictionary<object, object?> dict)
    {
        /// <summary>
        /// 检查字典是否包含指定键
        /// </summary>
        public bool ContainsKey(object key)
        {
            return dict.ContainsKey(key);
        }

        /// <summary>
        /// 检查字典是否包含指定值
        /// </summary>
        public bool ContainsValue(object? value)
        {
            return dict.ContainsValue(value);
        }

        /// <summary>
        /// 获取字典中的值
        /// </summary>
        public object? Get(object key)
        {
            return dict.GetValueOrDefault(key);
        }

        /// <summary>
        /// 设置字典中的值
        /// </summary>
        public void Set(object key, object? value)
        {
            dict[key] = value;
        }

        /// <summary>
        /// 移除字典中的键值对
        /// </summary>
        public bool Remove(object key)
        {
            return dict.Remove(key);
        }

        /// <summary>
        /// 返回字典元素数量
        /// </summary>
        public int Count()
        {
            return dict.Count;
        }

        /// <summary>
        /// 清空字典
        /// </summary>
        public void Clear()
        {
            dict.Clear();
        }

        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        public string ToStr()
        {
            var items = dict.Select(kvp => $"{kvp.Key}: {kvp.Value?.ToString() ?? "null"}");
            return "{" + string.Join(", ", items) + "}";
        }
    }
}