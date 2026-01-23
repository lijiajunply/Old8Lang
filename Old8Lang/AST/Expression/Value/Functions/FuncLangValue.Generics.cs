using System.Reflection;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

// ReSharper disable CheckNamespace
namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// FuncLangValue - 泛型支持
/// </summary>
public partial class FuncLangValue
{
    public FuncLangValue InstantiateGeneric(
        Dictionary<string, ITypeInfo> typeArguments,
        TypeAnnotationManager typeAnnotationManager,
        VariateManager? manager = null)
    {
        if (!IsGeneric)
        {
            throw new InvalidOperationException($"函数 {Id?.IdName} 不是泛型函数");
        }

        // 验证类型参数数量
        if (typeArguments.Count != GenericParameters!.Count)
        {
            throw new ArgumentException(
                $"类型参数数量不匹配：期望 {GenericParameters.Count} 个，实际 {typeArguments.Count} 个");
        }

        // 使用新的约束验证器验证约束
        foreach (var genericParam in GenericParameters)
        {
            if (typeArguments.TryGetValue(genericParam.Name, out var actualType))
            {
                var validationResult = GenericConstraintValidator.Validate(
                    genericParam,
                    actualType,
                    typeArguments,
                    manager);

                if (!validationResult.IsValid)
                {
                    throw new ArgumentException(validationResult.ErrorMessage);
                }
            }
        }

        // 创建替换后的参数列表
        var instantiatedIds = new List<LangId>();
        if (Ids != null)
        {
            foreach (var param in Ids)
            {
                var paramType = param.AssumptionType ?? "";
                // 替换泛型类型参数
                if (!string.IsNullOrEmpty(paramType))
                {
                    paramType = ReplaceGenericTypeParameters(paramType, typeArguments);
                }
                // 创建新的参数，使用替换后的类型
                var newParam = new LangId(param.IdName, paramType, param.DefaultValue, param.IsParams, param.Position);
                instantiatedIds.Add(newParam);
            }
        }

        // 替换返回类型
        var returnType = Id?.AssumptionType ?? "";
        if (!string.IsNullOrEmpty(returnType))
        {
            returnType = ReplaceGenericTypeParameters(returnType, typeArguments);
        }

        // 创建新的函数 ID，使用替换后的返回类型
        var newId = Id != null
            ? new LangId(Id.IdName, returnType, Id.DefaultValue, Id.IsParams, Id.Position)
            : null;

        // 创建实例化的FuncLangValue（复制所有字段）
        var instantiated = new FuncLangValue(
            id: newId,
            ids: instantiatedIds,
            blockStatement: BlockStatement,
            genericParameters: null, // 实例化后不再是泛型函数
            position: Position,
            isLambda: IsLambda
        )
        {
            // 设置类型参数映射
            TypeArgumentMapping = typeArguments
        };

        // 复制闭包环境（如果有）
        if (CapturedScope is not null)
        {
            typeof(FuncLangValue)
                .GetProperty("CapturedScope", BindingFlags.NonPublic | BindingFlags.Instance)?
                .SetValue(instantiated, CapturedScope);
        }

        return instantiated;
    }

    /// <summary>
    /// 替换类型字符串中的泛型类型参数
    /// 例如: "T?" -> "int?" (当 T 映射到 int 时)
    /// </summary>

    private static string ReplaceGenericTypeParameters(string typeString, Dictionary<string, ITypeInfo> typeArguments)
    {
        if (string.IsNullOrEmpty(typeString))
            return typeString;

        // 处理可空类型 (T?)
        if (typeString.EndsWith("?"))
        {
            var baseType = typeString.Substring(0, typeString.Length - 1);
            // 先尝试直接替换整个类型（包括 ?）
            if (typeArguments.TryGetValue(baseType, out var typeInfo))
            {
                // 如果类型参数本身已经是可空的（如 int?），直接返回
                if (typeInfo.Name.EndsWith("?"))
                {
                    return typeInfo.Name;
                }
                // 否则添加可空标记
                return typeInfo.Name + "?";
            }
            // 如果没有找到，递归处理基础类型
            var replacedBase = ReplaceGenericTypeParameters(baseType, typeArguments);
            // 如果替换后的类型已经是可空的，不要再添加 ?
            if (replacedBase.EndsWith("?"))
            {
                return replacedBase;
            }
            return replacedBase + "?";
        }

        // 处理泛型类型 (List<T>, Dict<K,V>)
        var genericStart = typeString.IndexOf('<');
        if (genericStart > 0)
        {
            var mainType = typeString[..genericStart];
            var genericEnd = FindMatchingBracket(typeString, genericStart);
            if (genericEnd > 0)
            {
                var paramsPart = typeString.Substring(genericStart + 1, genericEnd - genericStart - 1);
                var parameters = SplitGenericParameters(paramsPart);
                var replacedParams = parameters.Select(p => ReplaceGenericTypeParameters(p.Trim(), typeArguments));
                var result = $"{mainType}<{string.Join(", ", replacedParams)}>";

                // 处理泛型类型后面的可空标记
                if (genericEnd + 1 < typeString.Length && typeString[genericEnd + 1] == '?')
                {
                    result += "?";
                }
                return result;
            }
        }

        // 处理联合类型 (T | U)
        if (typeString.Contains('|'))
        {
            var types = SplitTopLevelSeparator(typeString, '|');
            var replacedTypes = types.Select(t => ReplaceGenericTypeParameters(t.Trim(), typeArguments));
            return string.Join(" | ", replacedTypes);
        }

        // 处理交叉类型 (T & U)
        if (typeString.Contains('&'))
        {
            var types = SplitTopLevelSeparator(typeString, '&');
            var replacedTypes = types.Select(t => ReplaceGenericTypeParameters(t.Trim(), typeArguments));
            return string.Join(" & ", replacedTypes);
        }

        // 简单类型参数替换
        if (typeArguments.TryGetValue(typeString, out var simpleTypeInfo))
        {
            return simpleTypeInfo.Name;
        }

        return typeString;
    }

    /// <summary>
    /// 查找匹配的右尖括号
    /// </summary>

    private static int FindMatchingBracket(string text, int startIndex)
    {
        int depth = 0;
        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '<') depth++;
            else if (text[i] == '>') depth--;
            if (depth == 0) return i;
        }
        return -1;
    }

    /// <summary>
    /// 分割泛型参数（考虑嵌套）
    /// </summary>

    private static List<string> SplitGenericParameters(string parameters)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        int depth = 0;

        foreach (var ch in parameters)
        {
            if (ch == '<') depth++;
            else if (ch == '>') depth--;
            else if (ch == ',' && depth == 0)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }
            current.Append(ch);
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    /// <summary>
    /// 按顶层分隔符分割（不分割泛型括号内的内容）
    /// </summary>

    private static List<string> SplitTopLevelSeparator(string text, char separator)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        int depth = 0;

        foreach (var ch in text)
        {
            if (ch == '<') depth++;
            else if (ch == '>') depth--;
            else if (ch == separator && depth == 0)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }
            current.Append(ch);
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }


}
