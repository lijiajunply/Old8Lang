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

            if (type is BoolLangValue boolValue)
            {
                return new IntLangValue(boolValue.Value ? 1 : 0);
            }

            if (type is StringLangValue stringValue)
            {
                var str = stringValue.Value;
                // Handle quoted strings
                if (str.StartsWith("\"") && str.EndsWith("\""))
                {
                    str = str.Substring(1, str.Length - 2);
                }

                // Handle common boolean strings
                if (str.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return new IntLangValue(1);
                }
                if (str.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return new IntLangValue();
                }

                // Try to parse as integer
                if (int.TryParse(str, out var intResult))
                {
                    return new IntLangValue(intResult);
                }

                // Try to parse as double and convert to int
                if (double.TryParse(str, out var doubleResult))
                {
                    return new IntLangValue(Convert.ToInt32(doubleResult));
                }

                throw new FormatException($"Cannot convert '{stringValue.Value}' to integer");
            }

            if (type is NullLangValue)
            {
                return new IntLangValue();
            }

            throw new FormatException($"Cannot convert {type.GetType().Name} to integer");
        }

        /// <summary>
        /// 将值转换为浮点数类型
        /// </summary>
        /// <returns>转换后的浮点数类型值</returns>
        public DoubleLangValue ToDouble()
        {
            if (type is DoubleLangValue doubleValue)
            {
                return doubleValue;
            }

            if (type is IntLangValue intValue)
            {
                return new DoubleLangValue(Convert.ToDouble(intValue.Value));
            }

            if (type is StringLangValue stringValue)
            {
                var str = stringValue.Value;
                // Handle quoted strings
                if (str.StartsWith("\"") && str.EndsWith("\""))
                {
                    str = str.Substring(1, str.Length - 2);
                }

                // Handle boolean strings
                if (str.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return new DoubleLangValue(1.0);
                }
                if (str.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return new DoubleLangValue();
                }

                // Try to parse as double
                if (double.TryParse(str, out var doubleResult))
                {
                    return new DoubleLangValue(doubleResult);
                }

                throw new FormatException($"Cannot convert '{stringValue.Value}' to double");
            }

            if (type is BoolLangValue boolValue)
            {
                return new DoubleLangValue(boolValue.Value ? 1.0 : 0.0);
            }

            if (type is NullLangValue)
            {
                return new DoubleLangValue();
            }

            throw new FormatException($"Cannot convert {type.GetType().Name} to double");
        }

        /// <summary>
        /// 将值转换为布尔类型
        /// </summary>
        /// <returns>转换后的布尔类型值</returns>
        public BoolLangValue ToBool()
        {
            if (type is BoolLangValue boolValue)
            {
                return boolValue;
            }

            if (type is IntLangValue intValue)
            {
                return new BoolLangValue(intValue.Value != 0);
            }

            if (type is DoubleLangValue doubleValue)
            {
                return new BoolLangValue(doubleValue.Value != 0.0);
            }

            if (type is StringLangValue stringValue)
            {
                var str = stringValue.Value;
                // Handle quoted strings
                if (str.StartsWith("\"") && str.EndsWith("\""))
                {
                    str = str.Substring(1, str.Length - 2);
                }

                // Handle boolean strings
                if (str.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return new BoolLangValue(true);
                }
                if (str.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return new BoolLangValue();
                }

                // Handle numeric strings
                if (int.TryParse(str, out var intResult))
                {
                    return new BoolLangValue(intResult != 0);
                }

                if (double.TryParse(str, out var doubleResult))
                {
                    return new BoolLangValue(doubleResult != 0.0);
                }

                // Non-empty string is true
                return new BoolLangValue(!string.IsNullOrEmpty(str));
            }

            if (type is NullLangValue)
            {
                return new BoolLangValue();
            }

            throw new FormatException($"Cannot convert {type.GetType().Name} to bool");
        }

        /// <summary>
        /// 将值转换为字符类型
        /// </summary>
        /// <returns>转换后的字符类型值</returns>
        public CharLangValue ToChar()
        {
            if (type is CharLangValue charValue)
            {
                return charValue;
            }

            if (type is IntLangValue intValue)
            {
                if (intValue.Value is >= 0 and <= 65535)
                {
                    return new CharLangValue(Convert.ToChar(intValue.Value));
                }
                throw new FormatException($"Integer value {intValue.Value} is out of valid character range");
            }

            if (type is StringLangValue stringValue)
            {
                var str = stringValue.Value;
                // Handle quoted strings
                if (str.StartsWith("\"") && str.EndsWith("\""))
                {
                    str = str.Substring(1, str.Length - 2);
                }

                if (str.Length == 1)
                {
                    return new CharLangValue(str[0]);
                }
                throw new FormatException($"String '{stringValue.Value}' is not a single character");
            }

            if (type is NullLangValue)
            {
                return new CharLangValue();
            }

            throw new FormatException($"Cannot convert {type.GetType().Name} to char");
        }

        /// <summary>
        /// 将值转换为列表类型
        /// </summary>
        /// <returns>转换后的列表类型值</returns>
        public ListLangValue ToList()
        {
            if (type is ListLangValue listValue)
            {
                return listValue;
            }

            if (type is NullLangValue)
            {
                return new ListLangValue(new List<LangExpression>());
            }

            throw new FormatException($"Cannot convert {type.GetType().Name} to list");
        }

        /// <summary>
        /// 将值转换为数组类型
        /// </summary>
        /// <returns>转换后的数组类型值</returns>
        public ArrayLangValue ToArray()
        {
            if (type is ArrayLangValue arrayValue)
            {
                return arrayValue;
            }

            if (type is NullLangValue)
            {
                return new ArrayLangValue(new List<LangValueType>());
            }

            if (type is ListLangValue listValue)
            {
                return new ArrayLangValue(listValue.GetItems());
            }

            throw new FormatException($"Cannot convert {type.GetType().Name} to array");
        }

        /// <summary>
        /// 将值转换为元组类型
        /// </summary>
        /// <returns>转换后的元组类型值</returns>
        public TupleLangValue ToTuple()
        {
            if (type is TupleLangValue tupleValue)
            {
                return tupleValue;
            }

            if (type is NullLangValue)
            {
                return new TupleLangValue(new LangId("null"), new LangId("null"));
            }

            throw new FormatException($"Cannot convert {type.GetType().Name} to tuple");
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

        public BoolLangValue EqualsTo(LangValueType otherValue)
        {
            return new BoolLangValue(type.Equal(otherValue));
        }
    }
}