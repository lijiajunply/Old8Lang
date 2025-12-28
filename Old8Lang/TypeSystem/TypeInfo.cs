using System.Collections.Concurrent;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Interpreter;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 类型信息接口，定义类型假注系统的基础抽象
/// </summary>
public interface ITypeInfo
{
    /// <summary>
    /// 类型名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 是否为类类型
    /// </summary>
    bool IsClassType { get; }

    /// <summary>
    /// 父类型（用于继承和多态）
    /// </summary>
    ITypeInfo? BaseType { get; }

    /// <summary>
    /// 检查是否兼容指定类型（支持多态）
    /// </summary>
    bool IsCompatibleWith(ITypeInfo other);

    /// <summary>
    /// 获取类型的所有成员
    /// </summary>
    ConcurrentDictionary<string, LangValueType> GetMembers(VariateManager manager);
}

/// <summary>
/// 基本类型信息（int, string, double等）
/// </summary>
public class PrimitiveTypeInfo(string name) : ITypeInfo
{
    public string Name { get; } = name;
    public bool IsClassType => false;
    public ITypeInfo? BaseType => null;

    public bool IsCompatibleWith(ITypeInfo other)
    {
        // 基本类型的兼容性规则
        if (Name == other.Name) return true;
        if (Name == "any") return true;
        if (other.Name == "any") return true;

        // 数值类型兼容性
        if (Name == "double" && other.Name == "int") return true;

        return false;
    }

    public ConcurrentDictionary<string, LangValueType> GetMembers(VariateManager manager) => [];
}

/// <summary>
/// 类类型信息，支持继承和多态
/// </summary>
public class ClassTypeInfo(string name, ITypeInfo? baseType = null) : ITypeInfo
{
    public string Name { get; } = name;
    public bool IsClassType => true;
    public ITypeInfo? BaseType { get; } = baseType;

    private readonly ConcurrentDictionary<string, LangValueType> CachedMembers = [];

    public bool IsCompatibleWith(ITypeInfo other)
    {
        if (Name == other.Name) return true;
        if (other.Name == "any") return true;

        // 支持继承多态：子类可以赋值给父类类型
        var current = this;
        while (current.BaseType != null)
        {
            if (current.BaseType.Name == other.Name) return true;
            current = (ClassTypeInfo)current.BaseType;
        }

        return false;
    }

    public ConcurrentDictionary<string, LangValueType> GetMembers(VariateManager manager)
    {
        // 如果有缓存，直接返回
        if (CachedMembers.Count > 0) return CachedMembers;

        var members = new ConcurrentDictionary<string, LangValueType>();

        // 获取类型模板
        if (manager.GetAny(new LangId(Name)) is TypeTemplate typeTemplate)
        {
            // 添加实例成员
            foreach (var member in typeTemplate.Variates)
            {
                var value = member.Value.Run(manager);
                members.TryAdd(member.Key.IdName, value);
            }

            // 递归获取父类成员
            if (BaseType != null && BaseType is ClassTypeInfo baseClassInfo)
            {
                var baseMembers = baseClassInfo.GetMembers(manager);
                foreach (var member in baseMembers)
                {
                    members.TryAdd(member.Key, member.Value);
                }
            }
        }

        // 缓存结果
        foreach (var member in members)
        {
            CachedMembers.TryAdd(member.Key, member.Value);
        }

        return CachedMembers;
    }

    /// <summary>
    /// 清除缓存，当类定义发生变化时调用
    /// </summary>
    public void ClearCache()
    {
        CachedMembers.Clear();
    }
}

/// <summary>
/// 泛型类型信息，支持类型参数
/// </summary>
public class GenericTypeInfo : ITypeInfo
{
    public string Name { get; }
    public List<string> TypeParameters { get; }
    public Dictionary<string, ITypeInfo>? TypeArguments { get; }
    public Dictionary<string, List<ITypeInfo>>? Constraints { get; }
    public ITypeInfo? BaseType { get; }
    public bool IsClassType => true;

    /// <summary>
    /// 判断是否为泛型定义（未实例化）
    /// </summary>
    public bool IsGenericDefinition => TypeArguments == null;

    /// <summary>
    /// 判断是否为泛型实例（已实例化）
    /// </summary>
    public bool IsGenericInstance => TypeArguments != null;

    private readonly ConcurrentDictionary<string, LangValueType> CachedMembers = [];

    /// <summary>
    /// 构造泛型定义
    /// </summary>
    public GenericTypeInfo(
        string name,
        List<string> typeParameters,
        Dictionary<string, List<ITypeInfo>>? constraints = null,
        ITypeInfo? baseType = null)
    {
        Name = name;
        TypeParameters = typeParameters;
        Constraints = constraints;
        BaseType = baseType;
        TypeArguments = null;
    }

    /// <summary>
    /// 构造泛型实例（私有，通过 Instantiate 方法创建）
    /// </summary>
    private GenericTypeInfo(
        string name,
        List<string> typeParameters,
        Dictionary<string, ITypeInfo> typeArguments,
        Dictionary<string, List<ITypeInfo>>? constraints,
        ITypeInfo? baseType)
    {
        Name = name;
        TypeParameters = typeParameters;
        TypeArguments = typeArguments;
        Constraints = constraints;
        BaseType = baseType;
    }

    /// <summary>
    /// 实例化泛型类型
    /// </summary>
    public GenericTypeInfo Instantiate(Dictionary<string, ITypeInfo> typeArguments)
    {
        if (!IsGenericDefinition)
        {
            throw new InvalidOperationException($"类型 {Name} 已经是实例化的泛型类型");
        }

        // 验证类型参数数量
        if (typeArguments.Count != TypeParameters.Count)
        {
            throw new ArgumentException(
                $"类型参数数量不匹配：期望 {TypeParameters.Count} 个，实际 {typeArguments.Count} 个");
        }

        // 验证类型参数约束
        if (Constraints != null)
        {
            foreach (var (paramName, constraintTypes) in Constraints)
            {
                if (typeArguments.TryGetValue(paramName, out var actualType))
                {
                    foreach (var constraintType in constraintTypes)
                    {
                        if (!actualType.IsCompatibleWith(constraintType))
                        {
                            throw new ArgumentException(
                                $"类型 {actualType.Name} 不满足约束 {constraintType.Name}");
                        }
                    }
                }
            }
        }

        // 创建新的泛型实例
        return new GenericTypeInfo(Name, TypeParameters, typeArguments, Constraints, BaseType);
    }

    /// <summary>
    /// 替换类型参数
    /// </summary>
    public ITypeInfo SubstituteTypeParameters(Dictionary<string, ITypeInfo> substitutions)
    {
        if (IsGenericDefinition)
        {
            // 如果是泛型定义，执行实例化
            return Instantiate(substitutions);
        }

        // 如果是泛型实例，替换类型参数
        var newTypeArguments = new Dictionary<string, ITypeInfo>();
        foreach (var (paramName, paramType) in TypeArguments!)
        {
            if (paramType is GenericTypeInfo genericParam)
            {
                newTypeArguments[paramName] = genericParam.SubstituteTypeParameters(substitutions);
            }
            else if (substitutions.ContainsKey(paramType.Name))
            {
                newTypeArguments[paramName] = substitutions[paramType.Name];
            }
            else
            {
                newTypeArguments[paramName] = paramType;
            }
        }

        return new GenericTypeInfo(Name, TypeParameters, newTypeArguments, Constraints, BaseType);
    }

    public bool IsCompatibleWith(ITypeInfo other)
    {
        // 泛型类型兼容性检查
        if (Name == other.Name)
        {
            // 如果都是泛型实例，检查类型参数是否兼容
            if (other is GenericTypeInfo otherGeneric &&
                IsGenericInstance && otherGeneric.IsGenericInstance)
            {
                if (TypeArguments!.Count != otherGeneric.TypeArguments!.Count)
                    return false;

                foreach (var (paramName, thisType) in TypeArguments)
                {
                    if (otherGeneric.TypeArguments.TryGetValue(paramName, out var otherType))
                    {
                        if (!thisType.IsCompatibleWith(otherType))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }

            return true;
        }

        if (other.Name == "any") return true;

        // 支持继承多态
        var current = this;
        while (current.BaseType != null)
        {
            if (current.BaseType.Name == other.Name) return true;
            if (current.BaseType is GenericTypeInfo genericBase)
                current = genericBase;
            else
                break;
        }

        return false;
    }

    public ConcurrentDictionary<string, LangValueType> GetMembers(VariateManager manager)
    {
        if (CachedMembers.Count > 0) return CachedMembers;

        // 泛型类型的成员获取需要考虑类型参数替换
        // 这部分将在后续集成 TypeTemplate 时实现
        return CachedMembers;
    }

    /// <summary>
    /// 获取完整的泛型类型名称
    /// </summary>
    public string GetFullName()
    {
        if (IsGenericDefinition)
        {
            return $"{Name}<{string.Join(", ", TypeParameters)}>";
        }

        var typeArgNames = TypeArguments!.Select(kv => kv.Value.Name);
        return $"{Name}<{string.Join(", ", typeArgNames)}>";
    }

    public override string ToString()
    {
        return GetFullName();
    }
}

/// <summary>
/// 多态类型家族，支持类型族和关联类型
/// </summary>
public class TypeFamily
{
    private readonly ConcurrentDictionary<string, ITypeInfo> Types = [];
    private readonly ConcurrentDictionary<string, List<ITypeInfo>> InheritanceRelations = [];

    /// <summary>
    /// 注册类型
    /// </summary>
    public void RegisterType(ITypeInfo typeInfo)
    {
        Types.TryAdd(typeInfo.Name, typeInfo);

        // 记录继承关系
        if (typeInfo is ClassTypeInfo { BaseType: not null } classType)
        {
            InheritanceRelations.AddOrUpdate(
                classType.BaseType.Name,
                [classType],
                (_, existing) =>
                {
                    existing.Add(classType);
                    return existing;
                });
        }
    }

    /// <summary>
    /// 获取类型信息
    /// </summary>
    public ITypeInfo? GetType(string typeName)
    {
        return Types.GetValueOrDefault(typeName);
    }

    /// <summary>
    /// 检查类型兼容性（支持多态）
    /// </summary>
    public bool IsCompatible(string sourceTypeName, string targetTypeName)
    {
        var sourceType = GetType(sourceTypeName);
        var targetType = GetType(targetTypeName);

        if (sourceType == null || targetType == null) return false;

        return sourceType.IsCompatibleWith(targetType);
    }

    /// <summary>
    /// 获取类型的所有子类型
    /// </summary>
    public List<ITypeInfo> GetSubTypes(string typeName)
    {
        return InheritanceRelations.TryGetValue(typeName, out var subTypes) ? subTypes : [];
    }

    /// <summary>
    /// 检查是否为多态类型族
    /// </summary>
    public bool IsPolymorphicType(string typeName)
    {
        var type = GetType(typeName);
        return type?.IsClassType == true && GetSubTypes(typeName).Count > 0;
    }
}