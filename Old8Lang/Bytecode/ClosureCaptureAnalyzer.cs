using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;

namespace Old8Lang.Bytecode;

/// <summary>
/// 闭包变量捕获分析器 - 分析 Lambda 函数中引用的外部变量
/// </summary>
public class ClosureCaptureAnalyzer
{
    private readonly HashSet<string> _capturedVariables = new();
    private readonly HashSet<string> _localVariables = new();
    private readonly HashSet<string> _parameters = new();

    /// <summary>
    /// 分析函数体，返回捕获的外部变量列表
    /// </summary>
    public List<string> AnalyzeCaptures(BlockStatement functionBody, List<string> parameters)
    {
        _capturedVariables.Clear();
        _localVariables.Clear();
        _parameters.Clear();

        // 记录参数
        foreach (var param in parameters)
        {
            _parameters.Add(param);
        }

        // 分析函数体
        AnalyzeNode(functionBody);

        // 返回捕获的变量（排除参数和局部变量）
        return _capturedVariables
            .Where(v => !_parameters.Contains(v) && !_localVariables.Contains(v))
            .ToList();
    }

    private void AnalyzeNode(IOldLangTree? node)
    {
        if (node == null) return;

        // 处理 SetStatement（变量声明和赋值）
        if (node is SetStatement setStmt)
        {
            // 记录局部变量声明
            if (setStmt.Id != null)
            {
                _localVariables.Add(setStmt.Id.IdName);
            }
            // 分析右侧表达式
            AnalyzeNode(setStmt.Value);
            return;
        }

        // 处理 ReturnStatement（返回语句）
        if (node is ReturnStatement returnStmt)
        {
            // 分析返回值表达式
            AnalyzeNode(returnStmt.Expression);
            return;
        }

        // 处理 LangId（变量引用）
        if (node is LangId id)
        {
            _capturedVariables.Add(id.IdName);
            return;
        }

        // 处理 Operation（二元操作）
        if (node is Operation operation)
        {
            AnalyzeNode(operation.Left);
            AnalyzeNode(operation.Right);
            return;
        }

        // 对于其他节点类型，遍历子节点
        if (node is OldStatement statement)
        {
            for (int i = 0; i < statement.Count; i++)
            {
                AnalyzeNode(statement[i]);
            }
        }
    }
}
