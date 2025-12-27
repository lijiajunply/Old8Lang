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
            result.SymbolTable = BuildSymbolTable(ast);
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
    /// 构建符号表
    /// </summary>
    private Dictionary<string, SymbolInfo> BuildSymbolTable(BlockStatement ast)
    {
        var symbolTable = new Dictionary<string, SymbolInfo>();
        // TODO: 实现符号表构建逻辑
        return symbolTable;
    }
}