using Old8Lang.LangParser;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LanguageServer.Models;
using System.Collections.Concurrent;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 文档管理器 - 负责管理打开的文档及其解析状态
/// </summary>
public class DocumentManager
{
    private readonly ConcurrentDictionary<string, DocumentParseResult> Documents = new();

    /// <summary>
    /// 是否启用调试模式
    /// </summary>
    public bool DebugModeEnabled { get; set; } = false;

    /// <summary>
    /// 是否启用性能分析
    /// </summary>
    public bool ProfilingEnabled { get; set; } = false;

    /// <summary>
    /// 打开或更新文档
    /// </summary>
    public DocumentParseResult UpdateDocument(string uri, string text)
    {
        var result = ParseDocument(uri, text);
        Documents[uri] = result;
        return result;
    }

    /// <summary>
    /// 获取文档解析结果
    /// </summary>
    public DocumentParseResult? GetDocument(string uri)
    {
        Documents.TryGetValue(uri, out var result);
        return result;
    }

    /// <summary>
    /// 获取所有文档
    /// </summary>
    public IEnumerable<KeyValuePair<string, DocumentParseResult>> GetAllDocuments()
    {
        return Documents;
    }

    /// <summary>
    /// 关闭文档
    /// </summary>
    public void CloseDocument(string uri)
    {
        Documents.TryRemove(uri, out _);
    }

    /// <summary>
    /// 解析文档
    /// </summary>
    private DocumentParseResult ParseDocument(string uri, string text)
    {
        var result = new DocumentParseResult
        {
            Uri = uri,
            Text = text,
            Diagnostics = new List<DiagnosticInfo>()
        };

        try
        {
            // 词法分析
            var tokens = LangTokenizer.Tokenize(text);
            result.Tokens = tokens;

            // 语法分析
            var parser = new LangParser.LangParser(tokens, text, uri);
            var ast = parser.ParseProgram();
            result.Ast = ast;

            // 构建符号表
            result.SymbolTable = BuildSymbolTable(ast, uri, tokens, text);

            // 语义分析
            PerformSemanticAnalysis(result);

            // 如果启用调试模式或性能分析，添加提示
            if (DebugModeEnabled || ProfilingEnabled)
            {
                result.Diagnostics.Add(new DiagnosticInfo
                {
                    Severity = DiagnosticSeverity.Information,
                    Message = BuildDebugProfilingMessage(),
                    Line = 0,
                    Column = 0,
                    Source = "Old8Lang"
                });
            }
        }
        catch (SyntaxError ex)
        {
            result.Diagnostics.Add(new DiagnosticInfo
            {
                Severity = DiagnosticSeverity.Error,
                Message = ex.Message,
                Line = ex.Position.Line,
                Column = ex.Position.Column,
                Source = "Old8Lang"
            });
        }
        catch (Exception ex)
        {
            result.Diagnostics.Add(new DiagnosticInfo
            {
                Severity = DiagnosticSeverity.Error,
                Message = $"解析错误: {ex.Message}",
                Line = 0,
                Column = 0,
                Source = "Old8Lang"
            });
        }

        return result;
    }

    /// <summary>
    /// 执行语义分析
    /// </summary>
    private void PerformSemanticAnalysis(DocumentParseResult result)
    {
        try
        {
            var analyzer = new SemanticAnalyzer(result);
            var semanticDiagnostics = analyzer.Analyze();
            result.Diagnostics.AddRange(semanticDiagnostics);

            // 检查重复定义
            analyzer.CheckDuplicateDefinitions();
        }
        catch
        {
            // 语义分析失败不影响基本功能
        }
    }

    /// <summary>
    /// 构建调试/性能分析提示消息
    /// </summary>
    private string BuildDebugProfilingMessage()
    {
        var messages = new List<string>();
        if (DebugModeEnabled)
        {
            messages.Add("调试模式已启用");
        }
        if (ProfilingEnabled)
        {
            messages.Add("性能分析已启用");
        }
        return string.Join(", ", messages);
    }

    /// <summary>
    /// 构建符号表
    /// </summary>
    private Dictionary<string, SymbolInfo> BuildSymbolTable(BlockStatement ast, string uri, List<LangToken>? tokens, string sourceCode)
    {
        var builder = new SymbolTableBuilder(uri, tokens, sourceCode);
        return builder.Build(ast);
    }
}