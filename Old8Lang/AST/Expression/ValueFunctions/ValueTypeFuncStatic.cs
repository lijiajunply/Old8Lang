using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// LangValueType类型的扩展方法类，提供类型转换和基本操作功能
/// </summary>
[Serializable]
public static class ValueTypeFuncStatic
{
    extension(LangValueType type)
    {
        /// <summary>
        /// 将值转换为整数类型
        /// </summary>
        /// <returns>转换后的整数类型值</returns>
        public IntLangValue ToInt()
        {
            if (type is IntLangValue intValue)
            {
                return intValue;
            }

            if (type is DoubleLangValue doubleValue)
            {
                return new IntLangValue(Convert.ToInt32(doubleValue.Value));
            }

            if (type is CharLangValue charValue)
            {
                return new IntLangValue(Convert.ToInt32(charValue.Value));
            }

            return new IntLangValue(int.Parse(type.ToString()));
        }

        /// <summary>
        /// 将值转换为类型对象
        /// </summary>
        /// <returns>表示当前值类型的TypeLangValue</returns>
        public TypeLangValue ToType()
        {
            return new TypeLangValue(type.TypeToString());
        }

        /// <summary>
        /// 将值转换为字符串表示
        /// </summary>
        /// <returns>值的字符串表示</returns>
        public StringLangValue ToStr()
        {
            return new StringLangValue(type.ToDisplayString());
        }

        /// <summary>
        /// 获取值的哈希码
        /// </summary>
        /// <returns>值的哈希码</returns>
        public IntLangValue ToHash()
        {
            return new IntLangValue(type.GetHashCode());
        }

        /// <summary>
        /// 比较当前值与另一个值是否相等
        /// </summary>
        /// <param name="otherValue">要比较的另一个值</param>
        /// <returns>比较结果，相等返回true，否则返回false</returns>
        public BoolLangValue Equal(LangValueType otherValue)
        {
            return new BoolLangValue(type.Equal(otherValue));
        }

        public BoolLangValue ToBool(LangValueType otherValue)
        {
            return new BoolLangValue(type.Equal(otherValue));
        }
    }
}