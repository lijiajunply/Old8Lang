using Old8Lang.Error;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 访问修饰符类型
/// </summary>
public enum AccessModifierType
{
    Public,
    Private,
    Protected,
    Static,
    Abstract
}

/// <summary>
/// 类成员ID，扩展LangId，支持访问修饰符
/// </summary>
public class ClassMemberId : LangId
{
    
    /// <summary>
    /// 互斥的修饰符组
    /// </summary>
    private static readonly List<HashSet<AccessModifierType>> MutuallyExclusiveModifiers =
    [
        [AccessModifierType.Public, AccessModifierType.Private, AccessModifierType.Protected]
    ];

    /// <summary>
    /// 访问修饰符集合
    /// </summary>
    public readonly HashSet<AccessModifierType> Modifiers;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">原始LangId</param>
    /// <param name="modifiers">访问修饰符列表</param>
    public ClassMemberId(LangId id, IEnumerable<AccessModifierType>? modifiers = null)
        : base(id.IdName, id.AssumptionType, position: id.Position)
    {
        Modifiers = modifiers != null ? [..modifiers] : [];
        ValidateModifiers();
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">成员名称</param>
    /// <param name="assumptionType">类型假设</param>
    /// <param name="modifiers">访问修饰符列表</param>
    public ClassMemberId(string name, string assumptionType = "", IEnumerable<AccessModifierType>? modifiers = null)
        : base(name, assumptionType)
    {
        Modifiers = modifiers != null ? [..modifiers] : [];
        ValidateModifiers();
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name"></param>
    /// <param name="assumptionType"></param>
    /// <param name="modifiers">访问修饰符列表</param>
    /// <param name="position">位置信息</param>
    public ClassMemberId(string name, string assumptionType, IEnumerable<AccessModifierType>? modifiers,
        SourcePosition position)
        : base(name, assumptionType, position: position)
    {
        Modifiers = modifiers != null ? [..modifiers] : [];
        ValidateModifiers();
    }

    /// <summary>
    /// 验证修饰符是否合法
    /// </summary>
    /// <exception cref="SyntaxError">当修饰符互斥时抛出</exception>
    private void ValidateModifiers()
    {
        // 检查互斥修饰符
        foreach (var conflictingModifiers in from exclusiveGroup in MutuallyExclusiveModifiers
                 let count = Modifiers.Count(exclusiveGroup.Contains)
                 where count > 1
                 select string.Join(", ", Modifiers.Where(exclusiveGroup.Contains)))
        {
            throw new SyntaxError(Position, $"修饰符 {conflictingModifiers} 互斥，不能同时使用");
        }
    }

    /// <summary>
    /// 检查是否有指定修饰符
    /// </summary>
    /// <param name="modifier">要检查的修饰符</param>
    /// <returns>是否有指定修饰符</returns>
    public bool HasModifier(AccessModifierType modifier)
    {
        return Modifiers.Contains(modifier);
    }

    /// <summary>
    /// 添加修饰符
    /// </summary>
    /// <param name="modifier">要添加的修饰符</param>
    /// <exception cref="SyntaxError">当修饰符互斥时抛出</exception>
    public void AddModifier(AccessModifierType modifier)
    {
        var newModifiers = new HashSet<AccessModifierType>(Modifiers) { modifier };
        // 检查互斥修饰符
        if (MutuallyExclusiveModifiers.Select(exclusiveGroup => newModifiers.Count(m => exclusiveGroup.Contains(m)))
            .Any(count => count > 1))
        {
            throw new SyntaxError(Position, $"修饰符 {modifier} 与已有的修饰符互斥");
        }

        Modifiers.Add(modifier);
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        var modifierList = new List<string>();
        if (HasModifier(AccessModifierType.Static)) modifierList.Add("static");
        if (HasModifier(AccessModifierType.Public)) modifierList.Add("public");
        if (HasModifier(AccessModifierType.Private)) modifierList.Add("private");
        if (HasModifier(AccessModifierType.Protected)) modifierList.Add("protected");

        var modifiers = modifierList.Count != 0 ? string.Join(" ", modifierList) + " " : string.Empty;
        return $"{modifiers}{base.ToString()}";
    }
}