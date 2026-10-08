using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser;

namespace Old8Lang.Bytecode.Closures;

/// <summary>
/// 闭包变量捕获分析器 - 分析 Lambda 函数中引用的外部变量
/// </summary>
public class ClosureCaptureAnalyzer
{
    private readonly HashSet<string> _capturedVariables = new();
    private readonly HashSet<string> _localVariables = new();
    private readonly HashSet<string> _parameters = new();

    /// <summary>
    /// 闭包体内被赋值的变量名
    /// </summary>
    /// <remarks>
    /// 其中若某个名字在外层作用域中已存在，说明闭包试图写回外层变量。
    /// 字节码模式的闭包按值快照捕获，没有共享单元，无法支持这种写回，
    /// 调用方需要据此报错而不是静默地让它在闭包内另建一个同名局部变量。
    /// </remarks>
    public IReadOnlyCollection<string> AssignedVariables => _localVariables;

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

    /// <summary>
    /// 递归分析调用的实参（位置参数与命名参数）
    /// </summary>
    private void AnalyzeArguments(List<LangExpression> arguments, List<NamedArgument>? namedArguments)
    {
        foreach (var argument in arguments)
        {
            AnalyzeNode(argument);
        }

        if (namedArguments is null)
        {
            return;
        }

        foreach (var namedArgument in namedArguments)
        {
            AnalyzeNode(namedArgument.Value);
        }
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

        // 处理 IfStatement（条件语句）
        if (node is IfStatement ifStmt)
        {
            // 分析所有 if/elif 块
            foreach (var child in ifStmt.Children)
            {
                AnalyzeNode(child);
            }
            // 分析 else 块
            if (ifStmt.ElseBlock != null)
            {
                AnalyzeNode(ifStmt.ElseBlock);
            }
            return;
        }

        // 处理 IfChild（if/elif 块）
        if (node is IfChild ifChild)
        {
            // 分析条件表达式
            AnalyzeNode(ifChild.Condition);
            // 分析块语句
            AnalyzeNode(ifChild.Block);
            return;
        }

        // 处理 LangId（变量引用）
        if (node is LangId id)
        {
            _capturedVariables.Add(id.IdName);
            return;
        }

        // 处理成员访问与方法调用：obj.member、obj.method(args)
        //
        // 点号右侧是成员名（字段名或方法名），不是变量引用。若把它当成变量收集，
        // 闭包就会为一个并不存在的变量（例如 ToUpper）建立捕获项，闭包创建时即报
        // “名称未定义”。因此这里只递归左操作数；右操作数是方法调用时，额外递归实参。
        if (node is Operation { Opera: LangTokenType.Dot } dotExpression)
        {
            AnalyzeNode(dotExpression.Left);

            if (dotExpression.Right is Instance methodCall)
            {
                AnalyzeArguments(methodCall.Ids, methodCall.NamedArgs);
            }

            return;
        }

        // 处理标识符调用：f(x)。
        //
        // 被调用名本身要作为变量引用收集：装饰器包装函数（形如
        // func deco(f) { return (x) -> f(x) * 2 }）正是靠捕获形参 f
        // 才拿得到被装饰的函数。名字是否真的需要捕获由调用方结合作用域判断。
        if (node is Instance instance)
        {
            AnalyzeNode(instance.Id);
            AnalyzeArguments(instance.Ids, instance.NamedArgs);
            return;
        }

        // 处理复杂表达式调用：expressions(x, y)
        if (node is FunctionCallExpression functionCall)
        {
            AnalyzeNode(functionCall.FunctionExpression);
            AnalyzeArguments(functionCall.Arguments, functionCall.NamedArguments);
            return;
        }

        // 处理 Operation（二元操作）
        if (node is Operation operation)
        {
            AnalyzeNode(operation.Left);
            AnalyzeNode(operation.Right);
            return;
        }

        // 处理 FuncLangValue（Lambda 表达式）
        if (node is AST.Expression.Value.FuncLangValue funcValue)
        {
            // 对于嵌套 Lambda，需要分析其函数体中引用的变量
            // 但要排除 Lambda 自己的参数
            var nestedParameters = funcValue.Ids?.Select(id => id.IdName).ToList() ?? new List<string>();

            // 嵌套闭包内部的赋值属于嵌套闭包自身，不应算作当前闭包的赋值，
            // 否则会把 "只有内层闭包才写回外层变量" 的情况误判到当前层。
            // 嵌套闭包在编译到它自己时会被单独分析，因此这里做快照隔离。
            var outerAssignedSnapshot = new HashSet<string>(_localVariables, StringComparer.Ordinal);

            // 将嵌套 Lambda 的参数临时添加到局部变量集合（避免被捕获）
            foreach (var param in nestedParameters)
            {
                _localVariables.Add(param);
            }

            // 分析嵌套 Lambda 的函数体
            AnalyzeNode(funcValue.BlockStatement);

            // 内层闭包自己声明的名字（形参、以及它内部赋值建立的局部变量）只属于它自己。
            // 若把它们留在当前闭包的捕获集合里，外层闭包就会凭空多出一个同名局部变量：
            // 内层闭包随后在“闭包写外层局部变量”的检查里看到这个名字，会误判成写回外层变量
            // 而直接报错（典型触发：内层闭包里的 result <- f(x)，外层恰有同名全局/局部）。
            var nestedDeclared = new HashSet<string>(_localVariables, StringComparer.Ordinal);
            nestedDeclared.ExceptWith(outerAssignedSnapshot);
            _capturedVariables.ExceptWith(nestedDeclared);

            // 恢复赋值集合：嵌套闭包的赋值不回传给当前闭包
            _localVariables.Clear();
            _localVariables.UnionWith(outerAssignedSnapshot);

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
