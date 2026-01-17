namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// string 类型的扩展方法类
/// 为虚拟机模式和编译器模式中的原生 C# string 提供 Old8Lang 风格的方法
/// </summary>
public static class StringExtensions
{
    extension(string str)
    {
        /// <summary>
        /// 获取字符串长度
        /// </summary>
        public int Length()
        {
            return str.Length;
        }

        /// <summary>
        /// 分割字符串
        /// </summary>
        public List<object?> Split(string separator)
        {
            var parts = str.Split(separator);
            return parts.Cast<object?>().ToList();
        }

        /// <summary>
        /// 转换为字符串表示（返回自身）
        /// </summary>
        public string ToStr()
        {
            return str;
        }
    }
}
