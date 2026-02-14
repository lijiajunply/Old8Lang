using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VM 模式下的类型映射工具类
/// 负责管理 C# 原生类型与 Old8Lang 类型之间的等价关系
/// </summary>
public static class VMTypeMapper
{
    /// <summary>
    /// 获取 C# 原生类型对应的等价 Old8Lang 类型
    /// </summary>
    /// <param name="nativeType">C# 原生类型</param>
    /// <returns>对应的 Old8Lang 类型，如果没有特定映射则返回 LangValueType</returns>
    public static Type GetEquivalentLangType(Type nativeType)
    {
        // object[] 等价于 ArrayLangValue (VM 模式下的数组表示)
        if (nativeType == typeof(object[]))
        {
            return typeof(ArrayLangValue);
        }

        // List<object?> 等价于 ListLangValue
        if (nativeType == typeof(List<object?>))
        {
            return typeof(ListLangValue);
        }

        // Dictionary<object, object?> 等价于 DictionaryLangValue
        if (nativeType == typeof(Dictionary<object, object?>))
        {
            return typeof(DictionaryLangValue);
        }

        // string 等价于 StringLangValue
        if (nativeType == typeof(string))
        {
            return typeof(StringLangValue);
        }

        // int 等价于 IntLangValue
        if (nativeType == typeof(int))
        {
            return typeof(IntLangValue);
        }

        // double 等价于 DoubleLangValue
        if (nativeType == typeof(double))
        {
            return typeof(DoubleLangValue);
        }

        // bool 等价于 BoolLangValue
        if (nativeType == typeof(bool))
        {
            return typeof(BoolLangValue);
        }

        // char 等价于 CharLangValue
        if (nativeType == typeof(char))
        {
            return typeof(CharLangValue);
        }

        // Tuple<object?, object?> 等价于 TupleLangValue (VM 模式下的元组表示)
        if (nativeType.IsGenericType && nativeType.GetGenericTypeDefinition() == typeof(Tuple<,>))
        {
            return typeof(TupleLangValue);
        }

        // 默认返回基类型
        return typeof(LangValueType);
    }

    /// <summary>
    /// 检查两个类型是否等价（用于 VM 模式）
    /// </summary>
    /// <param name="actualType">实际类型（通常是 C# 原生类型）</param>
    /// <param name="targetType">目标类型（通常是 Old8Lang 类型）</param>
    /// <returns>如果两个类型等价则返回 true</returns>
    public static bool IsEquivalentType(Type actualType, Type targetType)
    {
        // 如果目标类型是 LangValueType 基类，检查实际类型是否可以映射到 LangValueType
        if (targetType == typeof(LangValueType))
        {
            // C# 原生类型可以映射到 LangValueType
            if (actualType == typeof(int) || actualType == typeof(long) ||
                actualType == typeof(double) || actualType == typeof(bool) ||
                actualType == typeof(char) || actualType == typeof(string))
            {
                return true;
            }

            // 如果实际类型已经是 LangValueType 的子类，也认为等价
            if (typeof(LangValueType).IsAssignableFrom(actualType))
            {
                return true;
            }
        }

        // object[] 等价于 ArrayLangValue
        if (actualType == typeof(object[]) && targetType == typeof(ArrayLangValue))
        {
            return true;
        }

        // List<object?> 等价于 ListLangValue
        if (actualType == typeof(List<object?>) && targetType == typeof(ListLangValue))
        {
            return true;
        }

        // object[] 和 List<object?> 等价于 ILangList 接口
        if (targetType == typeof(ILangList))
        {
            if (actualType == typeof(object[]) || actualType == typeof(List<object?>))
            {
                return true;
            }
        }

        // Dictionary<object, object?> 等价于 DictionaryLangValue
        if (actualType == typeof(Dictionary<object, object?>) && targetType == typeof(DictionaryLangValue))
        {
            return true;
        }

        // string 等价于 StringLangValue
        if (actualType == typeof(string) && targetType == typeof(StringLangValue))
        {
            return true;
        }

        // int 等价于 IntLangValue
        if (actualType == typeof(int) && targetType == typeof(IntLangValue))
        {
            return true;
        }

        // double 等价于 DoubleLangValue
        if (actualType == typeof(double) && targetType == typeof(DoubleLangValue))
        {
            return true;
        }

        // bool 等价于 BoolLangValue
        if (actualType == typeof(bool) && targetType == typeof(BoolLangValue))
        {
            return true;
        }

        // char 等价于 CharLangValue
        if (actualType == typeof(char) && targetType == typeof(CharLangValue))
        {
            return true;
        }

        // Tuple<object?, object?> 等价于 TupleLangValue (VM 模式下的元组表示)
        if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(Tuple<,>) &&
            targetType == typeof(TupleLangValue))
        {
            return true;
        }

        return false;
    }
}
