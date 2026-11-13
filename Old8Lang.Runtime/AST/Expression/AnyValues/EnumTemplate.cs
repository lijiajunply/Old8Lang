using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.AnyValues;

/// <summary>
/// 枚举模板类，用于存储枚举的定义信息
/// </summary>
public partial class EnumTemplate(
    string enumName,
    Dictionary<string, int> members,
    SourcePosition position = default)
    : ImportInfo(position)
{
    /// <summary>
    /// 枚举名称
    /// </summary>
    public readonly string EnumName = enumName;

    /// <summary>
    /// 枚举成员字典 (成员名 -> 值)
    /// </summary>
    public readonly Dictionary<string, int> Members = members;

    /// <summary>
    /// 获取枚举成员的值
    /// </summary>
    /// <param name="memberName">成员名</param>
    /// <returns>枚举成员值</returns>
    /// <exception cref="NameError">当成员不存在时抛出</exception>
    public EnumLangValue GetMemberValue(string memberName)
    {
        if (Members.TryGetValue(memberName, out var value))
        {
            return EnumLangValue.Create(EnumName, memberName, value, Position);
        }
        throw new NameError(this, $"{EnumName}.{memberName}");
    }

    /// <summary>
    /// 检查是否包含指定成员
    /// </summary>
    /// <param name="memberName">成员名</param>
    /// <returns>是否包含该成员</returns>
    public bool HasMember(string memberName)
    {
        return Members.ContainsKey(memberName);
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>枚举的字符串表示</returns>
    public override string ToString()
    {
        var memberStrings = Members.Select(kvp => $"{kvp.Key} = {kvp.Value}");
        return $"enum {EnumName} {{ {string.Join(", ", memberStrings)} }}";
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        // EnumTemplate 是编译时的元数据，不需要在运行时访问
        // 直接返回默认值
        return default!;
    }
}
