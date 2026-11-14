using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Visitor;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 函数声明语句：承载函数的纯语法模型，运行时值由 <c>FuncInitRuntimeExtensions</c> 按需创建。
/// </summary>
/// <param name="syntax">函数语法模型</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
public partial class FuncInit(FunctionSyntax syntax, SourcePosition position = default) : OldStatement(position), IBlockDeclaration
{
    /// <summary>函数语法模型（函数名、参数、函数体、泛型、装饰器与文档注释）。</summary>
    public readonly FunctionSyntax Syntax = syntax;

    /// <summary>函数名与返回类型注解。</summary>
    public LangId? Id => Syntax.Id;

    /// <summary>参数列表。</summary>
    public List<LangId> Parameters => Syntax.Parameters;

    /// <summary>函数体。</summary>
    public BlockStatement Body => Syntax.Body;

    /// <summary>检查函数是否为 Lambda 表达式（通过检查 Id 是否为 null）。</summary>
    public bool IsLambda => Syntax.Id is null;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitFuncInit(this);

    /// <summary>获取指定索引处的语句（实现OldStatement接口）。</summary>
    public override OldStatement this[int index] => this;

    /// <summary>获取语句数量（实现OldStatement接口）。</summary>
    public override int Count => 0;

    /// <summary>将函数初始化转换为字符串表示。</summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        var paramList = string.Join(", ", Syntax.Parameters);
        sb.AppendLine($"func {Syntax.Id}({paramList})");
        sb.AppendLine($"{{ {Syntax.Body} }}");
        return sb.ToString();
    }

    /// <summary>
    /// 验证 params 参数的合法性
    /// </summary>
    /// <exception cref="SyntaxError">当 params 参数使用不当时抛出</exception>
    public void ValidateParamsParameter()
    {
        var ids = Syntax.Parameters;
        if (ids.Count == 0)
        {
            return;
        }

        // 查找所有 params 参数
        var paramsIndices = new List<int>();
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i].IsParams)
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
            var paramsParam = ids[paramsIndices[1]];
            throw new SyntaxError(
                paramsParam.Position,
                $"函数 '{Syntax.Id?.IdName}' 只能有一个 params 参数，但发现了 {paramsIndices.Count} 个");
        }

        var paramsIndex = paramsIndices[0];
        var paramsId = ids[paramsIndex];

        // 规则2: params 参数必须是最后一个参数
        if (paramsIndex != ids.Count - 1)
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{Syntax.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 必须是参数列表的最后一个参数");
        }

        // 规则3: params 参数必须有类型注解，且必须是数组类型
        if (string.IsNullOrEmpty(paramsId.AssumptionType))
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{Syntax.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 必须有类型注解，且必须是数组类型（例如 array<int>）");
        }

        // 检查是否是数组类型
        var typeAnnotation = paramsId.AssumptionType.ToLower().Trim();
        if (!typeAnnotation.StartsWith("array<") || !typeAnnotation.EndsWith(">"))
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{Syntax.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 的类型必须是数组类型（例如 array<int>），但当前类型为 '{paramsId.AssumptionType}'");
        }

        // 规则4: params 参数不能有默认值
        if (paramsId.DefaultValue is not null)
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{Syntax.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 不能有默认值，params 参数会自动处理为空数组");
        }
    }
}
