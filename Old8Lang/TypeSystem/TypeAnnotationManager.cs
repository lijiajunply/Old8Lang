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
    private readonly TypeFamily TypeFamily = new();
    private readonly VariateManager GlobalManager;

    public TypeAnnotationManager(VariateManager globalManager)
    {
        GlobalManager = globalManager;
        InitializeBasicTypes();
    }

    /// <summary>
    /// 获取全局变量管理器
    /// </summary>
    public VariateManager GetGlobalManager() => GlobalManager;

    /// <summary>
    /// 初始化基本类型
    /// </summary>
    private void InitializeBasicTypes()
    {
        // 注册基本类型
        TypeFamily.RegisterType(new PrimitiveTypeInfo("int"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("double"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("string"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("bool"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("char"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("void"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("any"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("null"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("array"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("list"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("dict"));
        TypeFamily.RegisterType(new PrimitiveTypeInfo("function"));
    }

    /// <summary>
    /// 注册类类型
    /// </summary>
    public void RegisterClassType(string className, string? baseClassName = null)
    {
        ITypeInfo? baseType = null;
        if (!string.IsNullOrEmpty(baseClassName))
        {
            baseType = TypeFamily.GetType(baseClassName);
            if (baseType == null)
            {
                // 如果父类还没有注册，先创建一个占位符
                baseType = new ClassTypeInfo(baseClassName);
                TypeFamily.RegisterType(baseType);
            }
        }

        var classType = new ClassTypeInfo(className, baseType);
        TypeFamily.RegisterType(classType);
    }

    /// <summary>
    /// 解析类型假注表达式（递归支持嵌套泛型）
    /// 支持复杂类型表达式如 "List&lt;int&gt;", "Map&lt;string, List&lt;Person&gt;&gt;", "Shape|Circle", "int?"
    /// </summary>
    public ParsedTypeAnnotation ParseTypeAnnotation(string typeAnnotation)
    {
        if (string.IsNullOrEmpty(typeAnnotation))
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
        // 处理可空类型（例如 "int?", "string?"）
        if (typeAnnotation.EndsWith('?'))
        {
            var innerTypeStr = typeAnnotation.Substring(0, typeAnnotation.Length - 1).Trim();
            var innerType = ParseTypeAnnotationRecursive(innerTypeStr);
            innerType.IsNullable = true;
            return innerType;
        }

        // 处理联合类型（检查顶层的 |，不检查泛型内部的）
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
    /// </summary>
    private bool ValidateParsedTypeCompatibility(ParsedTypeAnnotation expected, ParsedTypeAnnotation actual)
    {
        // 处理联合类型
        if (expected.BaseType == "union")
        {
            return expected.TypeParameters!.Any(type =>
                TypeFamily.IsCompatible(actual.BaseType, type));
        }

        if (actual.BaseType == "union")
        {
            return actual.TypeParameters!.Any(type =>
                TypeFamily.IsCompatible(type, expected.BaseType));
        }

        // 处理多态类型
        return TypeFamily.IsCompatible(actual.BaseType, expected.BaseType);
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
        return TypeFamily.IsPolymorphicType(typeName);
    }

    /// <summary>
    /// 获取类型的所有子类型
    /// </summary>
    public List<string> GetSubTypes(string typeName)
    {
        return TypeFamily.GetSubTypes(typeName)
            .Select(t => t.Name)
            .ToList();
    }

    /// <summary>
    /// 获取类型族信息
    /// </summary>
    public TypeFamily GetTypeFamily() => TypeFamily;

    /// <summary>
    /// 检查类型是否兼容（支持多态）
    /// </summary>
    public bool IsTypeCompatible(string sourceTypeName, string targetTypeName)
    {
        return TypeFamily.IsCompatible(sourceTypeName, targetTypeName);
    }

    /// <summary>
    /// 获取类型的所有成员
    /// </summary>
    public ConcurrentDictionary<string, LangValueType> GetTypeMembers(string typeName, VariateManager manager)
    {
        var typeInfo = TypeFamily.GetType(typeName);
        if (typeInfo == null)
            return new ConcurrentDictionary<string, LangValueType>();

        return typeInfo.GetMembers(manager);
    }

    /// <summary>
    /// 注册泛型类型定义
    /// </summary>
    public void RegisterGenericType(
        string name,
        List<string> typeParameters,
        Dictionary<string, List<ITypeInfo>>? constraints = null,
        string? baseClassName = null)
    {
        ITypeInfo? baseType = null;
        if (!string.IsNullOrEmpty(baseClassName))
        {
            baseType = TypeFamily.GetType(baseClassName);
        }

        var genericType = new GenericTypeInfo(name, typeParameters, constraints, baseType);
        TypeFamily.RegisterType(genericType);
    }

    /// <summary>
    /// 实例化泛型类型
    /// </summary>
    public GenericTypeInfo? InstantiateGenericType(string name, Dictionary<string, ITypeInfo> typeArguments)
    {
        var genericDefinition = TypeFamily.GetType(name);
        if (genericDefinition is not GenericTypeInfo genericType)
        {
            return null;
        }

        return genericType.Instantiate(typeArguments);
    }

    /// <summary>
    /// 将 ParsedTypeAnnotation 转换为 ITypeInfo
    /// </summary>
    public ITypeInfo? ResolveTypeAnnotation(ParsedTypeAnnotation annotation)
    {
        // 处理泛型类型
        if (annotation.IsGeneric && annotation.GenericArguments != null)
        {
            var baseTypeInfo = TypeFamily.GetType(annotation.BaseType);
            if (baseTypeInfo is GenericTypeInfo genericType)
            {
                // 解析泛型参数
                var typeArgs = new Dictionary<string, ITypeInfo>();
                for (int i = 0; i < annotation.GenericArguments.Count && i < genericType.TypeParameters.Count; i++)
                {
                    var argAnnotation = annotation.GenericArguments[i];
                    var argType = ResolveTypeAnnotation(argAnnotation);
                    if (argType != null)
                    {
                        typeArgs[genericType.TypeParameters[i]] = argType;
                    }
                }

                return genericType.Instantiate(typeArgs);
            }
        }

        // 处理简单类型
        return TypeFamily.GetType(annotation.BaseType);
    }

    /// <summary>
    /// 验证泛型约束
    /// </summary>
    public bool ValidateGenericConstraints(GenericTypeInfo genericType, Dictionary<string, ITypeInfo> typeArguments)
    {
        if (genericType.Constraints == null) return true;

        foreach (var (paramName, constraintTypes) in genericType.Constraints)
        {
            if (typeArguments.TryGetValue(paramName, out var actualType))
            {
                foreach (var constraintType in constraintTypes)
                {
                    if (!actualType.IsCompatibleWith(constraintType))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
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

    /// <summary>
    /// 旧的类型参数（为保持兼容性保留，但推荐使用 GenericArguments）
    /// </summary>
    [Obsolete("请使用 GenericArguments 代替")]
    public List<string>? TypeParameters { get; set; }

    /// <summary>
    /// 类型约束（用于泛型定义时的约束）
    /// 例如: T: IComparable → Constraints = [ParsedTypeAnnotation{BaseType="IComparable"}]
    /// </summary>
    public List<ParsedTypeAnnotation>? Constraints { get; set; }

    public bool IsNullable { get; set; } = false;

    /// <summary>
    /// 是否为泛型类型
    /// </summary>
    public bool IsGeneric => GenericArguments is { Count: > 0 } || TypeParameters is { Count: > 0 };

    /// <summary>
    /// 是否为联合类型
    /// </summary>
    public bool IsUnion => BaseType == "union";

    /// <summary>
    /// 获取完整的类型名称（包括泛型参数）
    /// </summary>
    public string GetFullName()
    {
        if (IsUnion && GenericArguments != null)
        {
            return string.Join("|", GenericArguments.Select(arg => arg.GetFullName()));
        }

        if (!IsGeneric)
        {
            return BaseType + (IsNullable ? "?" : "");
        }

        if (GenericArguments != null)
        {
            var args = string.Join(", ", GenericArguments.Select(arg => arg.GetFullName()));
            return $"{BaseType}<{args}>" + (IsNullable ? "?" : "");
        }

        // 兼容旧格式
        if (TypeParameters != null)
        {
            var args = string.Join(", ", TypeParameters);
            return $"{BaseType}<{args}>" + (IsNullable ? "?" : "");
        }

        return BaseType + (IsNullable ? "?" : "");
    }
}