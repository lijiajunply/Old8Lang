namespace Old8Lang.AST.Expression.AnyValues;

/// <summary>
/// 字段定义表
/// 存储类的所有字段定义信息
/// </summary>
public class FieldDefinitionTable
{
    /// <summary>
    /// 字段名到字段定义的映射
    /// </summary>
    private readonly Dictionary<string, FieldDefinition> _fieldMap = new();

    /// <summary>
    /// 所有字段的列表（用于遍历）
    /// </summary>
    private readonly List<FieldDefinition> _allFields = [];

    /// <summary>
    /// 添加字段定义
    /// </summary>
    public void AddField(FieldDefinition fieldDef)
    {
        _fieldMap[fieldDef.FieldName] = fieldDef;
        _allFields.Add(fieldDef);
    }

    /// <summary>
    /// 根据字段名查找字段定义
    /// 复杂度：O(1)
    /// </summary>
    public FieldDefinition? LookupField(string fieldName)
    {
        return _fieldMap.GetValueOrDefault(fieldName);
    }

    /// <summary>
    /// 检查是否包含指定字段
    /// </summary>
    public bool ContainsField(string fieldName)
    {
        return _fieldMap.ContainsKey(fieldName);
    }

    /// <summary>
    /// 获取所有字段
    /// </summary>
    public IReadOnlyList<FieldDefinition> GetAllFields()
    {
        return _allFields.AsReadOnly();
    }

    /// <summary>
    /// 合并另一个字段定义表（用于继承）
    /// </summary>
    /// <param name="parentTable">父类的字段定义表</param>
    public void MergeFrom(FieldDefinitionTable parentTable)
    {
        foreach (var field in parentTable._allFields)
        {
            // 如果当前表中已经有同名字段，跳过（子类字段优先）
            if (_fieldMap.ContainsKey(field.FieldName))
            {
                continue;
            }

            AddField(field);
        }
    }

    /// <summary>
    /// 获取字段数量
    /// </summary>
    public int Count => _allFields.Count;
}

/// <summary>
/// 字段定义
/// 存储字段的元数据和初始值表达式
/// </summary>
public class FieldDefinition(
    string fieldName,
    LangExpression initialValueExpression,
    HashSet<AccessModifierType>? modifiers = null,
    bool isStatic = false,
    string? originClassName = null)
{
    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; } = fieldName;

    /// <summary>
    /// 初始值表达式
    /// </summary>
    public LangExpression InitialValueExpression { get; } = initialValueExpression;

    /// <summary>
    /// 访问修饰符
    /// </summary>
    public HashSet<AccessModifierType> Modifiers { get; } = modifiers ?? [];

    /// <summary>
    /// 是否为静态字段
    /// </summary>
    public bool IsStatic { get; } = isStatic;

    /// <summary>
    /// 字段来源类名（用于追踪继承）
    /// </summary>
    public string? OriginClassName { get; } = originClassName;

    /// <summary>
    /// 检查是否有指定修饰符
    /// </summary>
    public bool HasModifier(AccessModifierType modifier)
    {
        return Modifiers.Contains(modifier);
    }

    /// <summary>
    /// 检查访问权限
    /// </summary>
    public bool IsAccessibleFrom(bool isInternalAccess)
    {
        bool isPrivate = HasModifier(AccessModifierType.Private);
        bool isProtected = HasModifier(AccessModifierType.Protected);

        // 私有字段只能在类内部访问
        if (isPrivate && !isInternalAccess)
            return false;

        // 受保护字段只能在类内部访问
        if (isProtected && !isInternalAccess)
            return false;

        // 公开字段都可以访问
        return true;
    }
}