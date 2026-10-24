using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 工作区索引中的一个文件快照。
/// </summary>
public sealed class IndexedFile
{
    /// <summary>文件 URI（LSP 形式）</summary>
    public required string Uri { get; init; }

    /// <summary>文件系统路径；内存中的未保存文档为 null</summary>
    public string? FilePath { get; init; }

    /// <summary>文件内容</summary>
    public required string Text { get; init; }

    /// <summary>词法分析结果</summary>
    public required List<LangToken> Tokens { get; init; }

    /// <summary>顶层符号表（函数、类、全局变量）</summary>
    public required Dictionary<string, SymbolInfo> Symbols { get; init; }

    /// <summary>该快照是否来自已打开（可能未保存）的编辑器文档</summary>
    public bool IsOpen { get; init; }

    /// <summary>磁盘文件长度（仅磁盘快照有效）</summary>
    internal long Length { get; init; }

    /// <summary>磁盘文件最后写入时间（仅磁盘快照有效）</summary>
    internal DateTime LastWriteUtc { get; init; }

    /// <summary>是否成功完成语法分析</summary>
    public bool Parsed { get; init; }

    /// <summary>在顶层符号表中查找符号</summary>
    public bool TryGetSymbol(string name, out SymbolInfo symbol)
    {
        return Symbols.TryGetValue(name, out symbol!);
    }

    /// <summary>该文件是否声明了同名顶层符号（用于跨文件歧义判定）</summary>
    public bool DeclaresGlobal(string name) => Symbols.ContainsKey(name);
}

/// <summary>
/// 工作区索引服务 - 为跨文件查找引用、重命名、工作区符号和代码镜头提供数据。
///
/// 索引来源有两类：
/// 1. 工作区根目录下所有 <c>*.old8</c> 文件（按修改时间和长度缓存）；
/// 2. <see cref="DocumentManager"/> 中已打开的文档（覆盖磁盘版本，支持未保存内容）。
///
/// 没有配置根目录时退化为“仅已打开文档”，因此单元测试可以只构造
/// <c>new WorkspaceIndex(documentManager)</c> 而不触碰文件系统。
/// </summary>
public sealed class WorkspaceIndex(DocumentManager documentManager)
{
    private static readonly string[] SkippedDirectoryNames =
    [
        "bin", "obj", "node_modules", ".git", ".vs", ".vscode", ".idea", "out", "dist"
    ];

    private static readonly TimeSpan RescanInterval = TimeSpan.FromSeconds(2);
    private const int MaxIndexedFiles = 1000;

    private readonly DocumentManager _documentManager = documentManager;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, IndexedFile> _diskFiles = new(StringComparer.Ordinal);
    private string[] _roots = [];
    private DateTime _lastScanUtc = DateTime.MinValue;

    /// <summary>当前索引根目录（文件系统绝对路径）</summary>
    public IReadOnlyList<string> Roots
    {
        get
        {
            lock (_gate)
            {
                return _roots;
            }
        }
    }

    /// <summary>设置索引根目录；会清空磁盘缓存并在下次访问时重建</summary>
    public void SetRoots(IEnumerable<string>? roots)
    {
        var normalized = (roots ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path =>
            {
                try
                {
                    return Path.GetFullPath(path);
                }
                catch
                {
                    return path;
                }
            })
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        lock (_gate)
        {
            _roots = normalized;
            _diskFiles.Clear();
            _lastScanUtc = DateTime.MinValue;
        }
    }

    /// <summary>强制下次访问时重新扫描磁盘文件</summary>
    public void Invalidate()
    {
        lock (_gate)
        {
            _diskFiles.Clear();
            _lastScanUtc = DateTime.MinValue;
        }
    }

    /// <summary>获取当前索引快照（磁盘文件 + 已打开文档覆盖）</summary>
    public IReadOnlyList<IndexedFile> GetFiles()
    {
        lock (_gate)
        {
            RefreshDiskFiles();

            // 统一以 LSP URI 为键（磁盘缓存内部用文件路径作键）
            var files = _diskFiles.Values.ToDictionary(file => file.Uri, StringComparer.Ordinal);

            // 已打开文档优先，保证未保存内容立即生效
            foreach (var (uri, document) in _documentManager.GetAllDocuments())
            {
                files[uri] = FromOpenDocument(uri, document);
            }

            return files.Values.ToList();
        }
    }

    /// <summary>
    /// 在所有已索引文件中解析符号。优先返回 <paramref name="preferredUri"/> 中声明的符号，
    /// 其次是第一个声明的符号。
    /// </summary>
    public bool TryResolveSymbol(
        string name,
        string? preferredUri,
        out SymbolInfo? symbol,
        out IndexedFile? file)
    {
        symbol = null;
        file = null;

        var files = GetFiles();

        if (preferredUri != null)
        {
            var preferred = files.FirstOrDefault(f => f.Uri == preferredUri);
            if (preferred != null && preferred.TryGetSymbol(name, out var preferredSymbol))
            {
                symbol = preferredSymbol;
                file = preferred;
                return true;
            }
        }

        foreach (var candidate in files)
        {
            if (candidate.TryGetSymbol(name, out var candidateSymbol))
            {
                symbol = candidateSymbol;
                file = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 跨文件查找顶层符号的引用（含定义）。
    /// </summary>
    /// <remarks>
    /// 为避免把互不相关的脚本中同名符号算进来，其他文件若自身声明了同名顶层符号，
    /// 则整个文件被跳过（该文件里的同名标识符属于它自己的定义）。
    /// </remarks>
    public List<SourceLocation> FindReferences(string symbolName, string? definitionUri = null)
    {
        var references = new List<SourceLocation>();
        var files = GetFiles();

        // 定义文件优先，保证定义位置排在前面
        var ordered = files
            .OrderByDescending(f => definitionUri != null && f.Uri == definitionUri)
            .ToList();

        foreach (var file in ordered)
        {
            var isDefinitionFile = definitionUri != null && file.Uri == definitionUri;
            if (!isDefinitionFile && file.DeclaresGlobal(symbolName))
            {
                continue;
            }

            CollectIdentifierOccurrences(file, symbolName, references);
        }

        return references;
    }

    /// <summary>统计顶层符号的引用数量（含定义）</summary>
    public int CountReferences(string symbolName, string? definitionUri = null)
    {
        return FindReferences(symbolName, definitionUri).Count;
    }

    /// <summary>
    /// 收集单文件内的标识符出现位置（不做跨文件歧义过滤）。
    /// 用于方法、参数、属性等文件内作用域的符号。
    /// </summary>
    public static List<SourceLocation> CollectIdentifierOccurrences(
        IndexedFile file,
        string symbolName,
        List<SourceLocation>? into = null,
        bool includeMemberAccess = false)
    {
        var result = into ?? [];

        for (var i = 0; i < file.Tokens.Count; i++)
        {
            var token = file.Tokens[i];
            if (token.Type != LangTokenType.Identifier || token.Value != symbolName)
            {
                continue;
            }

            if (!IsReferenceLikeToken(file.Tokens, i, includeMemberAccess))
            {
                continue;
            }

            result.Add(new SourceLocation
            {
                Uri = file.Uri,
                Line = token.Line - 1,
                Column = token.Column,
                EndLine = token.Line - 1,
                EndColumn = token.Column + token.Value.Length
            });
        }

        return result;
    }

    /// <summary>
    /// 计算跨文件重命名的编辑位置。仅对顶层符号（函数/类/全局变量）跨文件生效。
    /// </summary>
    public Dictionary<string, List<SourceLocation>> FindRenameLocations(string symbolName, string? definitionUri)
    {
        var grouped = new Dictionary<string, List<SourceLocation>>(StringComparer.Ordinal);

        foreach (var location in FindReferences(symbolName, definitionUri))
        {
            if (!grouped.TryGetValue(location.Uri, out var list))
            {
                list = [];
                grouped[location.Uri] = list;
            }

            list.Add(location);
        }

        return grouped;
    }

    /// <summary>单文件内的重命名位置（方法/参数等文件内作用域符号）</summary>
    public static List<SourceLocation> FindLocalOccurrences(IndexedFile file, string symbolName)
    {
        return CollectIdentifierOccurrences(file, symbolName);
    }

    private void RefreshDiskFiles()
    {
        if (_roots.Length == 0)
        {
            _diskFiles.Clear();
            return;
        }

        var now = DateTime.UtcNow;
        if (_lastScanUtc != DateTime.MinValue && now - _lastScanUtc < RescanInterval)
        {
            return;
        }

        _lastScanUtc = now;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var budget = MaxIndexedFiles;

        foreach (var root in _roots)
        {
            IEnumerable<string> paths;
            try
            {
                paths = EnumerateSourceFiles(root);
            }
            catch
            {
                continue;
            }

            foreach (var path in paths)
            {
                if (budget-- <= 0)
                {
                    break;
                }

                seen.Add(path);

                FileInfo info;
                try
                {
                    info = new FileInfo(path);
                }
                catch
                {
                    continue;
                }

                var cacheKey = info.FullName;
                if (_diskFiles.TryGetValue(cacheKey, out var cached) &&
                    cached is { FilePath: not null } &&
                    cached.LastWriteUtc == info.LastWriteTimeUtc &&
                    cached.Length == info.Length)
                {
                    continue;
                }

                IndexedFile parsed;
                try
                {
                    parsed = BuildIndexedFile(
                        DocumentUri.FromFileSystemPath(path).ToString(),
                        path,
                        File.ReadAllText(path),
                        info.LastWriteTimeUtc,
                        info.Length);
                }
                catch
                {
                    continue;
                }

                _diskFiles[cacheKey] = parsed;
            }
        }

        // 删除已经不存在的文件
        foreach (var staleKey in _diskFiles.Keys.Where(key => !seen.Contains(key)).ToList())
        {
            _diskFiles.Remove(staleKey);
        }
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();

            string[] subdirectories;
            string[] files;
            try
            {
                subdirectories = Directory.GetDirectories(directory);
                files = Directory.GetFiles(directory, "*.old8");
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var subdirectory in subdirectories)
            {
                var name = Path.GetFileName(subdirectory);
                if (SkippedDirectoryNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                pending.Push(subdirectory);
            }
        }
    }

    private static IndexedFile BuildIndexedFile(
        string uri,
        string? filePath,
        string text,
        DateTime lastWriteUtc = default,
        long length = 0)
    {
        List<LangToken> tokens = [];
        Dictionary<string, SymbolInfo> symbols = new();
        var parsed = false;

        try
        {
            tokens = LangTokenizer.Tokenize(text);
        }
        catch
        {
            // 词法分析失败时仍保留文件条目（没有 token），不影响其他文件
        }

        if (tokens.Count > 0)
        {
            try
            {
                var parser = new LangParser.LangParser(tokens, text, uri);
                var ast = parser.ParseProgram();
                var builder = new SymbolTableBuilder(uri, tokens, text);
                symbols = builder.Build(ast);
                parsed = true;
            }
            catch
            {
                // 语法错误文件不提供符号，但 token 仍可用于文本级引用
            }
        }

        return new IndexedFile
        {
            Uri = uri,
            FilePath = filePath,
            Text = text,
            Tokens = tokens,
            Symbols = symbols,
            Parsed = parsed,
            LastWriteUtc = lastWriteUtc,
            Length = length
        };
    }

    private static IndexedFile FromOpenDocument(string uri, DocumentParseResult? document)
    {
        if (document == null)
        {
            return new IndexedFile
            {
                Uri = uri,
                Text = string.Empty,
                Tokens = [],
                Symbols = new Dictionary<string, SymbolInfo>(),
                IsOpen = true
            };
        }

        return new IndexedFile
        {
            Uri = uri,
            Text = document.Text,
            Tokens = document.Tokens ?? [],
            Symbols = document.SymbolTable ?? new Dictionary<string, SymbolInfo>(),
            IsOpen = true,
            Parsed = document.Ast != null
        };
    }

    /// <summary>
    /// 判断某个标识符 token 是否应算作“引用”。
    /// 排除成员访问（<c>obj.name</c> 中的 <c>name</c>）和参数声明，避免跨文件误报。
    /// </summary>
    private static bool IsReferenceLikeToken(List<LangToken> tokens, int index, bool includeMemberAccess)
    {
        if (!includeMemberAccess && index > 0 && tokens[index - 1].Type == LangTokenType.Dot)
        {
            return false;
        }

        // 参数声明：func f(a: int) / (a: int, b: int)
        if (index + 1 < tokens.Count && tokens[index + 1].Type == LangTokenType.Colon)
        {
            if (index == 0)
            {
                return false;
            }

            var previous = tokens[index - 1].Type;
            if (previous is LangTokenType.LeftParen or LangTokenType.Comma or LangTokenType.Func)
            {
                return false;
            }
        }

        return true;
    }
}
