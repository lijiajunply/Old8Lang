using System.Collections.Concurrent;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 类型假注管理器，提供类型假注的解析、验证和管理功能
/// </summary>
public class TypeAnnotationManager
{
    private readonly TypeFamily _typeFamily = new();
    private readonly VariateManager _globalManager;

    public TypeAnnotationManager(VariateManager globalManager)
    {
        _globalManager = globalManager;
        InitializeBasicTypes();
    }

    /// <summary>
    /// 获取全局变量管理器
    /// </summary>
    public VariateManager GetGlobalManager() => _globalManager;

    /// <summary>
    /// 初始化基本类型
    /// </summary>
    private void InitializeBasicTypes()
    {
        // 注册基本类型
        _typeFamily.RegisterType(new PrimitiveTypeInfo("int"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("double"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("string"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("bool"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("char"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("void"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("any"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("null"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("function"));

        // 注册泛型集合类型
        RegisterGenericCollectionTypes();
    }

    /// <summary>
    /// 注册泛型集合类型（list&lt;T>, array&lt;T>, dict&lt;K,V>）
    /// </summary>
    private void RegisterGenericCollectionTypes()
    {
        // list<T>: 单类型参数
        var listType = new GenericTypeInfo(
            name: "list",
            typeParameters: ["T"]
        );
        _typeFamily.RegisterType(listType);

        // array<T>: 单类型参数
        var arrayType = new GenericTypeInfo(
            name: "array",
            typeParameters: ["T"]
        );
        _typeFamily.RegisterType(arrayType);

        // dict<K, V>: 两个类型参数（键和值）
        var dictType = new GenericTypeInfo(
            name: "dict",
            typeParameters: ["K", "V"]
        );
        _typeFamily.RegisterType(dictType);
    }

    /// <summary>
    /// 注册类类型
    /// </summary>
    public void RegisterClassType(string className, string? baseClassName = null, List<string>? interfaceNames = null)
    {
        ITypeInfo? baseType = null;
        if (!string.IsNullOrEmpty(baseClassName))
        {
            baseType = _typeFamily.GetType(baseClassName);
            if (baseType is null)
            {
                // 如果父类还没有注册，先创建一个占位符
                baseType = new ClassTypeInfo(baseClassName);
                _typeFamily.RegisterType(baseType);
            }
        }

        var classType = new ClassTypeInfo(className, baseType, interfaceNames);
        _typeFamily.RegisterType(classType);
    }

    /// <summary>
    /// 注册接口类型
    /// </summary>
    public void RegisterInterfaceType(string interfaceName, List<string>? parentInterfaceNames = null)
    {
        var interfaceType = new InterfaceTypeInfo(interfaceName, parentInterfaceNames);
        _typeFamily.RegisterType(interfaceType);
    }

    /// <summary>
    /// 注册枚举类型
    /// </summary>
    /// <param name="enumName">枚举名称</param>
    /// <param name="members">枚举成员列表</param>
    public void RegisterEnumType(string enumName, List<string> members)
    {
        var enumType = new EnumTypeInfo(enumName, members);
        _typeFamily.RegisterType(enumType);
    }

    /// <summary>
    /// 解析类型假注表达式（递归支持嵌套泛型）
    /// 支持复杂类型表达式如 "List&lt;int&gt;", "Map&lt;string, List&lt;Person&gt;&gt;", "Shape|Circle", "int?"
    /// </summary>
    public ParsedTypeAnnotation ParseTypeAnnotation(string typeAnnotation)
    {
        if (string.IsNullOrWhiteSpace(typeAnnotation))
        {
            return new ParsedTypeAnnotation { BaseType = "any" };
        }

        return ParseTypeAnnotationRecursive(typeAnnotation.Trim());
    }

    /// <summary>
    /// 递归解析类型假注（内部方法）
    /// </summary>
    private ParsedTypeAnnotation ParseTypeAnnotationRecursive(string typeAnnotation)
    {
        // 处理数组类型（例如 "int[]", "string[]"）
        // 需要在处理联合类型之前处理，因为可能有 "int[] | string[]" 这样的联合类型
        if (typeAnnotation.EndsWith("[]"))
        {
            var elementTypeStr = typeAnnotation.Substring(0, typeAnnotation.Length - 2).Trim();
            var elementType = ParseTypeAnnotationRecursive(elementTypeStr);
            return new ParsedTypeAnnotation
            {
                BaseType = "array",
                GenericArguments = [elementType]
            };
        }

        // 处理联合类型（检查顶层的 |，不检查泛型内部的）
        // 优先处理联合类型，因为 "int? | string?" 中的 ? 是属于各个成员的
        var unionIndex = FindTopLevelSeparator(typeAnnotation, '|');
        if (unionIndex >= 0)
        {
            var types = SplitTopLevel(typeAnnotation, '|');
            return new ParsedTypeAnnotation
            {
                BaseType = "union",
                GenericArguments = types.Select(t => ParseTypeAnnotationRecursive(t.Trim())).ToList()
            };
        }

        // 处理交叉类型（检查顶层的 &，不检查泛型内部的）
        var intersectionIndex = FindTopLevelSeparator(typeAnnotation, '&');
        if (intersectionIndex >= 0)
        {
            var types = SplitTopLevel(typeAnnotation, '&');
            return new ParsedTypeAnnotation
            {
                BaseType = "intersection",
                GenericArguments = types.Select(t => ParseTypeAnnotationRecursive(t.Trim())).ToList()
            };
        }

        // 处理可空类型（例如 "int?", "string?"）
        // 在联合类型和交叉类型之后处理，确保不会误处理 "int? | string?" 的末尾 ?
        if (typeAnnotation.EndsWith('?'))
        {
            var innerTypeStr = typeAnnotation.Substring(0, typeAnnotation.Length - 1).Trim();
            var innerType = ParseTypeAnnotationRecursive(innerTypeStr);
            innerType.IsNullable = true;
            return innerType;
        }

        // 处理泛型类型
        var genericStart = typeAnnotation.IndexOf('<');
        if (genericStart > 0)
        {
            var mainType = typeAnnotation[..genericStart].Trim();
            var genericEnd = FindMatchingBracket(typeAnnotation, genericStart);

            if (genericEnd < 0)
            {
                throw new ArgumentException($"泛型类型语法错误：缺少匹配的 '>'：{typeAnnotation}");
            }

            var paramsPart = typeAnnotation.Substring(genericStart + 1, genericEnd - genericStart - 1).Trim();

            // 递归解析泛型参数
            var parameters = SplitTopLevel(paramsPart, ',')
                .Select(p => ParseTypeAnnotationRecursive(p.Trim()))
                .ToList();

            var result = new ParsedTypeAnnotation
            {
                BaseType = mainType,
                GenericArguments = parameters
            };

            // 检查是否还有可空标记
            if (genericEnd + 1 < typeAnnotation.Length && typeAnnotation[genericEnd + 1] == '?')
            {
                result.IsNullable = true;
            }

            return result;
        }

        // 简单类型
        return new ParsedTypeAnnotation { BaseType = typeAnnotation.Trim() };
    }

    /// <summary>
    /// 查找顶层（不在泛型括号内）的分隔符
    /// </summary>
    private int FindTopLevelSeparator(string text, char separator)
    {
        int depth = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '<') depth++;
            else if (text[i] == '>') depth--;
            else if (text[i] == separator && depth == 0)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 按顶层分隔符分割（不分割泛型括号内的内容）
    /// </summary>
    private List<string> SplitTopLevel(string text, char separator)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        int depth = 0;

        foreach (var ch in text)
        {
            if (ch == '<') depth++;
            else if (ch == '>') depth--;

            if (ch == separator && depth == 0)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    /// <summary>
    /// 查找匹配的右括号
    /// </summary>
    private int FindMatchingBracket(string text, int openIndex)
    {
        int depth = 1;
        for (int i = openIndex + 1; i < text.Length; i++)
        {
            if (text[i] == '<') depth++;
            else if (text[i] == '>') depth--;

            if (depth == 0)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 验证类型假注的兼容性
    /// </summary>
    public bool ValidateTypeCompatibility(string expectedAnnotation, string actualAnnotation)
    {
        var expected = ParseTypeAnnotation(expectedAnnotation);
        var actual = ParseTypeAnnotation(actualAnnotation);

        return ValidateParsedTypeCompatibility(expected, actual);
    }

    /// <summary>
    /// 验证解析后的类型兼容性
    ///
    /// 联合类型兼容性规则：
    /// - A|B 兼容于 A（联合类型可以赋值给任一成员类型）
    /// - A 兼容于 A|B（任一成员类型可以赋值给联合类型）
    ///
    /// 交叉类型兼容性规则：
    /// - A & B 兼容于 A（交叉类型满足所有成员，可以赋值给任一成员）
    /// - A & B 兼容于 B
    /// - A 不兼容于 A & B（单个类型不满足所有约束）
    /// </summary>
    private bool ValidateParsedTypeCompatibility(ParsedTypeAnnotation expected, ParsedTypeAnnotation actual)
    {
        // 处理期望类型是联合类型的情况：A|B
        // 实际类型只需要匹配联合类型的任一成员即可
        if (expected.BaseType == "union")
        {
            return expected.GenericArguments!.Any(expectedMember =>
                ValidateParsedTypeCompatibility(expectedMember, actual));
        }

        // 处理实际类型是联合类型的情况：值为 A|B
        // 联合类型的任一成员都应该兼容于期望类型
        if (actual.BaseType == "union")
        {
            return actual.GenericArguments!.Any(actualMember =>
                ValidateParsedTypeCompatibility(expected, actualMember));
        }

        // 处理期望类型是交叉类型的情况：A&B
        // 实际类型必须满足所有成员约束
        if (expected.BaseType == "intersection")
        {
            return expected.GenericArguments!.All(expectedMember =>
                ValidateParsedTypeCompatibility(expectedMember, actual));
        }

        // 处理实际类型是交叉类型的情况：值为 A&B
        // 交叉类型满足所有成员，可以赋值给任一成员
        if (actual.BaseType == "intersection")
        {
            return actual.GenericArguments!.Any(actualMember =>
                ValidateParsedTypeCompatibility(expected, actualMember));
        }

        // 特殊处理：null 可以赋值给任何可空类型
        if (actual.BaseType == "null" && expected.IsNullable)
        {
            return true;
        }

        // 处理普通类型：先尝试使用 TypeFamily 进行兼容性检查
        // 如果类型未注册（GetType 返回 null），则使用名称匹配作为后备方案
        var expectedTypeInfo = _typeFamily.GetType(expected.BaseType);
        var actualTypeInfo = _typeFamily.GetType(actual.BaseType);

        if (expectedTypeInfo is not null && actualTypeInfo is not null)
        {
            // 两个类型都已注册，使用 TypeFamily 的兼容性检查
            return _typeFamily.IsCompatible(actual.BaseType, expected.BaseType);
        }

        // 如果有类型未注册,使用名称匹配作为后备方案
        // 这允许对未注册的接口类型（如 IComparable）进行基本的类型检查
        if (expected.BaseType != actual.BaseType &&
            expected.BaseType != "any" &&
            actual.BaseType != "any")
        {
            return false;
        }

        // 基类型匹配，检查泛型参数
        if (expected.GenericArguments is not null && actual.GenericArguments is not null)
        {
            if (expected.GenericArguments.Count != actual.GenericArguments.Count)
            {
                return false;
            }

            // 递归检查每个泛型参数
            for (int i = 0; i < expected.GenericArguments.Count; i++)
            {
                if (!ValidateParsedTypeCompatibility(expected.GenericArguments[i], actual.GenericArguments[i]))
                {
                    return false;
                }
            }
        }
        else if (expected.GenericArguments is not null || actual.GenericArguments is not null)
        {
            // 一个有泛型参数，一个没有
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证变量赋值的类型兼容性
    /// </summary>
    public void ValidateVariableAssignment(
        string expectedAnnotation,
        LangValueType actualValue,
        IOldLangTree node,
        string variableName)
    {
        var actualType = GetActualTypeAnnotation(actualValue);

        if (!ValidateTypeCompatibility(expectedAnnotation, actualType))
        {
            throw new Error.TypeError(
                node,
                expectedAnnotation,
                actualType,
                $"变量 '{variableName}' 类型假注不匹配"
            );
        }
    }

    /// <summary>
    /// 获取值的实际类型注解
    /// </summary>
    public string GetActualTypeAnnotation(LangValueType value)
    {
        return value switch
        {
            EnumLangValue enumValue => enumValue.EnumTypeName,
            IntLangValue => "int",
            DoubleLangValue => "double",
            StringLangValue => "string",
            BoolLangValue => "bool",
            CharLangValue => "char",
            AnyLangValue any => any.ClassId.IdName, // 类实例类型
            ArrayLangValue => "array",
            ListLangValue => "list",
            DictionaryLangValue => "dict",
            FuncLangValue => "function",
            TypeTemplate => "class",
            NullLangValue => "null",
            VoidLangValue => "void",
            _ => "any"
        };
    }

    /// <summary>
    /// 检查是否为多态类型
    /// </summary>
    public bool IsPolymorphicType(string typeName)
    {
        return _typeFamily.IsPolymorphicType(typeName);
    }

    /// <summary>
    /// 获取类型的所有子类型
    /// </summary>
    public List<string> GetSubTypes(string typeName)
    {
        return _typeFamily.GetSubTypes(typeName)
            .Select(t => t.Name)
            .ToList();
    }

    /// <summary>
    /// 获取类型族信息
    /// </summary>
    public TypeFamily GetTypeFamily() => _typeFamily;

    /// <summary>
    /// 检查类型是否兼容（支持多态）
    /// </summary>
    public bool IsTypeCompatible(string sourceTypeName, string targetTypeName)
    {
        // 如果任一类型包含联合或交叉类型符号，使用类型注解验证
        if (sourceTypeName.Contains('|') || sourceTypeName.Contains('&') ||
            targetTypeName.Contains('|') || targetTypeName.Contains('&'))
        {
            return ValidateTypeCompatibility(targetTypeName, sourceTypeName);
        }

        // 尝试使用 TypeFamily 的基本兼容性检查
        // 如果 TypeFamily 中未注册某个类型（如 tuple），则回退到基本类型兼容性检查
        var sourceType = _typeFamily.GetType(sourceTypeName);
        var targetType = _typeFamily.GetType(targetTypeName);

        if (sourceType is not null && targetType is not null)
        {
            return _typeFamily.IsCompatible(sourceTypeName, targetTypeName);
        }

        // 回退到基本类型兼容性检查（用于处理内置类型如 tuple、array 等）
        return TypeChecker.IsTypeCompatible(targetTypeName, sourceTypeName);
    }

    /// <summary>
    /// 获取类型的所有成员
    /// </summary>
    public ConcurrentDictionary<string, LangValueType> GetTypeMembers(string typeName, VariateManager manager)
    {
        var typeInfo = _typeFamily.GetType(typeName);
        if (typeInfo is null)
            return new ConcurrentDictionary<string, LangValueType>();

        return typeInfo.GetMembers(manager);
    }
}

/// <summary>
/// 解析后的类型假注信息
/// </summary>
public class ParsedTypeAnnotation
{
    public string BaseType { get; set; } = "";

    /// <summary>
    /// 泛型类型参数（递归结构，支持嵌套泛型）
    /// 例如: List&lt;int> → GenericArguments = [ParsedTypeAnnotation{BaseType="int"}]
    /// 例如: Map&lt;string, List&lt;int>> → GenericArguments = [ParsedTypeAnnotation{BaseType="string"}, ParsedTypeAnnotation{BaseType="List", GenericArguments=[...]}]
    /// </summary>
    public List<ParsedTypeAnnotation>? GenericArguments { get; set; }

    public bool IsNullable { get; set; }

    /// <summary>
    /// 是否为泛型类型
    /// </summary>
    public bool IsGeneric => GenericArguments is { Count: > 0 };

    /// <summary>
    /// 是否为联合类型
    /// </summary>
    public bool IsUnion => BaseType == "union";

    /// <summary>
    /// 是否为交叉类型
    /// </summary>
    public bool IsIntersection => BaseType == "intersection";

    /// <summary>
    /// 获取完整的类型名称（包括泛型参数）
    /// </summary>
    public string GetFullName()
    {
        if (IsUnion && GenericArguments is not null)
        {
            return string.Join(" | ", GenericArguments.Select(arg => arg.GetFullName()));
        }

        if (IsIntersection && GenericArguments is not null)
        {
            return string.Join(" & ", GenericArguments.Select(arg => arg.GetFullName()));
        }

        if (!IsGeneric)
        {
            return BaseType + (IsNullable ? "?" : "");
        }

        if (GenericArguments is null) return BaseType + (IsNullable ? "?" : "");
        {
            var args = string.Join(", ", GenericArguments.Select(arg => arg.GetFullName()));
            return $"{BaseType}<{args}>" + (IsNullable ? "?" : "");
        }
    }
}