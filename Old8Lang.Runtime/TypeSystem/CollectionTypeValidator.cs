using Old8Lang.AST;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using CompilerException = Old8Lang.Error.CompilerException;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 泛型集合类型验证器,用于在编译时验证集合类型注解的正确性
/// </summary>
public static class CollectionTypeValidator
{
    /// <summary>
    /// 验证集合类型注解与实际值是否匹配
    /// </summary>
    /// <param name="typeAnnotation">类型注解字符串(如 "list&lt;int>")</param>
    /// <param name="value">实际值</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="variableName">变量名(用于错误消息)</param>
    /// <param name="position">源代码位置信息</param>
    /// <exception cref="TypeError">当类型不匹配时抛出</exception>
    public static void ValidateCollectionTypeAnnotation(
        string typeAnnotation,
        LangExpression value,
        LocalManager local,
        string variableName,
        SourcePosition position)
    {
        // 解析类型注解
        if (local.Interpreter?.Manager is null) return;
        var typeAnnotationManager = new TypeAnnotationManager(local.Interpreter.Manager);
        var parsedAnnotation = typeAnnotationManager.ParseTypeAnnotation(typeAnnotation);

        // 检查是否为泛型集合类型
        if (!parsedAnnotation.IsGeneric)
        {
            return; // 非泛型类型,不需要验证
        }

        var baseType = parsedAnnotation.BaseType.ToLower();
        if (baseType != "list" && baseType != "array" && baseType != "dict")
        {
            return; // 非集合类型,不需要验证
        }

        // 验证集合元素
        ValidateCollectionElements(parsedAnnotation, value, variableName, position, typeAnnotationManager);
    }

    /// <summary>
    /// 验证集合元素的类型
    /// </summary>
    private static void ValidateCollectionElements(
        ParsedTypeAnnotation parsedAnnotation,
        LangExpression value,
        string variableName,
        SourcePosition position,
        TypeAnnotationManager typeAnnotationManager)
    {
        var baseType = parsedAnnotation.BaseType.ToLower();

        if (baseType == "list")
        {
            ValidateListElements(parsedAnnotation, value, variableName, position, typeAnnotationManager);
        }
        else if (baseType == "array")
        {
            ValidateArrayElements(parsedAnnotation, value, variableName, position, typeAnnotationManager);
        }
        else if (baseType == "dict")
        {
            ValidateDictElements(parsedAnnotation, value, variableName, position, typeAnnotationManager);
        }
    }

    /// <summary>
    /// 验证列表元素类型
    /// </summary>
    private static void ValidateListElements(
        ParsedTypeAnnotation parsedAnnotation,
        LangExpression value,
        string variableName,
        SourcePosition position,
        TypeAnnotationManager typeAnnotationManager)
    {
        if (value is not ListLangValue listValue)
        {
            throw new CompilerException(
                $"变量 '{variableName}' 类型注解为 {parsedAnnotation.GetFullName()},但实际值不是列表。期望类型: {parsedAnnotation.GetFullName()}, 实际类型: {GetExpressionTypeName(value)}",
                position
            );
        }

        // 空列表跳过元素验证
        if (listValue.Value.Count == 0)
        {
            return;
        }

        // 获取期望的元素类型
        var expectedElementType = parsedAnnotation.GenericArguments![0];

        // 验证每个元素
        for (int i = 0; i < listValue.Value.Count; i++)
        {
            var element = listValue.Value[i];
            var actualElementTypeName = GetExpressionTypeName(element);
            var actualElementType = typeAnnotationManager.ParseTypeAnnotation(actualElementTypeName);

            if (!typeAnnotationManager.ValidateTypeCompatibility(
                    expectedElementType.GetFullName(),
                    actualElementType.GetFullName()))
            {
                throw new CompilerException(
                    $"变量 '{variableName}' 列表元素类型不匹配: 第 {i} 个元素期望类型 {expectedElementType.GetFullName()},实际类型 {actualElementTypeName}",
                    position
                );
            }
        }

        // 应用类型信息到 AST 节点
        listValue.ElementType = expectedElementType.GetFullName();
    }

    /// <summary>
    /// 验证数组元素类型
    /// </summary>
    private static void ValidateArrayElements(
        ParsedTypeAnnotation parsedAnnotation,
        LangExpression value,
        string variableName,
        SourcePosition position,
        TypeAnnotationManager typeAnnotationManager)
    {
        if (value is not ArrayLangValue arrayValue)
        {
            throw new CompilerException(
                $"变量 '{variableName}' 类型注解为 {parsedAnnotation.GetFullName()},但实际值不是数组。期望类型: {parsedAnnotation.GetFullName()}, 实际类型: {GetExpressionTypeName(value)}",
                position
            );
        }

        // 空数组跳过元素验证
        if (arrayValue.Values.Count == 0)
        {
            return;
        }

        // 获取期望的元素类型
        var expectedElementType = parsedAnnotation.GenericArguments![0];

        // 验证每个元素
        for (int i = 0; i < arrayValue.Values.Count; i++)
        {
            var element = arrayValue.Values[i];
            var actualElementTypeName = GetExpressionTypeName(element);
            var actualElementType = typeAnnotationManager.ParseTypeAnnotation(actualElementTypeName);

            if (!typeAnnotationManager.ValidateTypeCompatibility(
                    expectedElementType.GetFullName(),
                    actualElementType.GetFullName()))
            {
                throw new CompilerException(
                    $"变量 '{variableName}' 数组元素类型不匹配: 第 {i} 个元素期望类型 {expectedElementType.GetFullName()},实际类型 {actualElementTypeName}",
                    position
                );
            }
        }

        // 应用类型信息到 AST 节点
        arrayValue.ElementType = expectedElementType.GetFullName();
    }

    /// <summary>
    /// 验证字典键值类型
    /// </summary>
    private static void ValidateDictElements(
        ParsedTypeAnnotation parsedAnnotation,
        LangExpression value,
        string variableName,
        SourcePosition position,
        TypeAnnotationManager typeAnnotationManager)
    {
        if (value is not DictionaryLangValue dictValue)
        {
            throw new CompilerException(
                $"变量 '{variableName}' 类型注解为 {parsedAnnotation.GetFullName()},但实际值不是字典。期望类型: {parsedAnnotation.GetFullName()}, 实际类型: {GetExpressionTypeName(value)}",
                position
            );
        }

        // 空字典跳过元素验证
        if (dictValue.Tuples.Count == 0)
        {
            return;
        }

        // 获取期望的键值类型
        var expectedKeyType = parsedAnnotation.GenericArguments![0];
        var expectedValueType = parsedAnnotation.GenericArguments[1];

        // 验证每个键值对
        for (int i = 0; i < dictValue.Tuples.Count; i++)
        {
            var tuple = dictValue.Tuples[i];

            // 验证键类型
            var actualKeyTypeName = GetExpressionTypeName(tuple.Elements[0]);
            var actualKeyType = typeAnnotationManager.ParseTypeAnnotation(actualKeyTypeName);

            if (!typeAnnotationManager.ValidateTypeCompatibility(
                    expectedKeyType.GetFullName(),
                    actualKeyType.GetFullName()))
            {
                throw new CompilerException(
                    $"变量 '{variableName}' 字典键类型不匹配: 第 {i} 个键期望类型 {expectedKeyType.GetFullName()},实际类型 {actualKeyTypeName}",
                    position
                );
            }

            // 验证值类型
            var actualValueTypeName = GetExpressionTypeName(tuple.Elements[1]);
            var actualValueType = typeAnnotationManager.ParseTypeAnnotation(actualValueTypeName);

            if (!typeAnnotationManager.ValidateTypeCompatibility(
                    expectedValueType.GetFullName(),
                    actualValueType.GetFullName()))
            {
                throw new CompilerException(
                    $"变量 '{variableName}' 字典值类型不匹配: 第 {i} 个值期望类型 {expectedValueType.GetFullName()},实际类型 {actualValueTypeName}",
                    position
                );
            }
        }

        // 应用类型信息到 AST 节点
        dictValue.KeyType = expectedKeyType.GetFullName();
        dictValue.ValueType = expectedValueType.GetFullName();
    }

    /// <summary>
    /// 获取表达式的类型名称
    /// </summary>
    private static string GetExpressionTypeName(LangExpression expr)
    {
        return expr switch
        {
            IntLangValue => "int",
            DoubleLangValue => "double",
            StringLangValue => "string",
            BoolLangValue => "bool",
            CharLangValue => "char",
            ListLangValue list => list.ElementType is not null ? $"list<{list.ElementType}>" : "list",
            ArrayLangValue array => array.ElementType is not null ? $"array<{array.ElementType}>" : "array",
            DictionaryLangValue dict => dict.KeyType is not null && dict.ValueType is not null
                ? $"dict<{dict.KeyType},{dict.ValueType}>"
                : "dict",
            NullLangValue => "null",
            _ => "any"
        };
    }
}