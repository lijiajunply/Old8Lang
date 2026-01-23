using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 泛型类型解析器，用于将泛型参数解析为具体类型
/// </summary>
public class GenericTypeResolver(
    Dictionary<string, Type> typeMapping,
    LocalManager local,
    LangInterpreter? interpreter = null)
{
    /// <summary>
    /// 解析类型，如果是泛型参数则替换为具体类型
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <returns>解析后的Type</returns>
    public Type ResolveType(string typeName)
    {
        // 如果是泛型参数，从映射中获取
        if (typeMapping.TryGetValue(typeName, out var mappedType))
        {
            return mappedType;
        }

        // 否则尝试从类型注解管理器获取
        var typeInfo = interpreter?.TypeAnnotationManager.GetTypeFamily().GetType(typeName);
        if (typeInfo != null)
        {
            // 这里需要将ITypeInfo转换为System.Type
            // 暂时使用基本的类型映射
            var resolved = typeName.ToLower() switch
            {
                "int" => typeof(int),
                "string" => typeof(string),
                "double" => typeof(double),
                "bool" => typeof(bool),
                "char" => typeof(char),
                "void" => typeof(void),
                "object" => typeof(object),
                _ => null
            };
            if (resolved != null) return resolved;

            // 尝试解析常用系统接口
            if (typeName == "IComparable") return typeof(IComparable);
            if (typeName == "IDisposable") return typeof(IDisposable);
            if (typeName == "IEnumerable") return typeof(System.Collections.IEnumerable);
        }

        // 尝试从 LocalManager 获取已编译的类
        if (local.ClassVar.TryGetValue(typeName, out var classType))
        {
            return classType;
        }

        // 尝试从 LocalManager 获取泛型类模板（递归特化）
        // 注意：这里我们可能无法直接返回一个特化的Type，因为缺少泛型参数
        // 但如果只是类名引用（如 MyClass），它应该在 ClassVar 中
        // 如果 MyClass 还没有被编译（顺序问题），这可能是一个问题
        // 但编译器通常会预先扫描所有类定义

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
                types[i] = ResolveType(param.AssumptionType);
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

        return ResolveType(returnTypeAnnotation);
    }
}