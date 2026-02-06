using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// 实例方法重载组 - 管理同名实例方法的多个重载版本
/// </summary>
public class InstanceMethodOverloadGroup
{
    private readonly List<IInstanceMethod> _overloads = new();
    private readonly Lock _lock = new();

    /// <summary>
    /// 方法名称
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// 重载数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _overloads.Count;
            }
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="methodName">方法名称</param>
    /// <param name="targetType">目标类型</param>
    public InstanceMethodOverloadGroup(string methodName, Type targetType)
    {
        MethodName = methodName;
        TargetType = targetType;
    }

    /// <summary>
    /// 添加重载
    /// </summary>
    /// <param name="method">要添加的方法</param>
    public void AddOverload(IInstanceMethod method)
    {
        lock (_lock)
        {
            // 检查是否已经添加过这个方法对象
            if (_overloads.Any(m => ReferenceEquals(m, method)))
            {
                return;
            }

            _overloads.Add(method);
        }
    }

    /// <summary>
    /// 解析最匹配的重载
    /// </summary>
    /// <param name="parameters">参数表达式列表</param>
    /// <param name="local">局部变量管理器（可选）</param>
    /// <returns>最匹配的方法，如果没有匹配返回 null</returns>
    public IInstanceMethod? ResolveOverload(List<LangExpression> parameters, LocalManager? local)
    {
        lock (_lock)
        {
            // 如果只有一个重载，直接返回
            if (_overloads.Count == 1)
            {
                var singleOverload = _overloads[0];
                return singleOverload.CanAccept(parameters, local) ? singleOverload : null;
            }

            // 计算每个重载的匹配分数
            var candidates = new List<(IInstanceMethod Method, int Score)>();

            foreach (var overload in _overloads)
            {
                var score = overload.CalculateMatchScore(parameters, local);
                if (score >= 0)
                {
                    candidates.Add((overload, score));
                }
            }

            // 如果没有匹配的重载
            if (candidates.Count == 0)
            {
                return null;
            }

            // 选择分数最高的重载
            var bestMatch = candidates.OrderByDescending(c => c.Score).First();
            return bestMatch.Method;
        }
    }

    /// <summary>
    /// 获取所有重载的签名信息（用于 IDE）
    /// </summary>
    /// <returns>签名信息列表</returns>
    public List<MethodSignatureInfo> GetAllSignatures()
    {
        lock (_lock)
        {
            var signatures = new List<MethodSignatureInfo>();

            foreach (var overload in _overloads)
            {
                var parameters = new List<ParameterSignatureInfo>();

                // 构建参数信息
                if (overload.ParameterNames != null && overload.ParameterTypes != null)
                {
                    for (int i = 0; i < overload.ParameterNames.Length; i++)
                    {
                        var paramName = overload.ParameterNames[i];
                        var paramType = i < overload.ParameterTypes.Length ? overload.ParameterTypes[i] : null;
                        var isOptional = i >= overload.MinParameterCount;

                        parameters.Add(new ParameterSignatureInfo
                        {
                            Name = paramName,
                            TypeName = paramType?.Name ?? "any",
                            IsOptional = isOptional
                        });
                    }
                }
                else
                {
                    // 如果没有参数名称，使用默认名称
                    var paramCount = overload.MaxParameterCount != -1
                        ? overload.MaxParameterCount
                        : overload.MinParameterCount;

                    for (int i = 0; i < paramCount; i++)
                    {
                        var isOptional = i >= overload.MinParameterCount;
                        parameters.Add(new ParameterSignatureInfo
                        {
                            Name = $"arg{i}",
                            TypeName = "any",
                            IsOptional = isOptional
                        });
                    }
                }

                // 构建签名字符串
                var paramSignatures = parameters.Select(p =>
                {
                    var optional = p.IsOptional ? "?" : "";
                    return $"{p.Name}{optional}: {p.TypeName}";
                });

                var returnTypeName = overload.DeclaredReturnType?.Name ?? "any";
                var signature = $"{MethodName}({string.Join(", ", paramSignatures)}) -> {returnTypeName}";

                signatures.Add(new MethodSignatureInfo
                {
                    Name = MethodName,
                    Signature = signature,
                    Documentation = overload.Documentation,
                    Parameters = parameters,
                    ReturnTypeName = returnTypeName
                });
            }

            return signatures;
        }
    }

    /// <summary>
    /// 获取所有重载
    /// </summary>
    /// <returns>重载列表</returns>
    public List<IInstanceMethod> GetAllOverloads()
    {
        lock (_lock)
        {
            return _overloads.ToList();
        }
    }
}

/// <summary>
/// 方法签名信息
/// </summary>
public class MethodSignatureInfo
{
    /// <summary>
    /// 方法名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 完整签名字符串
    /// </summary>
    public required string Signature { get; init; }

    /// <summary>
    /// 方法文档
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// 参数列表
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
    public required string TypeName { get; init; }

    /// <summary>
    /// 是否可选
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// 参数文档
    /// </summary>
    public string? Documentation { get; init; }
}
