using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.TypeSystem;

namespace Old8Lang.GlobalFunctions.Core;

/// <summary>
/// 全局函数重载解析辅助类
/// 使用 Old8Lang 类型系统进行统一的类型匹配
/// </summary>
public static class OverloadResolver
{
    /// <summary>
    /// 将 .NET 类型转换为 Old8Lang 类型名称
    /// </summary>
    public static string? ConvertDotNetTypeToOld8Type(Type? type)
    {
        if (type == null)
            return null;

        // 基本类型映射
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        if (type == typeof(char)) return "char";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(void)) return "void";
        if (type == typeof(object)) return "any";

        // 泛型类型
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            if (genericDef == typeof(List<>))
            {
                var elementType = ConvertDotNetTypeToOld8Type(genericArgs[0]);
                return elementType != null ? $"list<{elementType}>" : "list";
            }

            if (genericDef == typeof(Dictionary<,>))
            {
                var keyType = ConvertDotNetTypeToOld8Type(genericArgs[0]);
                var valueType = ConvertDotNetTypeToOld8Type(genericArgs[1]);
                return keyType != null && valueType != null ? $"dict<{keyType}, {valueType}>" : "dict";
            }
        }

        // 数组类型
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var old8ElementType = ConvertDotNetTypeToOld8Type(elementType);
            return old8ElementType != null ? $"array<{old8ElementType}>" : "array";
        }

        // 其他类型返回类型名
        return type.Name.ToLower();
    }

    /// <summary>
    /// 获取运行时值的 Old8Lang 类型名称
    /// </summary>
    public static string GetRuntimeValueType(object? value)
    {
        if (value == null)
            return "null";

        // 如果是 LangValueType，使用 TypeChecker
        if (value is LangValueType langValue)
        {
            return TypeChecker.GetLangValueType(langValue);
        }

        // 否则转换 .NET 类型
        return ConvertDotNetTypeToOld8Type(value.GetType()) ?? "object";
    }

    /// <summary>
    /// 计算类型匹配分数
    /// </summary>
    /// <param name="expectedType">期望的类型（.NET Type 或 null）</param>
    /// <param name="actualType">实际的 Old8Lang 类型名称</param>
    /// <returns>匹配分数，-1 表示不兼容</returns>
    public static int CalculateTypeMatchScore(Type? expectedType, string actualType)
    {
        // 如果期望类型为 null，表示接受任意类型
        if (expectedType == null)
            return 0;

        var expectedOld8Type = ConvertDotNetTypeToOld8Type(expectedType);
        if (expectedOld8Type == null)
            return 0;

        // 精确匹配
        if (expectedOld8Type == actualType)
            return 100;

        // 使用 TypeChecker 检查兼容性
        if (TypeChecker.IsTypeCompatible(expectedOld8Type, actualType))
        {
            // 兼容但不精确匹配，根据转换类型给分
            if (IsNumericConversion(expectedOld8Type, actualType))
                return 30; // 数值类型转换
            else
                return 50; // 其他隐式转换
        }

        // 不兼容
        return -1;
    }

    /// <summary>
    /// 检查是否是数值类型之间的转换
    /// </summary>
    private static bool IsNumericConversion(string expectedType, string actualType)
    {
        var numericTypes = new HashSet<string>
        {
            "byte", "int", "long", "float", "double", "decimal"
        };

        return numericTypes.Contains(expectedType) && numericTypes.Contains(actualType);
    }

    /// <summary>
    /// 检查函数是否可以接受给定的参数类型
    /// </summary>
    public static bool CanAcceptParameters(IGlobalFunction function, List<string> parameterTypes)
    {
        var count = parameterTypes.Count;

        // 检查参数数量
        if (count < function.MinParameterCount)
            return false;

        if (function.MaxParameterCount != -1 && count > function.MaxParameterCount)
            return false;

        // 如果没有指定参数类型，接受任意类型
        if (function.ParameterTypes == null)
            return true;

        // 检查每个参数的类型
        for (int i = 0; i < count && i < function.ParameterTypes.Length; i++)
        {
            var expectedType = function.ParameterTypes[i];
            var actualType = parameterTypes[i];

            var score = CalculateTypeMatchScore(expectedType, actualType);
            if (score < 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 计算函数与参数列表的总匹配分数
    /// </summary>
    public static int CalculateTotalMatchScore(IGlobalFunction function, List<string> parameterTypes)
    {
        if (!CanAcceptParameters(function, parameterTypes))
            return -1;

        var count = parameterTypes.Count;

        // 如果没有指定参数类型，返回基础分数
        if (function.ParameterTypes == null)
            return 0;

        int totalScore = 0;
        for (int i = 0; i < count && i < function.ParameterTypes.Length; i++)
        {
            var expectedType = function.ParameterTypes[i];
            var actualType = parameterTypes[i];

            var score = CalculateTypeMatchScore(expectedType, actualType);
            if (score < 0)
                return -1;

            totalScore += score;
        }

        return totalScore;
    }
}
