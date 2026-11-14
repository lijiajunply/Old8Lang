using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Runtime;

/// <summary>
/// 把 lambda 语法节点适配为既有运行时可调用值（阶段 2 语法/运行时分界）。
/// </summary>
internal static class LambdaExpressionRuntimeFactory
{
    /// <summary>
    /// 创建与旧解析器输出等价的函数值节点；闭包捕获仍由各 Visitor 在求值时处理。
    /// </summary>
    public static LangValueType Create(LambdaExpression node) =>
        node.IsAsync
            ? new AsyncFuncLangValue(node.Id, node.Parameters, node.Body, node.Position)
            : new FuncLangValue(node.Id, node.Parameters, node.Body, null, node.Position, isLambda: true);
}
