using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.AnyValues;

/// <summary>
/// 类型元数据 - 存储类定义信息（类似C#的Type）
/// 与实例数据完全分离，所有实例共享同一份元数据
/// </summary>
public class ClassMetadata(
    string className,
    string? parentClassName = null,
    List<string>? interfaceNames = null,
    List<string>? mixinNames = null,
    bool isInterface = false,
    bool isAbstract = false,
    bool isMixin = false)
{
    /// <summary>
    /// 类名
    /// </summary>
    public string ClassName { get; } = className;

    /// <summary>
    /// 父类名称（单一继承）
    /// </summary>
    public string? ParentClassName { get; } = parentClassName;

    /// <summary>
    /// 接口名称列表
    /// </summary>
    public List<string> InterfaceNames { get; } = interfaceNames ?? [];

    /// <summary>
    /// Mixin 名称列表
    /// </summary>
    public List<string> MixinNames { get; } = mixinNames ?? [];

    /// <summary>
    /// 方法查找表（类似 C# 的 VTable）
    /// 预计算所有方法的查找信息，实现 O(1) 查找
    /// </summary>
    public MethodTable MethodTable { get; } = new();

    /// <summary>
    /// 字段定义表
    /// 存储所有实例字段的定义信息
    /// </summary>
    public FieldDefinitionTable FieldTable { get; } = new();

    /// <summary>
    /// 静态成员存储
    /// 所有实例共享的静态数据
    /// </summary>
    public Dictionary<string, LangValueType> StaticMembers { get; } = new();

    /// <summary>
    /// 类是否为接口
    /// </summary>
    public bool IsInterface { get; } = isInterface;

    /// <summary>
    /// 类是否为抽象类
    /// </summary>
    public bool IsAbstract { get; } = isAbstract;

    /// <summary>
    /// 类是否为 Mixin
    /// </summary>
    public bool IsMixin { get; } = isMixin;

    /// <summary>
    /// 继承深度缓存
    /// 用于优化继承链遍历
    /// </summary>
    private int? InheritanceDepth;

    /// <summary>
    /// 获取继承深度（0表示无父类，1表示直接继承，以此类推）
    /// </summary>
    public int GetInheritanceDepth(VariateManager manager)
    {
        if (InheritanceDepth.HasValue)
            return InheritanceDepth.Value;

        if (ParentClassName is null)
        {
            InheritanceDepth = 0;
            return 0;
        }

        // 递归计算继承深度
        if (manager.GetAny(new LangId(ParentClassName)) is TypeTemplate parentTemplate &&
            parentTemplate.Metadata is not null)
        {
            InheritanceDepth = 1 + parentTemplate.Metadata.GetInheritanceDepth(manager);
            return InheritanceDepth.Value;
        }

        InheritanceDepth = 0;
        return 0;
    }

    /// <summary>
    /// 判断是否可以赋值给指定类型（继承关系检查）
    /// </summary>
    public bool IsAssignableTo(ClassMetadata targetType, VariateManager manager)
    {
        // 同一类型
        if (ClassName == targetType.ClassName)
            return true;

        // 检查父类
        if (ParentClassName is not null)
        {
            if (ParentClassName == targetType.ClassName)
                return true;

            // 递归检查祖先类
            if (manager.GetAny(new LangId(ParentClassName)) is TypeTemplate parentTemplate &&
                parentTemplate.Metadata is not null)
            {
                return parentTemplate.Metadata.IsAssignableTo(targetType, manager);
            }
        }

        // 检查接口
        if (InterfaceNames.Contains(targetType.ClassName))
            return true;

        // 检查 Mixin
        if (MixinNames.Contains(targetType.ClassName))
            return true;

        return false;
    }
}