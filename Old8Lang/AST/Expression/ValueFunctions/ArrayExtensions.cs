namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// object[] 类型的扩展方法类
/// 为虚拟机模式和编译器模式中的原生 C# 数组提供 Old8Lang 风格的方法
/// </summary>
public static class ArrayExtensions
{
    extension(object?[] array)
    {
        /// <summary>
        /// 获取数组长度
        /// </summary>
        public int Length()
        {
            return array.Length;
        }

        /// <summary>
        /// 检查数组是否包含指定元素
        /// </summary>
        public bool Contains(object? item)
        {
            return array.Contains(item);
        }

        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        public string ToStr()
        {
            var items = array.Select(item => item?.ToString() ?? "null");
            return "[" + string.Join(", ", items) + "]";
        }

        /// <summary>
        /// 转换为列表
        /// </summary>
        public List<object?> ToList()
        {
            return Enumerable.ToList(array);
        }
    }
}