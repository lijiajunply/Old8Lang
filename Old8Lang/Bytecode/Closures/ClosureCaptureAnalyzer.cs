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
    private readonly HashSet<string> _namesNeededByNestedClosures = new(StringComparer.Ordinal);

    /// <summary>
    /// 闭包体内被赋值的变量名
    /// </summary>
    /// <remarks>
    /// 其中若某个名字在外层作用域中已存在，说明闭包试图写回外层变量。
    /// 写回由共享单元（<see cref="UpValueCell"/>）支持，调用方据此决定捕获哪些名字。
    /// </remarks>
    public IReadOnlyCollection<string> AssignedVariables => _localVariables;

    /// <summary>
    /// 本函数体内嵌套闭包（lambda 或具名函数）需要从本层取得的名字（含更深层闭包的传递需求）
    /// </summary>
    /// <remarks>
    /// 字节码里嵌套闭包的声明会被提升到兄弟语句之前，因此本层必须在编译函数体之前就把
    /// 这些名字的槽位分配好，闭包才能拿到局部变量下标并按引用捕获。调用方应把这个集合
    /// 与 <see cref="AssignedVariables"/>（本层自己的局部名）求交集后再决定要提前分配谁，
    /// 否则会把嵌套闭包引用的全局名误当成局部变量而遮蔽它。
    /// </remarks>
    public IReadOnlyCollection<string> NamesNeededByNestedClosures => _namesNeededByNestedClosures;

    /// <summary>
    /// 分析函数体，返回捕获的外部变量列表
    /// </summary>
    public List<string> AnalyzeCaptures(BlockStatement functionBody, List<string> parameters)
    {
        _capturedVariables.Clear();
        _localVariables.Clear();
        _parameters.Clear();
        _namesNeededByNestedClosures.Clear();

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
    /// 记录一个嵌套闭包需要从当前层取得的名字
    /// </summary>
    /// <remarks>
    /// 用独立的分析器实例处理嵌套闭包，避免它的局部变量与赋值污染当前层的分析结果；
    /// 递归合并更深层闭包的需求，因为「闭包里的闭包引用了最外层的局部变量」同样要求
    /// 最外层提前分配该变量的槽位。
    /// </remarks>
    private void RecordNestedClosureNeeds(BlockStatement body, List<string> parameters)
    {
        var nested = new ClosureCaptureAnalyzer();
        var nestedFreeVariables = nested.AnalyzeCaptures(body, parameters);

        _namesNeededByNestedClosures.UnionWith(nestedFreeVariables);
        _namesNeededByNestedClosures.UnionWith(nested.AssignedVariables);
        _namesNeededByNestedClosures.UnionWith(nested.NamesNeededByNestedClosures);
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

            // 记录本层需要为它准备的名字（本层局部变量若在其中就要按引用捕获）
            RecordNestedClosureNeeds(funcValue.BlockStatement, nestedParameters);

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

        // 处理块语句
        //
        // BlockStatement 的 Count/索引器只暴露 OtherStatements（普通语句），函数定义与
        // 类定义放在 ImportStatements 里，因此下面通用的子节点遍历看不到嵌套的 func 声明。
        // 这里显式处理 ImportStatements：只记录嵌套函数需要的名字，不下钻（保持既有行为，
        // 避免类定义的方法体污染本层的自由变量集合）。
        if (node is BlockStatement block)
        {
            foreach (var importStatement in block.ImportStatements)
            {
                switch (importStatement)
                {
                    case FuncInit nestedInit:
                        RecordNestedClosureNeeds(
                            nestedInit.FuncValue.BlockStatement,
                            nestedInit.FuncValue.Ids?.Select(id => id.IdName).ToList() ?? []);
                        break;
                    case AsyncFuncInit asyncInit:
                        RecordNestedClosureNeeds(
                            asyncInit.AsyncFuncValue.BlockStatement,
                            asyncInit.AsyncFuncValue.Ids?.Select(id => id.IdName).ToList() ?? []);
                        break;
                }
            }

            foreach (var blockStatement in block.OtherStatements)
            {
                AnalyzeNode(blockStatement);
            }

            return;
        }

        // 处理具名嵌套函数（func 里再声明 func）
        //
        // 它有自己的独立作用域与闭包环境，函数体内的引用不应计入本层的自由变量，
        // 因此不在这里下钻分析；但它需要的名字要记录下来，本层需为其中的局部变量
        // 提前分配槽位，闭包才能按引用捕获（嵌套函数声明在字节码里被提升到兄弟语句之前）。
        if (node is FuncInit funcInit)
        {
            var initValue = funcInit.FuncValue;
            var initParameters = initValue.Ids?.Select(id => id.IdName).ToList() ?? [];
            RecordNestedClosureNeeds(initValue.BlockStatement, initParameters);
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
