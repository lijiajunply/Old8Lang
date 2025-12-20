using System.Collections.Concurrent;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
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
        _typeFamily.RegisterType(new PrimitiveTypeInfo("array"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("list"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("dict"));
        _typeFamily.RegisterType(new PrimitiveTypeInfo("function"));
    }

    /// <summary>
    /// 注册类类型
    /// </summary>
    public void RegisterClassType(string className, string? baseClassName = null)
    {
        ITypeInfo? baseType = null;
        if (!string.IsNullOrEmpty(baseClassName))
        {
            baseType = _typeFamily.GetType(baseClassName);
            if (baseType == null)
            {
                // 如果父类还没有注册，先创建一个占位符
                baseType = new ClassTypeInfo(baseClassName);
                _typeFamily.RegisterType(baseType);
            }
        }

        var classType = new ClassTypeInfo(className, baseType);
        _typeFamily.RegisterType(classType);
    }

    /// <summary>
    /// 解析类型假注表达式
    /// 支持复杂类型表达式如 "List<int>", "Map<string, Person>", "Shape|Circle"
    /// </summary>
    public ParsedTypeAnnotation ParseTypeAnnotation(string typeAnnotation)
    {
        if (string.IsNullOrEmpty(typeAnnotation))
        {
            return new ParsedTypeAnnotation { BaseType = "any" };
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
            var mainType = typeAnnotation.Substring(0, typeAnnotation.IndexOf('<')).Trim();
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
                _typeFamily.IsCompatible(actual.BaseType, type));
        }

        if (actual.BaseType == "union")
        {
            return actual.TypeParameters!.Any(type =>
                _typeFamily.IsCompatible(type, expected.BaseType));
        }

        // 处理多态类型
        return _typeFamily.IsCompatible(actual.BaseType, expected.BaseType);
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
            AnyLangValue any => any.Id.IdName, // 类实例类型
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
        return _typeFamily.IsCompatible(sourceTypeName, targetTypeName);
    }

    /// <summary>
    /// 获取类型的所有成员
    /// </summary>
    public ConcurrentDictionary<string, LangValueType> GetTypeMembers(string typeName, VariateManager manager)
    {
        var typeInfo = _typeFamily.GetType(typeName);
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
    public bool IsGeneric => TypeParameters != null && TypeParameters.Count > 0;
    public bool IsUnion => BaseType == "union";
}