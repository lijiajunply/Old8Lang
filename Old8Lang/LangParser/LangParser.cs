using Old8Lang.AST;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser.Core;
using Old8Lang.LangParser.Parsers;

namespace Old8Lang.LangParser;

/// <summary>
/// LangParser 门面类，负责将标记流转换为抽象语法树(AST)
/// </summary>
/// <remarks>
/// 该类采用门面设计模式，作为所有解析器的统一入口，保持向后兼容性。
/// 内部委托给专门的解析器类处理不同类型的语法结构，包括：
/// - 语句解析(StatementParser)
/// - 表达式解析(ExpressionParser)
/// - 主表达式解析(PrimaryParser)
/// - 函数解析(FunctionParser)
/// - 类解析(ClassParser)
/// </remarks>
public class LangParser
{
    /// <summary>
    /// 解析器共享上下文，包含标记列表、源代码和文件名等信息
    /// </summary>
    private readonly ParserContext _context;
    
    /// <summary>
    /// 语句解析器，负责解析各种语句结构
    /// </summary>
    private readonly StatementParser _statementParser;
    
    /// <summary>
    /// 表达式解析器，负责解析各种表达式
    /// </summary>
    private readonly ExpressionParser _expressionParser;

    /// <summary>
    /// 构造函数，初始化所有解析器并解决它们之间的循环依赖
    /// </summary>
    /// <param name="tokens">词法分析生成的标记列表</param>
    /// <param name="sourceCode">原始源代码（用于错误信息生成）</param>
    /// <param name="fileName">源代码文件名（用于错误信息生成）</param>
    public LangParser(List<LangToken> tokens, string? sourceCode = null, string? fileName = null)
        : this(tokens, [], sourceCode, fileName)
    {
    }

    /// <summary>
    /// 构造函数（带文件头指令），初始化所有解析器并解决它们之间的循环依赖
    /// </summary>
    /// <param name="tokens">词法分析生成的标记列表</param>
    /// <param name="headerDirectiveTokens">文件头指令 token 列表</param>
    /// <param name="sourceCode">原始源代码（用于错误信息生成）</param>
    /// <param name="fileName">源代码文件名（用于错误信息生成）</param>
    public LangParser(List<LangToken> tokens, List<LangToken> headerDirectiveTokens, string? sourceCode = null, string? fileName = null)
    {
        // 1. 创建共享上下文，供所有解析器使用
        _context = new ParserContext(tokens, sourceCode, fileName);

        // 解析文件头指令
        foreach (var directiveToken in headerDirectiveTokens)
        {
            if (directiveToken.Type == LangTokenType.FileHeaderDirective)
            {
                // Token的Value格式为 "directiveName:directiveValue"
                var parts = directiveToken.Value.Split(':', 2);
                if (parts.Length == 2)
                {
                    var directive = new FileHeaderDirective(parts[0], parts[1], directiveToken.Line);
                    _context.HeaderDirectives.AddDirective(directive);
                }
            }
        }

        // 2. 按特定顺序创建各个解析器，解决它们之间的循环依赖
        // 解析器之间的依赖关系：
        // - StatementParser 依赖 ExpressionParser, FunctionParser, ClassParser, PrimaryParser
        // - ExpressionParser 依赖 PrimaryParser
        // - FunctionParser 依赖 StatementParser, ExpressionParser
        // - ClassParser 依赖 StatementParser
        // - PrimaryParser 依赖 StatementParser, ExpressionParser, FunctionParser
        // 使用延迟加载（Lambda表达式）解决循环依赖问题
        // 首先创建FunctionParser（需要延迟加载StatementParser和ExpressionParser）
        var functionParser = new FunctionParser(
            _context,
            () => _statementParser!,
            () => _expressionParser!);

        // 创建LinqParser（需要延迟加载ExpressionParser）
        var linqParser = new LinqParser(
            _context,
            () => _expressionParser!);

        // 创建PrimaryParser（需要延迟加载StatementParser和ExpressionParser）
        var primaryParser = new PrimaryParser(
            _context,
            () => _statementParser!,
            () => _expressionParser!,
            functionParser,
            linqParser);

        // 创建ExpressionParser（仅依赖PrimaryParser）
        _expressionParser = new ExpressionParser(_context, primaryParser, functionParser);

        // 创建ClassParser（需要延迟加载StatementParser、ExpressionParser和FunctionParser）
        var classParser = new ClassParser(
            _context,
            () => _statementParser!,
            () => _expressionParser!,
            () => functionParser!);

        // 创建ExtensionParser（需要延迟加载FunctionParser和StatementParser）
        var extensionParser = new ExtensionParser(
            _context,
            () => functionParser!,
            () => _statementParser!);

        // 最后创建StatementParser（依赖所有其他解析器）
        _statementParser = new StatementParser(
            _context,
            _expressionParser,
            functionParser,
            classParser,
            primaryParser,
            extensionParser);
    }

    /// <summary>
    /// 解析程序的入口方法，将整个程序转换为抽象语法树
    /// </summary>
    /// <returns>表示整个程序的块语句(BlockStatement)</returns>
    /// <exception cref="SyntaxError">当解析过程中遇到语法错误时抛出</exception>
    /// <remarks>
    /// 语法规则：root = statement* ;
    /// 该方法会跳过空语句（单独的分号），并处理解析过程中可能出现的各种异常，
    /// 为异常添加上下文信息，使错误提示更加友好和有用。
    /// </remarks>
    public BlockStatement ParseProgram()
    {
        var statements = new List<IOldLangTree>();

        try
        {
            // 循环解析所有语句，直到到达标记流末尾
            while (_context.CurrentIndex < _context.Tokens.Count)
            {
                // 跳过开头的分号（空语句）
                while (_context.CurrentToken.Type == LangTokenType.Semicolon)
                {
                    _context.CurrentIndex++;
                }

                // 跳过文档注释 tokens（它们会在 CollectPrecedingDocComments 中被处理）
                while (_context.CurrentIndex < _context.Tokens.Count &&
                       _context.CurrentToken.Type == LangTokenType.DocComment)
                {
                    _context.CurrentIndex++;
                }

                // 如果跳过分号和文档注释后到达文件末尾，退出循环
                if (_context.CurrentIndex >= _context.Tokens.Count)
                {
                    break;
                }

                // 解析一条语句并添加到语句列表中
                statements.Add(_statementParser.ParseStatement());

                // 跳过语句后的可选分号分隔符
                while (_context.CurrentToken.Type == LangTokenType.Semicolon)
                {
                    _context.CurrentIndex++;
                }
            }

            // 返回包含所有语句的块语句
            return new BlockStatement(statements);
        }
        catch (SyntaxError)
        {
            // 直接返回原始语法错误，不再重新包装
            throw;
        }
        catch (Exception ex)
        {
            // 处理其他类型的异常，转换为SyntaxError并添加上下文信息
            var currentToken = _context.CurrentToken;

            // 检查当前标记是否有效
            var tokenValue = currentToken.Type == LangTokenType.EndOfFile ? "<unknown>" : currentToken.Value;
            var line = currentToken.Line;
            var column = currentToken.Column;

            // 获取错误位置附近的源代码上下文
            string[] context;
            try
            {
                context = GetSourceContext(line);
            }
            catch
            {
                // 如果获取上下文失败，使用空数组
                context = [];
            }

            if (ex is Old8Exception old8Ex)
            {
                // 如果已经是Old8Exception，添加上下文信息后重新抛出
                throw new SyntaxError(
                    tokenValue,
                    line,
                    column,
                    _context.FileName,
                    $"解析错误：{old8Ex.Message}",
                    context);
            }

            // 其他类型的异常，转换为SyntaxError并添加上下文信息
            throw new SyntaxError(
                tokenValue,
                line,
                column,
                _context.FileName,
                $"解析时出现代码错误：{ex.Message}",
                context);
        }
    }

    /// <summary>
    /// 获取错误位置附近的源代码上下文，用于生成友好的错误信息
    /// </summary>
    /// <param name="line">错误发生的行号</param>
    /// <returns>错误位置前后的源代码行数组（最多5行）</returns>
    private string[] GetSourceContext(int line)
    {
        // 使用上下文对象中缓存的分割结果，提高性能
        var lines = _context.SourceLines;

        if (lines.Length == 0)
        {
            return [];
        }

        // 预分配容量，提高性能
        var contextLines = new List<string>(4);

        // 确保行号有效，避免负数行号导致的问题
        var safeLine = Math.Max(0, line);
        
        // 获取错误行前后各2行，最多显示5行上下文
        var startLine = Math.Max(0, safeLine - 2);
        var endLine = Math.Min(lines.Length - 1, safeLine + 1);

        // 收集上下文行
        for (var i = startLine; i <= endLine; i++)
        {
            contextLines.Add(lines[i]);
        }

        return contextLines.ToArray();
    }

    /// <summary>
    /// 获取文件头指令集合
    /// </summary>
    /// <returns>文件头指令集合</returns>
    public FileHeaderDirectives GetHeaderDirectives()
    {
        return _context.HeaderDirectives;
    }
}