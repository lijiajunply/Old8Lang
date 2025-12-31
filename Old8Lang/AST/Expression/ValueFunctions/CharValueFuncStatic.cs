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
    }
}