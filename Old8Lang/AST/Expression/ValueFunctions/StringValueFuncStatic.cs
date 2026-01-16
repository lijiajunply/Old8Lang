using System.Text;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// StringLangValue类型的扩展方法类，提供字符串操作功能
/// </summary>
[Serializable]
public static class StringValueFuncStatic
{
    /// <summary>
    /// 验证字符串不为空，如果为空则抛出异常
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <param name="paramName">参数名称</param>
    /// <param name="displayName">显示名称（用于错误消息）</param>
    /// <exception cref="ArgumentNullException">当字符串为空时抛出</exception>
    private static void ThrowIfNullOrEmpty(string value, string paramName, string displayName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException(paramName, $"{displayName}不能为空");
        }
    }

    extension(StringLangValue str)
    {
        /// <summary>
        /// 获取字符串的长度
        /// </summary>
        /// <returns>字符串长度</returns>
        public IntLangValue Length()
        {
            return new IntLangValue(str.Value.Length);
        }

        /// <summary>
        /// 获取字符串的子串
        /// </summary>
        /// <param name="start">子串的起始位置</param>
        /// <param name="length">子串的长度</param>
        /// <returns>包含子串的StringLangValue</returns>
        public StringLangValue Substring(IntLangValue start, IntLangValue length)
        {
            return new StringLangValue(str.Value.Substring(start.Value, length.Value));
        }

        /// <summary>
        /// 替换字符串中的指定子串
        /// </summary>
        /// <param name="oldValue">要替换的旧子串</param>
        /// <param name="newValue">替换后的新子串</param>
        /// <returns>替换后的StringLangValue</returns>
        public StringLangValue Replace(StringLangValue oldValue, StringLangValue newValue)
        {
            return new StringLangValue(str.Value.Replace(oldValue.Value, newValue.Value));
        }

        /// <summary>
        /// 使用指定分隔符分割字符串
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <returns>包含分割结果的ListLangValue</returns>
        public ListLangValue Split(StringLangValue separator)
        {
            var parts = str.Value.Split(separator.Value)
                .Select(s => new StringLangValue(s) as LangValueType)
                .ToList();
            return new ListLangValue(parts);
        }

        /// <summary>
        /// 将字符串转换为大写
        /// </summary>
        /// <returns>转换为大写后的StringLangValue</returns>
        public StringLangValue ToUpper()
        {
            return new StringLangValue(str.Value.ToUpper());
        }

        /// <summary>
        /// 将字符串转换为小写
        /// </summary>
        /// <returns>转换为小写后的StringLangValue</returns>
        public StringLangValue ToLower()
        {
            return new StringLangValue(str.Value.ToLower());
        }

        /// <summary>
        /// 检查字符串是否包含指定子串
        /// </summary>
        /// <param name="value">要检查的子串</param>
        /// <returns>包含检查结果的BoolLangValue</returns>
        public BoolLangValue Contains(StringLangValue value)
        {
            return new BoolLangValue(str.Value.Contains(value.Value));
        }

        /// <summary>
        /// 去除字符串首尾的空白字符
        /// </summary>
        /// <returns>去除空白字符后的StringLangValue</returns>
        public StringLangValue Trim()
        {
            return new StringLangValue(str.Value.Trim());
        }


        /// <summary>
        /// 检查字符串是否以指定的前缀开头
        /// </summary>
        /// <param name="prefixLangValue">前缀</param>
        /// <returns>如果以指定前缀开头则返回true，否则返回false</returns>
        public BoolLangValue StartsWith(StringLangValue prefixLangValue)
        {
            var input = str.Value;
            var prefix = prefixLangValue.Value;

            ThrowIfNullOrEmpty(input, nameof(input), "输入字符串");
            ThrowIfNullOrEmpty(prefix, nameof(prefix), "前缀");

            return new BoolLangValue(input.StartsWith(prefix));
        }

        /// <summary>
        /// 检查字符串是否以指定的后缀结尾
        /// </summary>
        /// <param name="suffixLangValue">后缀</param>
        /// <returns>如果以指定后缀结尾则返回true，否则返回false</returns>
        public BoolLangValue EndsWith(StringLangValue suffixLangValue)
        {
            var input = str.Value;
            var suffix = suffixLangValue.Value;

            ThrowIfNullOrEmpty(input, nameof(input), "输入字符串");
            ThrowIfNullOrEmpty(suffix, nameof(suffix), "后缀");

            return new BoolLangValue(input.EndsWith(suffix));
        }

        /// <summary>
        /// 在字符串中查找子字符串的位置
        /// </summary>
        /// <param name="substring">子字符串</param>
        /// <returns>子字符串的位置，如果没有找到则返回-1</returns>
        public IntLangValue IndexOf(StringLangValue substring)
        {
            ThrowIfNullOrEmpty(str.Value, nameof(str), "输入字符串");
            ThrowIfNullOrEmpty(substring.Value, nameof(substring), "子字符串");

            return new IntLangValue(str.Value.IndexOf(substring.Value, StringComparison.Ordinal));
        }
    }

    /// <param name="input">输入字符串</param>
    extension(StringLangValue input)
    {
        /// <summary>
        /// 重复字符串指定次数
        /// </summary>
        /// <param name="countValue">重复次数</param>
        /// <exception cref="ArgumentOutOfRangeException">重复次数不能为负数</exception>
        /// <returns>重复后的字符串</returns>
        public string Repeat(IntLangValue countValue)
        {
            ThrowIfNullOrEmpty(input.Value, nameof(input), "输入字符串");

            var count = countValue.Value;

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "重复次数不能为负数");
            }

            if (count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(input.Value.Length * count);
            for (int i = 0; i < count; i++)
            {
                builder.Append(input.Value);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 去除字符串左侧的空白字符
        /// </summary>
        /// <returns>去除左侧空白字符后的字符串</returns>
        public string TrimStart()
        {
            var input1 = input.Value;
            ThrowIfNullOrEmpty(input1, nameof(input1), "输入字符串");

            return input1.TrimStart();
        }

        /// <summary>
        /// 去除字符串右侧的空白字符
        /// </summary>
        /// <returns>去除右侧空白字符后的字符串</returns>
        public string TrimEnd()
        {
            var input1 = input.Value;
            ThrowIfNullOrEmpty(input1, nameof(input1), "输入字符串");

            return input1.TrimEnd();
        }
    }

    /// <param name="inputValue">输入字符串</param>
    extension(StringLangValue inputValue)
    {
        /// <summary>
        /// 将字符串转换为Base64编码
        /// </summary>
        /// <returns>Base64编码的字符串</returns>
        public string ToBase64()
        {
            var input = inputValue.Value;
            ThrowIfNullOrEmpty(input, nameof(input), "输入字符串");

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                throw new EncodingException($"Base64编码失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 将Base64编码的字符串转换为原始字符串
        /// </summary>
        /// <returns>原始字符串</returns>
        public string FromBase64()
        {
            var input = inputValue.Value;
            ThrowIfNullOrEmpty(input, nameof(input), "输入字符串");

            try
            {
                byte[] bytes = Convert.FromBase64String(input);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                throw new EncodingException($"Base64解码失败: {ex.Message}", ex);
            }
        }
    }
}

/// <summary>
/// 编码异常类
/// </summary>
public class EncodingException : Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常信息</param>
    /// <param name="innerException">内部异常</param>
    public EncodingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常信息</param>
    public EncodingException(string message) : base(message)
    {
    }
}