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

    private readonly ConcurrentDictionary<string, LangValueType> _cachedMembers = [];

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
        if (_cachedMembers.Count > 0) return _cachedMembers;

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
            _cachedMembers.TryAdd(member.Key, member.Value);
        }

        return _cachedMembers;
    }

    /// <summary>
    /// 清除缓存，当类定义发生变化时调用
    /// </summary>
    public void ClearCache()
    {
        _cachedMembers.Clear();
    }
}

/// <summary>
/// 多态类型家族，支持类型族和关联类型
/// </summary>
public class TypeFamily
{
    private readonly ConcurrentDictionary<string, ITypeInfo> _types = [];
    private readonly ConcurrentDictionary<string, List<ITypeInfo>> _inheritanceRelations = [];

    /// <summary>
    /// 注册类型
    /// </summary>
    public void RegisterType(ITypeInfo typeInfo)
    {
        _types.TryAdd(typeInfo.Name, typeInfo);

        // 记录继承关系
        if (typeInfo is ClassTypeInfo classType && classType.BaseType != null)
        {
            _inheritanceRelations.AddOrUpdate(
                classType.BaseType.Name,
                [classType],
                (key, existing) =>
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
        return _types.TryGetValue(typeName, out var type) ? type : null;
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
        return _inheritanceRelations.TryGetValue(typeName, out var subTypes) ? subTypes : [];
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