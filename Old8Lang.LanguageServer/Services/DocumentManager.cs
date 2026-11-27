using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Old8Lang.LangParser;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LanguageServer.Models;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 文档管理器 - 负责管理打开的文档及其解析状态。
/// 支持先暂存最新文本（O(1)）再由后台防抖任务解析，同时在补全/悬停等请求到达时按需解析最新文本，
/// 避免读到陈旧状态或重复解析同一版本。
/// </summary>
public class DocumentManager
{
    private sealed class StagedDocument
    {
        public required string Text { get; init; }
        public required long Version { get; init; }
    }

    private readonly ConcurrentDictionary<string, DocumentParseResult> Documents = new();
    private readonly ConcurrentDictionary<string, StagedDocument> _stagedTexts = new();
    private readonly ConcurrentDictionary<string, object> _parseLocks = new();
    [ThreadStatic]
    private static HashSet<string>? _parsingUrisOnThread;
    private long _versionSequence;
    private WorkspaceIndex? _workspaceIndex;

    /// <summary>绑定工作区索引以启用跨文件语义分析</summary>
    public void AttachWorkspaceIndex(WorkspaceIndex? workspaceIndex)
    {
        _workspaceIndex = workspaceIndex;
    }

    /// <summary>当前绑定的工作区索引（未绑定时为 null）</summary>
    public WorkspaceIndex? WorkspaceIndex => _workspaceIndex;

    /// <summary>
    /// 是否启用调试模式
    /// </summary>
    public bool DebugModeEnabled { get; set; } = false;

    /// <summary>
    /// 是否启用性能分析
    /// </summary>
    public bool ProfilingEnabled { get; set; } = false;

    /// <summary>
    /// 暂存最新文档文本（O(1)，不执行解析），返回单调递增版本号。
    /// 供防抖诊断调度器在 didChange 入口调用。
    /// </summary>
    public long StageDocumentText(string uri, string text)
    {
        var version = Interlocked.Increment(ref _versionSequence);
        _stagedTexts[uri] = new StagedDocument
        {
            Text = text,
            Version = version
        };
        return version;
    }

    /// <summary>
    /// 判断指定文档是否存在尚未解析的暂存更新
    /// </summary>
    public bool HasPendingUpdate(string uri)
    {
        if (!_stagedTexts.TryGetValue(uri, out var staged))
        {
            return false;
        }

        return !Documents.TryGetValue(uri, out var current) || current.Version < staged.Version;
    }

    /// <summary>
    /// 打开或更新文档（立即解析并缓存）
    /// </summary>
    public DocumentParseResult UpdateDocument(string uri, string text, CancellationToken cancellationToken = default)
    {
        var version = StageDocumentText(uri, text);
        var parseLock = _parseLocks.GetOrAdd(uri, _ => new object());

        lock (parseLock)
        {
            return ParseAndCommitLocked(uri, text, version, cancellationToken);
        }
    }

    /// <summary>
    /// 若文档存在尚未解析的暂存文本，则立即解析并返回最新结果；否则返回当前缓存结果。
    /// </summary>
    public DocumentParseResult? EnsureUpToDate(string uri, CancellationToken cancellationToken = default)
    {
        // 避免语义分析阶段通过 WorkspaceIndex.GetFiles() 回查当前正在解析的同一文档引发重入递归
        if (_parsingUrisOnThread != null && _parsingUrisOnThread.Contains(uri))
        {
            return Documents.TryGetValue(uri, out var inFlightExisting) ? inFlightExisting : null;
        }

        if (!_stagedTexts.TryGetValue(uri, out var staged))
        {
            return Documents.TryGetValue(uri, out var existing) ? existing : null;
        }

        var parseLock = _parseLocks.GetOrAdd(uri, _ => new object());
        lock (parseLock)
        {
            // 双重检查：可能已被其他线程解析到最新或更新版本
            if (!_stagedTexts.TryGetValue(uri, out staged))
            {
                return Documents.TryGetValue(uri, out var existing) ? existing : null;
            }

            if (Documents.TryGetValue(uri, out var current) && current.Version >= staged.Version)
            {
                return current;
            }

            return ParseAndCommitLocked(uri, staged.Text, staged.Version, cancellationToken);
        }
    }

    private DocumentParseResult ParseAndCommitLocked(
        string uri,
        string text,
        long version,
        CancellationToken cancellationToken)
    {
        _parsingUrisOnThread ??= new HashSet<string>(StringComparer.Ordinal);
        _parsingUrisOnThread.Add(uri);
        try
        {
            var result = ParseDocument(uri, text, version, cancellationToken);
            Documents.AddOrUpdate(
                uri,
                result,
                (_, existing) => existing.Version <= result.Version ? result : existing);

            if (_stagedTexts.TryGetValue(uri, out var latestStaged) && latestStaged.Version == version)
            {
                _stagedTexts.TryRemove(uri, out _);
            }

            return Documents.TryGetValue(uri, out var stored) && stored.Version >= result.Version
                ? stored
                : result;
        }
        finally
        {
            _parsingUrisOnThread.Remove(uri);
        }
    }

    /// <summary>
    /// 获取文档解析结果（若存在防抖中的暂存更新则按需立即解析，保证补全/导航始终看到最新文本）
    /// </summary>
    public DocumentParseResult? GetDocument(string uri)
    {
        if (_stagedTexts.ContainsKey(uri))
        {
            return EnsureUpToDate(uri);
        }

        Documents.TryGetValue(uri, out var result);
        return result;
    }

    /// <summary>
    /// 获取所有文档
    /// </summary>
    public IEnumerable<KeyValuePair<string, DocumentParseResult>> GetAllDocuments()
    {
        // 确保暂存的已打开文档也能被工作区索引看到最新内容
        foreach (var uri in _stagedTexts.Keys.ToArray())
        {
            EnsureUpToDate(uri);
        }

        return Documents;
    }

    /// <summary>
    /// 关闭文档
    /// </summary>
    public void CloseDocument(string uri)
    {
        _stagedTexts.TryRemove(uri, out _);
        Documents.TryRemove(uri, out _);
        _parseLocks.TryRemove(uri, out _);
    }

    /// <summary>
    /// 解析文档
    /// </summary>
    private DocumentParseResult ParseDocument(string uri, string text, long version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new DocumentParseResult
        {
            Uri = uri,
            Text = text,
            Version = version,
            Diagnostics = []
        };

        try
        {
            // 词法分析
            var tokens = LangTokenizer.Tokenize(text);
            result.Tokens = tokens;

            // 预热按行索引（O(N) 构建一次，供后续全部位置查询复用）
            _ = result.TokenIndex;

            cancellationToken.ThrowIfCancellationRequested();

            // 语法分析
            var parser = new LangParser.LangParser(tokens, text, uri);
            var ast = parser.ParseProgram();
            result.Ast = ast;

            cancellationToken.ThrowIfCancellationRequested();

            // 构建词法作用域树与顶层符号表
            var builder = new SymbolTableBuilder(uri, tokens, text);
            var rootScope = builder.BuildWithScope(ast);
            result.RootScope = rootScope;
            result.SymbolTable = rootScope.Symbols;
            result.TopLevelCalls = builder.TopLevelCalls;

            cancellationToken.ThrowIfCancellationRequested();

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
        catch (OperationCanceledException)
        {
            throw;
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

            // 容错恢复：输入 `obj.` / `this.` / `a.b().` 等未完成成员访问时，用占位标识符恢复符号表与作用域树，
            // 同时保留原始 Tokens 与真实 SyntaxError 诊断，确保成员补全与导航在编辑中断时仍可用。
            TryRecoverSymbolsOnSyntaxError(uri, text, result);
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

    private static readonly Regex DanglingDotPattern = new(
        @"(?<![0-9.])\.(?=\s*(?:\r?\n|$|[)\]}]))",
        RegexOptions.Compiled);

    /// <summary>
    /// 语法错误时的轻量级符号表恢复：
    /// 1. 优先为行尾/括号前悬空的 `.` 补上临时占位标识符重新解析符号表与作用域树；
    /// 2. 若仍无法解析，则沿用该文档最近一次有效解析的符号表与作用域树（不伪造 AST）。
    /// </summary>
    private void TryRecoverSymbolsOnSyntaxError(string uri, string text, DocumentParseResult result)
    {
        if (DanglingDotPattern.IsMatch(text))
        {
            var recoveredText = DanglingDotPattern.Replace(text, ".__old8_complete__");
            try
            {
                var recoveredTokens = LangTokenizer.Tokenize(recoveredText);
                var recoveredParser = new LangParser.LangParser(recoveredTokens, recoveredText, uri);
                var recoveredAst = recoveredParser.ParseProgram();
                var builder = new SymbolTableBuilder(uri, recoveredTokens, recoveredText);
                var recoveredScope = builder.BuildWithScope(recoveredAst);

                result.Ast = recoveredAst;
                result.RootScope = recoveredScope;
                result.SymbolTable = recoveredScope.Symbols;
                result.TopLevelCalls = builder.TopLevelCalls;
                return;
            }
            catch
            {
                // 占位恢复失败时继续走最近有效快照回退
            }
        }

        if (Documents.TryGetValue(uri, out var previous) && previous.SymbolTable != null)
        {
            result.RootScope = previous.RootScope;
            result.SymbolTable = previous.SymbolTable;
        }
    }

    /// <summary>
    /// 执行语义分析
    /// </summary>
    private void PerformSemanticAnalysis(DocumentParseResult result)
    {
        try
        {
            var analyzer = new SemanticAnalyzer(result, _workspaceIndex);
            var semanticDiagnostics = analyzer.Analyze();
            result.Diagnostics.AddRange(semanticDiagnostics);
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
