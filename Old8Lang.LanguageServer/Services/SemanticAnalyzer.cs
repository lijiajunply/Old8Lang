using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 语义分析器 - 检测语义错误
/// </summary>
public class SemanticAnalyzer(DocumentParseResult document)
{
    private readonly List<DiagnosticInfo> _diagnostics = [];
    private readonly HashSet<string> _definedSymbols = [];

    /// <summary>
    /// 执行语义分析
    /// </summary>
    public List<DiagnosticInfo> Analyze()
    {
        _diagnostics.Clear();
        _definedSymbols.Clear();

        if (document.Ast == null || document.Tokens == null)
        {
            return _diagnostics;
        }

        // 收集已定义的符号
        CollectDefinedSymbols();

        // 检查未定义的符号引用
        CheckUndefinedSymbols();

        return _diagnostics;
    }

    /// <summary>
    /// 收集已定义的符号（从符号表）
    /// </summary>
    private void CollectDefinedSymbols()
    {
        if (document.SymbolTable == null)
        {
            return;
        }

        foreach (var symbol in document.SymbolTable.Keys)
        {
            _definedSymbols.Add(symbol);
        }
    }

    /// <summary>
    /// 检查未定义的符号
    /// </summary>
    private void CheckUndefinedSymbols()
    {
        if (document.Tokens == null)
        {
            return;
        }

        // 内置函数列表
        var builtInFunctions = new HashSet<string>
        {
            "PrintLine", "Print", "Input", "ReadLine", "Sleep", "Exit",
            "ToInt", "ToDouble", "ToString", "ToBool", "ToChar",
            "Len", "Type", "Range", "Assert", "IsNull"
        };

        // 内置类型
        var builtInTypes = new HashSet<string>
        {
            "int", "double", "string", "bool", "char", "void", "var",
            "List", "Dict", "Tuple", "Array"
        };

        for (int i = 0; i < document.Tokens.Count; i++)
        {
            var token = document.Tokens[i];

            if (token.Type != LangTokenType.Identifier)
            {
                continue;
            }

            var symbolName = token.Value;

            // 跳过定义位置的检查
            if (IsDefinitionContext(i))
            {
                continue;
            }

            // 检查符号是否已定义
            if (!_definedSymbols.Contains(symbolName) &&
                !builtInFunctions.Contains(symbolName) &&
                !builtInTypes.Contains(symbolName))
            {
                // 检查是否是成员访问（obj.member）
                if (i > 0 && document.Tokens[i - 1].Type == LangTokenType.Dot)
                {
                    // 成员访问暂时跳过（需要类型信息）
                    continue;
                }

                _diagnostics.Add(new DiagnosticInfo
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"未定义的符号 '{symbolName}'",
                    Line = token.Line,
                    Column = token.Column,
                    Source = "Old8Lang Semantic"
                });
            }
        }
    }

    /// <summary>
    /// 检查token是否在定义上下文中
    /// </summary>
    private bool IsDefinitionContext(int tokenIndex)
    {
        if (tokenIndex <= 0 || document.Tokens == null)
        {
            return false;
        }

        var token = document.Tokens[tokenIndex];
        var prevToken = document.Tokens[tokenIndex - 1];

        // func name(...)
        if (prevToken.Type == LangTokenType.Func || prevToken.Type == LangTokenType.Async)
        {
            return true;
        }

        // class Name
        if (prevToken.Type == LangTokenType.Class)
        {
            return true;
        }

        // var <- value (赋值左侧)
        if (tokenIndex + 1 < document.Tokens.Count)
        {
            var nextToken = document.Tokens[tokenIndex + 1];
            if (nextToken.Type == LangTokenType.Assignment)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查重复定义
    /// </summary>
    public void CheckDuplicateDefinitions()
    {
        if (document.SymbolTable == null)
        {
            return;
        }

        var symbolLocations = new Dictionary<string, List<SourceLocation>>();

        // 收集所有符号的定义位置
        foreach (var (name, symbol) in document.SymbolTable)
        {
            if (!symbolLocations.ContainsKey(name))
            {
                symbolLocations[name] = new List<SourceLocation>();
            }
            symbolLocations[name].Add(symbol.Location);
        }

        // 检查重复
        foreach (var (name, locations) in symbolLocations)
        {
            if (locations.Count > 1)
            {
                foreach (var location in locations)
                {
                    var diagnostic = new DiagnosticInfo
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = $"符号 '{name}' 重复定义",
                        Line = location.Line + 1,
                        Column = location.Column + 1,
                        Source = "Old8Lang Semantic"
                    };
                    _diagnostics.Add(diagnostic);
                    document.Diagnostics.Add(diagnostic);
                }
            }
        }
    }
}
