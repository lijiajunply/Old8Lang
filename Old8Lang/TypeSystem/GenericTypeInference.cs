using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 泛型类型推断引擎
/// 从函数调用参数自动推断泛型类型参数
/// </summary>
public class GenericTypeInference(TypeAnnotationManager typeAnnotationManager)
{
    /// <summary>
    /// 从函数调用参数推断泛型类型
    /// </summary>
    /// <param name="funcValue">泛型函数</param>
    /// <param name="callArguments">调用参数</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="position">源代码位置</param>
    /// <returns>推断出的类型参数映射，如果失败则返回 null</returns>
    public Dictionary<string, ITypeInfo>? InferFunctionTypeArguments(
        FuncLangValue funcValue,
        List<LangExpression> callArguments,
        Interpreter.VariateManager manager,
        SourcePosition position)
    {
        if (!funcValue.IsGeneric)
        {
            return null;
        }

        // 检查参数数量是否匹配（考虑默认参数）
        var requiredParamCount = funcValue.Ids?.Count(id => id.DefaultValue == null) ?? 0;
        var totalParamCount = funcValue.Ids?.Count ?? 0;

        if (callArguments.Count < requiredParamCount || callArguments.Count > totalParamCount)
        {
            return null; // 参数数量不匹配，无法推断
        }

        // 创建类型参数映射
        var typeArgMapping = new Dictionary<string, ITypeInfo>();

        // 遍历函数参数，尝试从调用参数推断类型
        for (int i = 0; i < callArguments.Count && i < totalParamCount; i++)
        {
            var paramId = funcValue.Ids![i];
            var callArg = callArguments[i];

            // 获取参数的类型注解
            var paramTypeAnnotation = paramId.AssumptionType;
            if (string.IsNullOrEmpty(paramTypeAnnotation))
            {
                continue; // 没有类型注解，跳过
            }

            // 解析参数类型注解，提取泛型参数
            var genericParamsInType = ExtractGenericParametersFromType(paramTypeAnnotation);
            if (genericParamsInType.Count == 0)
            {
                continue; // 没有泛型参数，跳过
            }

            // 推断调用参数的类型
            var argType = InferArgumentType(callArg, manager);
            if (argType == null)
            {
                continue; // 无法推断参数类型，跳过
            }

            // 匹配泛型参数
            if (!MatchTypePattern(paramTypeAnnotation, argType, typeArgMapping))
            {
                return null; // 类型不匹配，推断失败
            }
        }

        // 检查是否所有泛型参数都已推断
        foreach (var genericParam in funcValue.GenericParameters!)
        {
            if (!typeArgMapping.ContainsKey(genericParam.Name))
            {
                // 无法推断所有类型参数
                return null;
            }
        }

        return typeArgMapping;
    }

    /// <summary>
    /// 从类型注解字符串中提取泛型参数名称
    /// 例如: "List&lt;T>" -> ["T"], "Dictionary&lt;K, V>" -> ["K", "V"]
    /// </summary>
    private HashSet<string> ExtractGenericParametersFromType(string typeAnnotation)
    {
        var result = new HashSet<string>();

        // 简单解析：查找 < 和 > 之间的内容
        var startIndex = typeAnnotation.IndexOf('<');
        var endIndex = typeAnnotation.LastIndexOf('>');

        if (startIndex == -1 || endIndex == -1 || startIndex >= endIndex)
        {
            // 检查是否是单个类型参数（如 "T"）
            if (char.IsUpper(typeAnnotation[0]) && typeAnnotation.Length <= 3)
            {
                result.Add(typeAnnotation);
            }

            return result;
        }

        // 提取 < > 之间的内容
        var genericPart = typeAnnotation.Substring(startIndex + 1, endIndex - startIndex - 1);

        // 按逗号分割（需要考虑嵌套泛型）
        var parts = SplitGenericArguments(genericPart);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            // 递归提取嵌套泛型参数
            var nested = ExtractGenericParametersFromType(trimmed);
            foreach (var n in nested)
            {
                result.Add(n);
            }
        }

        return result;
    }

    /// <summary>
    /// 分割泛型参数（考虑嵌套）
    /// 例如: "T, List&lt;U>" -> ["T", "List&lt;U>"]
    /// </summary>
    private List<string> SplitGenericArguments(string args)
    {
        var result = new List<string>();
        var current = "";
        var depth = 0;

        foreach (var ch in args)
        {
            if (ch == '<')
            {
                depth++;
                current += ch;
            }
            else if (ch == '>')
            {
                depth--;
                current += ch;
            }
            else if (ch == ',' && depth == 0)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += ch;
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.Trim());
        }

        return result;
    }

    /// <summary>
    /// 推断表达式的类型
    /// </summary>
    private ITypeInfo? InferArgumentType(LangExpression expr, Interpreter.VariateManager manager)
    {
        try
        {
            // 对于字面量，直接返回其类型
            switch (expr)
            {
                case IntLangValue:
                    return typeAnnotationManager.GetTypeFamily().GetType("int");

                case DoubleLangValue:
                    return typeAnnotationManager.GetTypeFamily().GetType("double");

                case StringLangValue:
                    return typeAnnotationManager.GetTypeFamily().GetType("string");

                case BoolLangValue:
                    return typeAnnotationManager.GetTypeFamily().GetType("bool");

                case CharLangValue:
                    return typeAnnotationManager.GetTypeFamily().GetType("char");

                case NullLangValue:
                    // null 可以匹配任何引用类型
                    return null;

                case LangId id:
                    // 查找变量类型
                    var value = manager.GetValue(id);
                    if (value != null)
                    {
                        return GetTypeInfoFromValue(value);
                    }

                    return null;

                case Instance instance:
                    // 对于函数调用，尝试执行并获取结果类型
                    // 注意：这可能有副作用，但对于推断泛型类型来说是必要的
                    try
                    {
                        var result = instance.Run(manager);
                        return GetTypeInfoFromValue(result);
                    }
                    catch
                    {
                        // 如果执行失败，尝试从函数签名推断
                        var funcValue = manager.GetValue(instance.Id);
                        if (funcValue is FuncLangValue func)
                        {
                            // 如果函数有返回类型注解，使用它
                            var returnTypeAnnotation = func.Id?.AssumptionType;
                            if (!string.IsNullOrEmpty(returnTypeAnnotation))
                            {
                                // 解析返回类型（可能包含 -> 符号）
                                var arrowIndex = returnTypeAnnotation.IndexOf("->", StringComparison.Ordinal);
                                if (arrowIndex != -1)
                                {
                                    var returnTypePart = returnTypeAnnotation.Substring(arrowIndex + 2).Trim();
                                    var returnType = typeAnnotationManager.GetTypeFamily().GetType(returnTypePart);
                                    if (returnType != null)
                                    {
                                        return returnType;
                                    }
                                }
                            }
                        }

                        return null;
                    }

                default:
                    // 对于复杂表达式，尝试执行并获取结果类型
                    // 注意：这可能有副作用，需要谨慎使用
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从运行时值获取类型信息
    /// </summary>
    private ITypeInfo? GetTypeInfoFromValue(LangValueType value)
    {
        return value switch
        {
            IntLangValue => typeAnnotationManager.GetTypeFamily().GetType("int"),
            DoubleLangValue => typeAnnotationManager.GetTypeFamily().GetType("double"),
            StringLangValue => typeAnnotationManager.GetTypeFamily().GetType("string"),
            BoolLangValue => typeAnnotationManager.GetTypeFamily().GetType("bool"),
            CharLangValue => typeAnnotationManager.GetTypeFamily().GetType("char"),
            _ => null
        };
    }

    /// <summary>
    /// 匹配类型模式
    /// 例如: 模式 "T" 匹配 "int" -> {T: int}
    ///       模式 "List&lt;T>" 匹配 "List&lt;int>" -> {T: int}
    /// </summary>
    private bool MatchTypePattern(
        string pattern,
        ITypeInfo actualType,
        Dictionary<string, ITypeInfo> typeArgMapping)
    {
        // 检查是否是单个类型参数
        if (pattern.Length <= 3 && char.IsUpper(pattern[0]) && !pattern.Contains('<'))
        {
            // 这是一个类型参数
            if (typeArgMapping.TryGetValue(pattern, out var existingType))
            {
                // 已经有映射，检查是否一致
                return existingType.Name == actualType.Name;
            }
            else
            {
                // 添加新的映射
                typeArgMapping[pattern] = actualType;
                return true;
            }
        }

        // 检查是否是泛型类型
        var patternGenericStart = pattern.IndexOf('<');
        if (patternGenericStart != -1)
        {
            // 提取基础类型名称
            var patternBaseName = pattern.Substring(0, patternGenericStart);

            // 检查实际类型是否也是同样的泛型类型
            if (!actualType.Name.StartsWith(patternBaseName))
            {
                return false;
            }

            // 提取泛型参数
            var patternGenericEnd = pattern.LastIndexOf('>');
            var patternGenericArgs = pattern.Substring(patternGenericStart + 1,
                patternGenericEnd - patternGenericStart - 1);

            var patternArgs = SplitGenericArguments(patternGenericArgs);

            // TODO: 从实际类型中提取泛型参数进行匹配
            // 这需要更复杂的类型系统支持

            return true; // 暂时返回 true，需要进一步实现
        }

        // 简单类型匹配
        return pattern == actualType.Name;
    }

    /// <summary>
    /// 从类实例化参数推断泛型类型
    /// </summary>
    public Dictionary<string, ITypeInfo>? InferClassTypeArguments(
        AST.Expression.AnyValues.TypeTemplate typeTemplate,
        List<LangExpression> constructorArguments,
        Interpreter.VariateManager manager,
        SourcePosition position)
    {
        if (!typeTemplate.IsGeneric)
        {
            return null;
        }

        // 类的类型推断通常需要从构造函数参数推断
        // 但 Old8Lang 的构造函数是用户定义的 init 方法
        // 因此这里的实现比较简单，主要留作未来扩展

        // 暂时不支持类的自动类型推断
        return null;
    }
}