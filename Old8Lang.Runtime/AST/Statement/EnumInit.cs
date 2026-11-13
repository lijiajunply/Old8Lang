using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 枚举定义语句，用于处理Old8Lang中的枚举声明
/// </summary>
/// <param name="enumName">枚举名称</param>
/// <param name="members">枚举成员列表</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
public partial class EnumInit(
    string enumName,
    List<(string name, LangExpression? value)> members,
    SourcePosition position = default) : OldStatement(position)
{

    /// <summary>
    /// 枚举名称
    /// </summary>
    internal readonly string _enumName = enumName;

    /// <summary>
    /// 枚举成员列表（成员名和可选的显式值）
    /// </summary>
    internal readonly List<(string name, LangExpression? value)> _members = members;

    /// <summary>
    /// 公共属性，用于访问枚举名称
    /// </summary>
    public string EnumName => _enumName;

    /// <summary>
    /// 公共属性，用于访问枚举成员
    /// </summary>
    public List<(string name, LangExpression? value)> Members => _members;

    /// <summary>
    /// 获取指定索引处的语句
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量
    /// </summary>
    /// <returns>返回0，因为EnumInit是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将枚举定义转换为字符串表示
    /// </summary>
    /// <returns>枚举定义的字符串表示</returns>
    public override string ToString()
    {
        var memberStrings = _members.Select(m =>
        {
            if (m.value is not null)
            {
                return $"{m.name} = {m.value}";
            }

            return m.name;
        });
        return $"enum {_enumName} {{ {string.Join(", ", memberStrings)} }}";
    }
}