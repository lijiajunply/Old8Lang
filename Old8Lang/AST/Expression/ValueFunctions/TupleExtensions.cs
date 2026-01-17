using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// 编译模式下 ITuple 的扩展方法
/// </summary>
public static class TupleExtensions
{
    extension(ITuple tuple)
    {
        /// <summary>
        /// 切片操作
        /// </summary>
        public object Slice(int start, int end)
        {
            int length = tuple.Length;

            // 处理负索引
            if (start < 0) start += length;
            if (end < 0) end += length;

            // 边界检查
            start = Math.Max(0, Math.Min(start, length));
            end = Math.Max(0, Math.Min(end, length));

            // 确保 start <= end
            if (start >= end) return new ValueTuple(); // 空元组

            // 提取元素
            var items = new object[end - start];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = tuple[start + i]!;
            }

            // 动态创建新的 ValueTuple
            return TupleLangValue.CreateValueTupleStatic(items);
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public string ToStr()
        {
            var items = new List<string>();
            for (int i = 0; i < tuple.Length; i++)
            {
                items.Add(tuple[i]?.ToString() ?? "null");
            }

            return $"({string.Join(", ", items)})";
        }

        /// <summary>
        /// 连接为字符串
        /// </summary>
        public string Join(string separator)
        {
            var items = new List<string>();
            for (int i = 0; i < tuple.Length; i++)
            {
                items.Add(tuple[i]?.ToString() ?? "null");
            }

            return string.Join(separator, items);
        }

        /// <summary>
        /// 包含检查
        /// </summary>
        public bool Contains(object? item)
        {
            for (int i = 0; i < tuple.Length; i++)
            {
                if (Equals(tuple[i], item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 反转元组
        /// </summary>
        public object Reverse()
        {
            var items = new object[tuple.Length];
            for (int i = 0; i < tuple.Length; i++)
            {
                items[i] = tuple[tuple.Length - 1 - i]!;
            }

            return TupleLangValue.CreateValueTupleStatic(items);
        }

        /// <summary>
        /// 连接两个元组
        /// </summary>
        public object Concat(ITuple other)
        {
            var items = new object[tuple.Length + other.Length];
            for (int i = 0; i < tuple.Length; i++)
            {
                items[i] = tuple[i]!;
            }

            for (int i = 0; i < other.Length; i++)
            {
                items[tuple.Length + i] = other[i]!;
            }

            return TupleLangValue.CreateValueTupleStatic(items);
        }
    }
}