using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 泛型类型解析器，用于将泛型参数解析为具体类型
/// </summary>
public class GenericTypeResolver
{
    private readonly Dictionary<string, Type> _typeMapping;
    private readonly LocalManager _local;
    private readonly LangInterpreter? _interpreter;

    public GenericTypeResolver(Dictionary<string, Type> typeMapping, LocalManager local, LangInterpreter? interpreter = null)
    {
        _typeMapping = typeMapping;
        _local = local;
        _interpreter = interpreter;
    }

    /// <summary>
    /// 解析类型，如果是泛型参数则替换为具体类型
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <returns>解析后的Type</returns>
    public Type? ResolveType(string typeName)
    {
        // 如果是泛型参数，从映射中获取
        if (_typeMapping.TryGetValue(typeName, out var mappedType))
        {
            return mappedType;
        }

        // 否则尝试从类型注解管理器获取
        if (_interpreter?.TypeAnnotationManager != null)
        {
            var typeInfo = _interpreter.TypeAnnotationManager.GetTypeFamily().GetType(typeName);
            if (typeInfo != null)
            {
                // 这里需要将ITypeInfo转换为System.Type
                // 暂时使用基本的类型映射
                return typeName.ToLower() switch
                {
                    "int" => typeof(int),
                    "string" => typeof(string),
                    "double" => typeof(double),
                    "bool" => typeof(bool),
                    "char" => typeof(char),
                    "void" => typeof(void),
                    _ => typeof(object)
                };
            }
        }

        // 基本类型映射
        return typeName.ToLower() switch
        {
            "int" => typeof(int),
            "string" => typeof(string),
            "double" => typeof(double),
            "bool" => typeof(bool),
            "char" => typeof(char),
            "void" => typeof(void),
            _ => typeof(object)
        };
    }

    /// <summary>
    /// 解析参数类型数组
    /// </summary>
    /// <param name="parameters">参数ID列表</param>
    /// <returns>解析后的类型数组</returns>
    public Type[] ResolveParameterTypes(List<LangId> parameters)
    {
        var types = new Type[parameters.Count];
        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            if (!string.IsNullOrEmpty(param.AssumptionType))
            {
                types[i] = ResolveType(param.AssumptionType) ?? typeof(object);
            }
            else
            {
                types[i] = typeof(object);
            }
        }
        return types;
    }

    /// <summary>
    /// 解析返回类型
    /// </summary>
    /// <param name="returnTypeAnnotation">返回类型注解</param>
    /// <returns>解析后的返回类型</returns>
    public Type ResolveReturnType(string? returnTypeAnnotation)
    {
        if (string.IsNullOrEmpty(returnTypeAnnotation))
        {
            return typeof(void);
        }
        return ResolveType(returnTypeAnnotation) ?? typeof(void);
    }
}