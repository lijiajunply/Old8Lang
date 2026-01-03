using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.ValueFunctions;

public static class CharValueFuncStatic
{
    extension(CharLangValue charValue)
    {
        /// <summary>
        /// 将字符转换为大写
        /// </summary>
        /// <returns>转换为大写后的字符</returns>
        public CharLangValue ToUpper()
        {
            return CharLangValue.Create(char.ToUpper(charValue.Value));
        }

        /// <summary>
        /// 将字符转换为小写
        /// </summary>
        /// <returns>转换为小写后的字符</returns>
        public CharLangValue ToLower()
        {
            return CharLangValue.Create(char.ToLower(charValue.Value));
        }

        /// <summary>
        /// 判断字符是否为数字
        /// </summary>
        /// <returns>如果是数字则返回true，否则返回false</returns>
        public BoolLangValue IsDigit()
        {
            return BoolLangValue.Create(char.IsDigit(charValue.Value));
        }

        /// <summary>
        /// 判断字符是否为字母
        /// </summary>
        /// <returns>如果是字母则返回true，否则返回false</returns>
        public BoolLangValue IsLetter()
        {
            return BoolLangValue.Create(char.IsLetter(charValue.Value));
        }

        /// <summary>
        /// 判断字符是否为空白字符（空格、制表符、换行符等）
        /// </summary>
        /// <returns>如果是空白字符则返回true，否则返回false</returns>
        public BoolLangValue IsWhiteSpace()
        {
            return BoolLangValue.Create(char.IsWhiteSpace(charValue.Value));
        }

        /// <summary>
        /// 判断字符是否为大写字母
        /// </summary>
        /// <returns>如果是大写字母则返回true，否则返回false</returns>
        public BoolLangValue IsUpper()
        {
            return BoolLangValue.Create(char.IsUpper(charValue.Value));
        }

        /// <summary>
        /// 判断字符是否为小写字母
        /// </summary>
        /// <returns>如果是小写字母则返回true，否则返回false</returns>
        public BoolLangValue IsLower()
        {
            return BoolLangValue.Create(char.IsLower(charValue.Value));
        }

        /// <summary>
        /// 判断字符是否为字母或数字
        /// </summary>
        /// <returns>如果是字母或数字则返回true，否则返回false</returns>
        public BoolLangValue IsLetterOrDigit()
        {
            return BoolLangValue.Create(char.IsLetterOrDigit(charValue.Value));
        }

        /// <summary>
        /// 判断字符是否为标点符号
        /// </summary>
        /// <returns>如果是标点符号则返回true，否则返回false</returns>
        public BoolLangValue IsPunctuation()
        {
            return BoolLangValue.Create(char.IsPunctuation(charValue.Value));
        }

        /// <summary>
        /// 判断字符是否为符号
        /// </summary>
        /// <returns>如果是符号则返回true，否则返回false</returns>
        public BoolLangValue IsSymbol()
        {
            return BoolLangValue.Create(char.IsSymbol(charValue.Value));
        }

        /// <summary>
        /// 判断字符是否为控制字符
        /// </summary>
        /// <returns>如果是控制字符则返回true，否则返回false</returns>
        public BoolLangValue IsControl()
        {
            return BoolLangValue.Create(char.IsControl(charValue.Value));
        }

        /// <summary>
        /// 将字符转换为整数（ASCII值）
        /// </summary>
        /// <returns>字符的整数值</returns>
        public IntLangValue ToInt()
        {
            return IntLangValue.Create(charValue.Value);
        }

        /// <summary>
        /// 获取字符的数字值（仅对数字字符有效）
        /// </summary>
        /// <returns>字符表示的数字值，如果不是数字字符则返回-1</returns>
        public DoubleLangValue GetNumericValue()
        {
            return DoubleLangValue.Create(char.GetNumericValue(charValue.Value));
        }

        /// <summary>
        /// 比较当前字符与另一个字符
        /// </summary>
        /// <param name="other">要比较的字符</param>
        /// <returns>如果当前字符小于other返回负数，等于返回0，大于返回正数</returns>
        public IntLangValue CompareTo(CharLangValue other)
        {
            return IntLangValue.Create(charValue.Value.CompareTo(other.Value));
        }
    }
}