using Old8Lang.AST;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.GlobalFunctions.Core;

/// <summary>
/// 函数签名信息（用于 IDE 显示）
/// </summary>
public class FunctionSignatureInfo
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 完整签名字符串（如 "Print(value:any) -> void"）
    /// </summary>
    public required string Signature { get; init; }

    /// <summary>
    /// 函数文档
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// 参数信息列表
    /// </summary>
    public required List<ParameterSignatureInfo> Parameters { get; init; }

    /// <summary>
    /// 返回类型名称
    /// </summary>
    public string? ReturnTypeName { get; init; }
}

/// <summary>
/// 参数签名信息
/// </summary>
public class ParameterSignatureInfo
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 参数类型名称
    /// </summary>
    public string? TypeName { get; init; }

    /// <summary>
    /// 参数文档
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// 完整标签（如 "value:string"）
    /// </summary>
    public string Label => TypeName != null ? $"{Name}:{TypeName}" : Name;
}

/// <summary>
/// 重载组 - 管理同名函数的多个重载版本
/// </summary>
public class OverloadGroup
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 所有重载版本
    /// </summary>
    public List<IGlobalFunction> Overloads { get; } = [];

    /// <summary>
    /// 创建重载组
    /// </summary>
    /// <param name="name">函数名称</param>
    public OverloadGroup(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 添加一个重载
    /// </summary>
    /// <param name="function">要添加的函数</param>
    public void AddOverload(IGlobalFunction function)
    {
        // 检查是否已经存在相同的函数对象
        if (Overloads.Any(f => ReferenceEquals(f, function)))
            return;

        Overloads.Add(function);
    }

    /// <summary>
    /// 解析最匹配的重载
    /// </summary>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="local">局部变量管理器（可为 null）</param>
    /// <returns>最匹配的函数，如果没有匹配返回 null</returns>
    public IGlobalFunction? ResolveOverload(List<LangExpression> parameters, LocalManager? local)
    {
        if (Overloads.Count == 0)
            return null;

        // 如果只有一个重载，直接返回
        if (Overloads.Count == 1)
            return Overloads[0];

        // 计算每个重载的匹配分数
        var candidates = new List<(IGlobalFunction func, int score)>();

        foreach (var overload in Overloads)
        {
            var score = overload.CalculateMatchScore(parameters, local);
            if (score >= 0)
            {
                candidates.Add((overload, score));
            }
        }

        if (candidates.Count == 0)
        {
            // 没有精确匹配，尝试找一个可以接受参数数量的重载
            foreach (var overload in Overloads)
            {
                if (overload.CanAccept(parameters, local))
                {
                    return overload;
                }
            }
            return null;
        }

        // 选择分数最高的重载
        return candidates.OrderByDescending(c => c.score).First().func;
    }

    /// <summary>
    /// 获取所有重载的签名信息（用于 IDE 显示）
    /// </summary>
    /// <returns>签名信息列表</returns>
    public List<FunctionSignatureInfo> GetAllSignatures()
    {
        var signatures = new List<FunctionSignatureInfo>();

        foreach (var overload in Overloads)
        {
            signatures.Add(BuildSignatureInfo(overload));
        }

        return signatures;
    }

    /// <summary>
    /// 构建单个函数的签名信息
    /// </summary>
    private FunctionSignatureInfo BuildSignatureInfo(IGlobalFunction function)
    {
        var parameters = new List<ParameterSignatureInfo>();
        var paramNames = function.ParameterNames ?? [];
        var paramTypes = function.ParameterTypes ?? [];

        // 确定参数数量
        int paramCount = Math.Max(
            paramNames.Length,
            Math.Max(paramTypes.Length, function.MinParameterCount)
        );

        for (int i = 0; i < paramCount; i++)
        {
            var paramName = i < paramNames.Length ? paramNames[i] : $"arg{i}";
            var paramType = i < paramTypes.Length ? paramTypes[i] : null;

            parameters.Add(new ParameterSignatureInfo
            {
                Name = paramName,
                TypeName = GetTypeName(paramType),
                Documentation = null
            });
        }

        // 构建签名字符串
        var paramStr = string.Join(", ", parameters.Select(p => p.Label));
        var signature = $"{function.Names[0]}({paramStr})";

        var returnTypeName = GetTypeName(function.DeclaredReturnType);
        if (returnTypeName != null)
        {
            signature += $" -> {returnTypeName}";
        }

        return new FunctionSignatureInfo
        {
            Name = function.Names[0],
            Signature = signature,
            Documentation = function.Documentation,
            Parameters = parameters,
            ReturnTypeName = returnTypeName
        };
    }

    /// <summary>
    /// 获取类型的友好名称
    /// </summary>
    private static string? GetTypeName(Type? type)
    {
        if (type == null)
            return null;

        // 常见类型的友好名称映射
        var typeNames = new Dictionary<Type, string>
        {
            { typeof(void), "void" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(decimal), "decimal" },
            { typeof(bool), "bool" },
            { typeof(string), "string" },
            { typeof(char), "char" },
            { typeof(byte), "byte" },
            { typeof(object), "any" }
        };

        if (typeNames.TryGetValue(type, out var name))
            return name;

        // 处理泛型类型
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();
            var argNames = string.Join(", ", genericArgs.Select(GetTypeName));

            if (genericDef == typeof(List<>))
                return $"list<{argNames}>";
            if (genericDef == typeof(Dictionary<,>))
                return $"dict<{argNames}>";

            return $"{type.Name.Split('`')[0]}<{argNames}>";
        }

        // 处理数组类型
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            return $"array<{GetTypeName(elementType)}>";
        }

        return type.Name;
    }
}
