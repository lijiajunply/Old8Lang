using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 函数初始化类，用于处理Old8Lang中的函数声明
/// </summary>
/// <param name="a">函数值对象</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
public partial class FuncInit(FuncLangValue a, SourcePosition position = default) : OldStatement(position), IBlockDeclaration
{
    /// <summary>
    /// 函数值对象，包含函数的完整定义
    /// </summary>
    public readonly FuncLangValue FuncValue = a;

    /// <summary>
    /// 检查函数是否为Lambda表达式（通过检查Id是否为null）
    /// </summary>
    public bool IsLambda => FuncValue.Id is null;







    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为FuncInit是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为FuncInit是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将函数初始化转换为字符串表示
    /// </summary>
    /// <returns>函数初始化的字符串表示</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        var paramList = FuncValue.Ids is not null ? string.Join(", ", FuncValue.Ids) : string.Empty;
        sb.AppendLine($"func {FuncValue.Id}({paramList})");
        sb.AppendLine($"{{ {FuncValue.BlockStatement} }}");
        return sb.ToString();
    }

    /// <summary>
    /// 验证 params 参数的合法性
    /// </summary>
    /// <exception cref="SyntaxError">当 params 参数使用不当时抛出</exception>
    internal void ValidateParamsParameter()
    {
        if (FuncValue.Ids is null || FuncValue.Ids.Count == 0)
        {
            return;
        }

        // 查找所有 params 参数
        var paramsIndices = new List<int>();
        for (int i = 0; i < FuncValue.Ids.Count; i++)
        {
            if (FuncValue.Ids[i].IsParams)
            {
                paramsIndices.Add(i);
            }
        }

        // 如果没有 params 参数，直接返回
        if (paramsIndices.Count == 0)
        {
            return;
        }

        // 规则1: 只能有一个 params 参数
        if (paramsIndices.Count > 1)
        {
            var paramsParam = FuncValue.Ids[paramsIndices[1]];
            throw new SyntaxError(
                paramsParam.Position,
                $"函数 '{FuncValue.Id?.IdName}' 只能有一个 params 参数，但发现了 {paramsIndices.Count} 个");
        }

        var paramsIndex = paramsIndices[0];
        var paramsId = FuncValue.Ids[paramsIndex];

        // 规则2: params 参数必须是最后一个参数
        if (paramsIndex != FuncValue.Ids.Count - 1)
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{FuncValue.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 必须是参数列表的最后一个参数");
        }

        // 规则3: params 参数必须有类型注解，且必须是数组类型
        if (string.IsNullOrEmpty(paramsId.AssumptionType))
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{FuncValue.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 必须有类型注解，且必须是数组类型（例如 array<int>）");
        }

        // 检查是否是数组类型
        var typeAnnotation = paramsId.AssumptionType.ToLower().Trim();
        if (!typeAnnotation.StartsWith("array<") || !typeAnnotation.EndsWith(">"))
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{FuncValue.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 的类型必须是数组类型（例如 array<int>），但当前类型为 '{paramsId.AssumptionType}'");
        }

        // 规则4: params 参数不能有默认值
        if (paramsId.DefaultValue is not null)
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{FuncValue.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 不能有默认值，params 参数会自动处理为空数组");
        }
    }
}