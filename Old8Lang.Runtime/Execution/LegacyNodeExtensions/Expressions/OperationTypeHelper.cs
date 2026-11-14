namespace Old8Lang.AST.Expression;

/// <summary>编译期运算类型辅助方法。</summary>
internal static class OperationTypeHelper
{
    internal static bool IsNumericType(Type type) => type == typeof(int) || type == typeof(double) ||
                                                     type == typeof(float) || type == typeof(long) ||
                                                     type == typeof(short) || type == typeof(byte);



    /// <summary>
    /// 获取内置类型方法的返回类型
    /// </summary>
    /// <param name="leftType">左操作数类型（调用方法的对象类型）</param>
    /// <param name="methodName">方法名</param>
    /// <param name="paramCount">参数数量</param>
    /// <returns>方法的返回类型，如果不是内置方法则返回null</returns>
    internal static Type? GetBuiltInMethodReturnType(Type? leftType, string methodName, int paramCount)
    {
        if (leftType is null) return null;

        // 处理无参数的内置方法
        if (paramCount == 0)
        {
            // 通用类型转换方法
            switch (methodName)
            {
                case "ToInt":
                    // int, double, bool, char 都有 ToInt() 方法
                    if (leftType == typeof(int) || leftType == typeof(double) ||
                        leftType == typeof(bool) || leftType == typeof(char))
                        return typeof(int);
                    break;
                case "ToDouble":
                    // int, double, bool 都有 ToDouble() 方法
                    if (leftType == typeof(int) || leftType == typeof(double) || leftType == typeof(bool))
                        return typeof(double);
                    break;
                case "ToBool":
                    // int, double, bool 都有 ToBool() 方法
                    if (leftType == typeof(int) || leftType == typeof(double) || leftType == typeof(bool))
                        return typeof(bool);
                    break;
                case "ToChar":
                    // int, char 都有 ToChar() 方法
                    if (leftType == typeof(int) || leftType == typeof(char))
                        return typeof(char);
                    break;
            }

            // char 类型特有的方法
            if (leftType == typeof(char))
            {
                switch (methodName)
                {
                    case "ToUpper":
                    case "ToLower":
                        return typeof(char);
                    case "IsDigit":
                    case "IsLetter":
                    case "IsWhiteSpace":
                    case "IsUpper":
                    case "IsLower":
                    case "IsLetterOrDigit":
                    case "IsPunctuation":
                    case "IsSymbol":
                    case "IsControl":
                        return typeof(bool);
                    case "GetNumericValue":
                        return typeof(double);
                }
            }

            // string 类型的方法
            if (leftType == typeof(string))
            {
                switch (methodName)
                {
                    case "ToUpper":
                    case "ToLower":
                    case "Trim":
                    case "TrimStart":
                    case "TrimEnd":
                        return typeof(string);
                    case "ToInt":
                        return typeof(int);
                    case "ToDouble":
                        return typeof(double);
                    case "ToBool":
                        return typeof(bool);
                }
            }

            // List 类型的方法
            if (leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(List<>))
            {
                switch (methodName)
                {
                    case "Count":
                        return typeof(int);
                    case "ToArray":
                        return leftType.GetGenericArguments()[0].MakeArrayType();
                }
            }

            // Array 类型的方法
            if (leftType.IsArray)
            {
                switch (methodName)
                {
                    case "Length":
                        return typeof(int);
                    case "ToList":
                        return typeof(List<>).MakeGenericType(leftType.GetElementType()!);
                }
            }
        }

        // 处理带参数的内置方法
        if (paramCount == 1)
        {
            // char 类型的 CompareTo 方法
            if (leftType == typeof(char) && methodName == "CompareTo")
            {
                return typeof(int);
            }

            // string 类型的方法
            if (leftType == typeof(string))
            {
                switch (methodName)
                {
                    case "Contains":
                    case "StartsWith":
                    case "EndsWith":
                        return typeof(bool);
                    case "IndexOf":
                    case "LastIndexOf":
                        return typeof(int);
                    case "Split":
                        return typeof(string[]);
                    case "Replace":
                        return typeof(string);
                    case "Substring":
                        return typeof(string);
                }
            }
        }

        if (paramCount == 2)
        {
            if (leftType == typeof(string))
            {
                switch (methodName)
                {
                    case "Replace":
                    case "Substring":
                        return typeof(string);
                }
            }
        }

        return null;
    }
}
