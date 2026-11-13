using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Compiler.Specialization;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 语言标识符类，用于表示变量名、函数名、类名等
/// </summary>
/// <param name="name">标识符名称</param>
/// <param name="assumptionType">类型注解，用于类型检查和推断</param>
/// <param name="defaultValue">默认值表达式</param>
/// <param name="isParams">是否为 params 可变参数</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <remarks>
/// 该类是Old8Lang表达式系统的基础组件，用于表示各种标识符。
/// 支持类型注解、默认值、params 可变参数等功能。
/// 注意：this 关键字现在由独立的 ThisExpression 类处理。
/// </remarks>
public partial class LangId(
    string name,
    string assumptionType = "",
    LangExpression? defaultValue = null,
    bool isParams = false,
    SourcePosition position = default) : LangExpression(position)
{

    /// <summary>
    /// 标识符名称
    /// </summary>
    public readonly string IdName = name;

    /// <summary>
    /// 类型注解，用于类型检查和推断
    /// </summary>
    public string AssumptionType { get; } = assumptionType;

    /// <summary>
    /// 默认值表达式
    /// </summary>
    public LangExpression? DefaultValue { get; } = defaultValue;

    /// <summary>
    /// 是否为 params 可变参数
    /// </summary>
    public bool IsParams { get; } = isParams;

    /// <summary>
    /// 将标识符转换为字符串表示
    /// </summary>
    /// <returns>标识符名称</returns>
    public override string ToString() => IdName;

    /// <summary>
    /// 比较两个LangId是否相等
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns>如果相等则返回true，否则返回false</returns>
    public override bool Equals(object? obj)
    {
        var a = obj as LangId;
        return a?.IdName == IdName;
    }

    /// <summary>
    /// 获取标识符的哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return IdName.GetHashCode();
    }

    /// <summary>
    /// 在已编译的委托表中查找具名函数（支持 <c>名称$参数类型</c> 形式的带签名键）。
    /// </summary>
    /// <remarks>
    /// 如果同名函数存在多个重载，返回 false，避免静默选中错误的重载。
    /// </remarks>
    internal static bool TryGetFunctionMethod(string name, LocalManager local, out MethodInfo method)
    {
        if (local.DelegateVar.TryGetValue(name, out var exactMatch))
        {
            method = exactMatch;
            return true;
        }

        MethodInfo? found = null;
        var prefix = $"{name}$";
        foreach (var (key, value) in local.DelegateVar)
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (found is not null)
            {
                // 多个重载：无法在这里确定签名
                method = null!;
                return false;
            }

            found = value;
        }

        method = found!;
        return found is not null;
    }

}
