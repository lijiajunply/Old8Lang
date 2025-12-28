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
    /// 解析类型假注表达式
    /// 支持复杂类型表达式如 "List&lt;int&gt;", "Map&lt;string, Person&gt;", "Shape|Circle", "int?"
    /// </summary>
    public ParsedTypeAnnotation ParseTypeAnnotation(string typeAnnotation)
    {
        if (string.IsNullOrEmpty(typeAnnotation))
        {
            return new ParsedTypeAnnotation { BaseType = "any" };
        }

        // 处理可空类型（例如 "int?", "string?"）
        if (typeAnnotation.EndsWith('?'))
        {
            var innerType = typeAnnotation.Substring(0, typeAnnotation.Length - 1).Trim();
            return new ParsedTypeAnnotation
            {
                BaseType = innerType,
                IsNullable = true
            };
        }

        // 处理联合类型
        if (typeAnnotation.Contains('|'))
        {
            var types = typeAnnotation.Split('|', StringSplitOptions.RemoveEmptyEntries);
            return new ParsedTypeAnnotation
            {
                BaseType = "union",
                TypeParameters = types.Select(t => t.Trim()).ToList()
            };
        }

        // 处理泛型类型
        if (typeAnnotation.Contains('<') && typeAnnotation.Contains('>'))
        {
            var mainType = typeAnnotation[..typeAnnotation.IndexOf('<')].Trim();
            var paramsPart = typeAnnotation.Substring(
                typeAnnotation.IndexOf('<') + 1,
                typeAnnotation.LastIndexOf('>') - typeAnnotation.IndexOf('<') - 1);

            var parameters = paramsPart.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).ToList();

            return new ParsedTypeAnnotation
            {
                BaseType = mainType,
                TypeParameters = parameters
            };
        }

        // 简单类型
        return new ParsedTypeAnnotation { BaseType = typeAnnotation.Trim() };
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
}

/// <summary>
/// 解析后的类型假注信息
/// </summary>
public class ParsedTypeAnnotation
{
    public string BaseType { get; set; } = "";
    public List<string>? TypeParameters { get; set; }
    public bool IsNullable { get; set; } = false;
    public bool IsGeneric => TypeParameters is { Count: > 0 };
    public bool IsUnion => BaseType == "union";
}