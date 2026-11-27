using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.Syntax;
using SymbolInfo = Old8Lang.LanguageServer.Models.SymbolInfo;
using SymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 语义分析器 - 基于 AST、词法作用域链与工作区索引执行语义诊断：
/// 1. 作用域感知的未定义符号检测；
/// 2. 同作用域重复定义检测；
/// 3. 显式类型注解赋值、return 返回值与函数/方法调用参数的类型不匹配检测；
/// 4. 未使用的局部变量警告；
/// 5. 已知用户类上的成员不存在、静态/实例误用与可见性违规检测；
/// 6. 跨文件导入解析与跨文件符号/类型检查。
/// </summary>
public class SemanticAnalyzer(DocumentParseResult document, WorkspaceIndex? workspaceIndex = null)
{
    private const string DiagnosticSource = "Old8Lang Semantic";

    private static readonly HashSet<string> BuiltInTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "double", "string", "bool", "char", "void", "any", "var", "object", "null", "function",
        "list", "dict", "dictionary", "tuple", "array", "set", "task", "thread", "channel",
        "comparable", "equatable", "cloneable", "disposable", "iterable", "enumerable",
        "icomparable", "iequatable", "icloneable", "idisposable"
    };

    private static readonly HashSet<string> BuiltInRuntimeNamespaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "Task", "Thread", "Channel", "Mutex", "Semaphore", "AtomicInt", "ReadWriteLock",
        "CountDownLatch", "CyclicBarrier", "CancellationTokenSource", "Lock", "Assert",
        "Math", "OS", "File", "Time", "Terminal", "Json", "Csv", "Crypto", "Vector", "Regex",
        "ColorfulTerminal", "TemplateEngine", "AssertLib", "CollectionLib", "MockLib", "TestRunner",
        "Net", "Http", "Socket", "WebSocket", "Database", "Sqlite", "MySql", "Postgres",
        "Serialization", "Protobuf", "MessagePack", "ML", "MachineLearning"
    };

    private static readonly HashSet<string> UniversalObjectMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "ToStr", "ToString", "GetType", "Equals", "HashCode", "GetHashCode",
        "ToInt", "ToDouble", "ToBool", "ToChar", "Dispose", "Clone"
    };

    private readonly List<DiagnosticInfo> _diagnostics = [];
    private readonly HashSet<SymbolInfo> _readSymbols = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, SymbolInfo> _importedSymbols = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, SymbolInfo>> _importedModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knownExternalModules = new(StringComparer.OrdinalIgnoreCase);
    private readonly ModuleResolver _moduleResolver = new();

    private SymbolScope? _rootScope;
    private Dictionary<string, SymbolInfo> _combinedSymbols = new(StringComparer.Ordinal);
    private HashSet<string> _builtInFunctions = new(StringComparer.OrdinalIgnoreCase);
    private bool _duplicatesChecked;

    /// <summary>
    /// 执行完整语义分析并返回诊断列表。
    /// </summary>
    public List<DiagnosticInfo> Analyze()
    {
        _diagnostics.Clear();
        _readSymbols.Clear();
        _importedSymbols.Clear();
        _importedModules.Clear();
        _knownExternalModules.Clear();
        _duplicatesChecked = false;

        if (document.Ast == null)
        {
            return _diagnostics;
        }

        // 确保全局函数注册表已初始化
        GlobalFunctionInitializer.EnsureInitialized();
        _builtInFunctions = new HashSet<string>(
            GlobalFunctionRegistry.Instance.GetAllFunctionNames(),
            StringComparer.OrdinalIgnoreCase);
        foreach (var nativeName in NativeLibraryRegistry.GetAllFunctionNames())
        {
            _builtInFunctions.Add(nativeName);
        }

        // 确保存在词法作用域树（兼容手工构造 DocumentParseResult 的旧单测）
        _rootScope = document.RootScope;
        if (_rootScope == null)
        {
            var builder = new SymbolTableBuilder(document.Uri, document.Tokens, document.Text);
            _rootScope = builder.BuildWithScope(document.Ast);
        }

        // 1. 解析跨文件 import 与工作区符号
        CollectImports(document.Ast);
        BuildCombinedSymbolTable();

        // 2. 检查同作用域重复定义
        CollectDuplicateDefinitionDiagnostics();

        // 3. 基于 AST 与作用域链遍历语句与表达式
        AnalyzeBlock(document.Ast, _rootScope, enclosingCallable: null, activeGenerics: []);

        // 4. 检查未使用的局部变量
        CheckUnusedLocalVariables(_rootScope);

        return _diagnostics;
    }

    /// <summary>
    /// 检查同一作用域内的重复定义，并写入 document.Diagnostics（供独立调用的兼容入口）。
    /// </summary>
    public void CheckDuplicateDefinitions()
    {
        var added = CollectDuplicateDefinitionDiagnostics();
        if (added.Count > 0)
        {
            document.Diagnostics.AddRange(added);
        }
    }

    private List<DiagnosticInfo> CollectDuplicateDefinitionDiagnostics()
    {
        if (_duplicatesChecked)
        {
            return [];
        }

        _duplicatesChecked = true;

        var rootScope = _rootScope ?? document.RootScope;
        if (rootScope == null && document.Ast != null)
        {
            var builder = new SymbolTableBuilder(document.Uri, document.Tokens, document.Text);
            rootScope = builder.BuildWithScope(document.Ast);
            _rootScope = rootScope;
        }

        if (rootScope == null)
        {
            return [];
        }

        var added = new List<DiagnosticInfo>();

        foreach (var scope in rootScope.EnumerateAllScopes())
        {
            var groups = scope.AllDeclarations
                .Where(d => d.Kind is SymbolKind.Function or SymbolKind.Class or SymbolKind.Interface
                    or SymbolKind.Enum or SymbolKind.Method or SymbolKind.Property or SymbolKind.Field
                    or SymbolKind.Parameter)
                .GroupBy(d => d.Name, StringComparer.Ordinal);

            foreach (var group in groups)
            {
                var declarations = group.ToList();
                if (declarations.Count <= 1)
                {
                    continue;
                }

                // 类内同名方法若参数个数全部不同，属于合法重载，不报重复定义
                if (scope.Kind == ScopeKind.Class &&
                    declarations.All(d => d.Kind == SymbolKind.Method) &&
                    declarations.Select(d => d.Parameters.Count).Distinct().Count() == declarations.Count)
                {
                    continue;
                }

                foreach (var dup in declarations)
                {
                    var diagnostic = new DiagnosticInfo
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = $"符号 '{dup.Name}' 重复定义",
                        Line = dup.Location.Line + 1,
                        Column = dup.Location.Column,
                        Source = DiagnosticSource
                    };
                    _diagnostics.Add(diagnostic);
                    added.Add(diagnostic);
                }
            }
        }

        return added;
    }

    private void CollectImports(BlockStatement ast)
    {
        var allImports = ast.ImportStatements.OfType<ImportStatement>()
            .Concat(ast.OtherStatements.OfType<ImportStatement>());

        foreach (var importStmt in allImports)
        {
            if (importStmt.IsDynamicImport)
            {
                continue;
            }

            var moduleName = importStmt.ImportString;
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                continue;
            }

            if (IsStandardOrBuiltinModule(moduleName))
            {
                RegisterExternalModule(importStmt, moduleName);
                continue;
            }

            var targetFile = ResolveImportedWorkspaceFile(moduleName);
            if (targetFile != null)
            {
                BindImportedFileSymbols(importStmt, moduleName, targetFile);
                continue;
            }

            // 仅对明确指向本地相对路径且当前文档在真实磁盘目录下时报告模块不存在，避免虚拟 URI 单测误报
            if (LooksLikeLocalFileImport(moduleName) && TryGetExistingDocumentDirectory(document.Uri, out _))
            {
                AddDiagnostic(
                    DiagnosticSeverity.Error,
                    $"无法解析导入的模块 '{moduleName}'",
                    importStmt.ModuleNamePosition);
            }
            else
            {
                // 无法定位具体文件时，至少把别名或选择导入名视为已知符号，避免连锁未定义误报
                RegisterExternalModule(importStmt, moduleName);
            }
        }
    }

    private void RegisterExternalModule(ImportStatement importStmt, string moduleName)
    {
        _knownExternalModules.Add(moduleName);
        if (!string.IsNullOrEmpty(importStmt.ModuleAlias))
        {
            _knownExternalModules.Add(importStmt.ModuleAlias);
        }

        if (importStmt.IsSelective)
        {
            foreach (var specifier in importStmt.Specifiers)
            {
                var localName = specifier.Alias ?? specifier.Name;
                if (!string.IsNullOrEmpty(localName))
                {
                    _knownExternalModules.Add(localName);
                }
            }
        }
    }

    private void BindImportedFileSymbols(ImportStatement importStmt, string moduleName, IndexedFile targetFile)
    {
        if (importStmt.IsSelective)
        {
            foreach (var specifier in importStmt.Specifiers)
            {
                var localName = specifier.Alias ?? specifier.Name;
                if (targetFile.Symbols.TryGetValue(specifier.Name, out var exported))
                {
                    _importedSymbols[localName] = exported;
                }
                else
                {
                    AddDiagnostic(
                        DiagnosticSeverity.Error,
                        $"模块 '{moduleName}' 中不存在导出符号 '{specifier.Name}'",
                        importStmt.Position);
                }
            }

            return;
        }

        if (!string.IsNullOrEmpty(importStmt.ModuleAlias))
        {
            _importedModules[importStmt.ModuleAlias] = targetFile.Symbols;
            return;
        }

        // 非选择性直接导入：将目标文件全部顶层符号导入当前文件可见集
        foreach (var (name, sym) in targetFile.Symbols)
        {
            _importedSymbols[name] = sym;
        }
    }

    private IndexedFile? ResolveImportedWorkspaceFile(string moduleName)
    {
        string? resolvedPath = null;
        if (TryGetExistingDocumentDirectory(document.Uri, out var currentPath))
        {
            try
            {
                var resolved = _moduleResolver.ResolveModule(moduleName, currentPath);
                if (resolved.IsSuccess && !string.IsNullOrEmpty(resolved.ResolvedPath))
                {
                    resolvedPath = Path.GetFullPath(resolved.ResolvedPath);
                }
            }
            catch
            {
                // 忽略解析异常，走后续回退
            }
        }

        if (workspaceIndex != null)
        {
            var files = workspaceIndex.GetFiles();

            // 1. 按解析出的绝对路径精确匹配
            if (resolvedPath != null)
            {
                var byPath = files.FirstOrDefault(f =>
                    !string.IsNullOrEmpty(f.FilePath) &&
                    string.Equals(Path.GetFullPath(f.FilePath), resolvedPath, StringComparison.OrdinalIgnoreCase));
                if (byPath != null)
                {
                    return byPath;
                }

                var targetUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri
                    .FromFileSystemPath(resolvedPath).ToString();
                var byUri = files.FirstOrDefault(f => string.Equals(f.Uri, targetUri, StringComparison.OrdinalIgnoreCase));
                if (byUri != null)
                {
                    return byUri;
                }
            }

            // 2. 按文件名/模块名匹配内存或工作区文件（支持虚拟 URI 跨文件单测，如 "./lib.old8" 或 "lib"）
            var normalizedName = moduleName.Replace('\\', '/').TrimStart('.', '/');
            var normalizedWithExt = normalizedName.EndsWith(".old8", StringComparison.OrdinalIgnoreCase)
                ? normalizedName
                : normalizedName + ".old8";

            var bySuffix = files.FirstOrDefault(f =>
                !string.Equals(f.Uri, document.Uri, StringComparison.OrdinalIgnoreCase) &&
                (f.Uri.EndsWith("/" + normalizedWithExt, StringComparison.OrdinalIgnoreCase) ||
                 f.Uri.EndsWith("/" + normalizedName, StringComparison.OrdinalIgnoreCase)));
            if (bySuffix != null)
            {
                return bySuffix;
            }
        }

        // 3. 若磁盘上存在该文件但尚未被索引，直接按需解析
        if (resolvedPath != null && File.Exists(resolvedPath))
        {
            try
            {
                var text = File.ReadAllText(resolvedPath);
                var tokens = LangTokenizer.Tokenize(text);
                var targetUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri
                    .FromFileSystemPath(resolvedPath).ToString();
                var ast = new LangParser.LangParser(tokens, text, targetUri).ParseProgram();
                var builder = new SymbolTableBuilder(targetUri, tokens, text);
                var scope = builder.BuildWithScope(ast);
                var symbols = scope.Symbols;
                return new IndexedFile
                {
                    Uri = targetUri,
                    FilePath = resolvedPath,
                    Text = text,
                    Tokens = tokens,
                    Symbols = symbols,
                    RootScope = scope,
                    Parsed = true
                };
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private void BuildCombinedSymbolTable()
    {
        _combinedSymbols = new Dictionary<string, SymbolInfo>(StringComparer.Ordinal);
        foreach (var (name, sym) in _importedSymbols)
        {
            _combinedSymbols[name] = sym;
        }

        if (document.SymbolTable != null)
        {
            foreach (var (name, sym) in document.SymbolTable)
            {
                _combinedSymbols[name] = sym;
            }
        }
    }

    private void AnalyzeBlock(
        BlockStatement block,
        SymbolScope currentScope,
        SymbolInfo? enclosingCallable,
        HashSet<string> activeGenerics)
    {
        foreach (var stmt in block.ImportStatements)
        {
            AnalyzeStatement(stmt, currentScope, enclosingCallable, activeGenerics);
        }

        foreach (var stmt in block.OtherStatements)
        {
            AnalyzeStatement(stmt, currentScope, enclosingCallable, activeGenerics);
        }
    }

    private void AnalyzeStatement(
        IOldLangTree statement,
        SymbolScope currentScope,
        SymbolInfo? enclosingCallable,
        HashSet<string> activeGenerics)
    {
        switch (statement)
        {
            case FuncInit funcInit:
            {
                var funcSymbol = funcInit.Id != null && currentScope.Symbols.TryGetValue(funcInit.Id.IdName, out var sym)
                    ? sym
                    : null;
                var funcScope = FindChildScopeForOwner(currentScope, ScopeKind.Function, funcSymbol)
                                ?? FindChildScopeAt(currentScope, funcInit.Position);
                var nextGenerics = ExtendGenerics(activeGenerics, funcSymbol?.GenericParameters);
                AnalyzeBlock(funcInit.Body, funcScope, funcSymbol, nextGenerics);
                break;
            }

            case AsyncFuncInit asyncFuncInit:
            {
                var funcSymbol = asyncFuncInit.Id != null && currentScope.Symbols.TryGetValue(asyncFuncInit.Id.IdName, out var sym)
                    ? sym
                    : null;
                var funcScope = FindChildScopeForOwner(currentScope, ScopeKind.Function, funcSymbol)
                                ?? FindChildScopeAt(currentScope, asyncFuncInit.Position);
                var nextGenerics = ExtendGenerics(activeGenerics, funcSymbol?.GenericParameters);
                AnalyzeBlock(asyncFuncInit.Body, funcScope, funcSymbol, nextGenerics);
                break;
            }

            case ClassInit classInit:
                AnalyzeClass(classInit, currentScope, activeGenerics);
                break;

            case SetStatement setStmt:
                AnalyzeSetStatement(setStmt, currentScope, activeGenerics);
                break;

            case ReturnStatement returnStmt:
                AnalyzeReturnStatement(returnStmt, currentScope, enclosingCallable, activeGenerics);
                break;

            case FuncRunStatement runStmt:
                if (runStmt.Expression != null)
                {
                    AnalyzeExpression(runStmt.Expression, currentScope, activeGenerics);
                }
                break;

            case ThrowStatement throwStmt:
                AnalyzeExpression(throwStmt.Expression, currentScope, activeGenerics);
                break;

            case YieldStatement yieldStmt:
                AnalyzeExpression(yieldStmt.YieldExpression, currentScope, activeGenerics);
                break;

            case IfStatement ifStmt:
                foreach (var child in ifStmt.Children)
                {
                    AnalyzeExpression(child.Condition, currentScope, activeGenerics);
                    var branchScope = FindChildScopeAt(currentScope, child.Block.Position);
                    AnalyzeBlock(child.Block, branchScope, enclosingCallable, activeGenerics);
                }

                if (ifStmt.ElseBlock != null)
                {
                    var elseScope = FindChildScopeAt(currentScope, ifStmt.ElseBlock.Position);
                    AnalyzeBlock(ifStmt.ElseBlock, elseScope, enclosingCallable, activeGenerics);
                }
                break;

            case WhileStatement whileStmt:
            {
                AnalyzeExpression(whileStmt.Condition, currentScope, activeGenerics);
                var blockScope = FindChildScopeAt(currentScope, whileStmt.Block.Position);
                if (whileStmt.Block is BlockStatement wb)
                {
                    AnalyzeBlock(wb, blockScope, enclosingCallable, activeGenerics);
                }
                else
                {
                    AnalyzeStatement(whileStmt.Block, blockScope, enclosingCallable, activeGenerics);
                }
                break;
            }

            case ForStatement forStmt:
            {
                var loopScope = FindChildScopeAt(currentScope, forStmt.Position, ScopeKind.Loop);
                AnalyzeSetStatement(forStmt.Init, loopScope, activeGenerics);
                AnalyzeExpression(forStmt.Condition, loopScope, activeGenerics);
                AnalyzeStatement(forStmt.Operation, loopScope, enclosingCallable, activeGenerics);
                AnalyzeBlock(forStmt.Block, loopScope, enclosingCallable, activeGenerics);
                break;
            }

            case ForInStatement forInStmt:
            {
                AnalyzeExpression(forInStmt.Expression, currentScope, activeGenerics);
                var loopScope = FindChildScopeAt(currentScope, forInStmt.Position, ScopeKind.Loop);
                if (forInStmt.Body is BlockStatement fb)
                {
                    AnalyzeBlock(fb, loopScope, enclosingCallable, activeGenerics);
                }
                else
                {
                    AnalyzeStatement(forInStmt.Body, loopScope, enclosingCallable, activeGenerics);
                }
                break;
            }

            case AsyncForInStatement asyncForInStmt:
            {
                AnalyzeExpression(asyncForInStmt.Expression, currentScope, activeGenerics);
                var loopScope = FindChildScopeAt(currentScope, asyncForInStmt.Position, ScopeKind.Loop);
                if (asyncForInStmt.Body is BlockStatement afb)
                {
                    AnalyzeBlock(afb, loopScope, enclosingCallable, activeGenerics);
                }
                else
                {
                    AnalyzeStatement(asyncForInStmt.Body, loopScope, enclosingCallable, activeGenerics);
                }
                break;
            }

            case TryStatement tryStmt:
            {
                var tryScope = FindChildScopeAt(currentScope, tryStmt.TryBlock.Position);
                AnalyzeBlock(tryStmt.TryBlock, tryScope, enclosingCallable, activeGenerics);

                foreach (var (_, _, filter, catchBlock) in tryStmt.CatchBlocks)
                {
                    var catchScope = FindChildScopeAt(currentScope, catchBlock.Position, ScopeKind.Catch);
                    if (filter != null)
                    {
                        AnalyzeExpression(filter, catchScope, activeGenerics);
                    }

                    AnalyzeBlock(catchBlock, catchScope, enclosingCallable, activeGenerics);
                }

                if (tryStmt.FinallyBlock != null)
                {
                    var finallyScope = FindChildScopeAt(currentScope, tryStmt.FinallyBlock.Position);
                    AnalyzeBlock(tryStmt.FinallyBlock, finallyScope, enclosingCallable, activeGenerics);
                }
                break;
            }

            case UsingStatement usingStmt:
            {
                AnalyzeExpression(usingStmt.ResourceExpression, currentScope, activeGenerics);
                var usingScope = FindChildScopeAt(currentScope, usingStmt.Position, ScopeKind.Using);
                AnalyzeBlock(usingStmt.BlockStatement, usingScope, enclosingCallable, activeGenerics);
                break;
            }

            case SwitchStatement switchStmt:
            {
                AnalyzeExpression(switchStmt.SwitchExpression, currentScope, activeGenerics);
                foreach (var caseStmt in switchStmt.SwitchCaseList)
                {
                    AnalyzeExpression(caseStmt.Expression, currentScope, activeGenerics);
                    var caseScope = FindChildScopeAt(currentScope, caseStmt.BlockStatement.Position);
                    AnalyzeBlock(caseStmt.BlockStatement, caseScope, enclosingCallable, activeGenerics);
                }

                if (switchStmt.DefaultBlockStatement != null)
                {
                    var defaultScope = FindChildScopeAt(currentScope, switchStmt.DefaultBlockStatement.Position);
                    AnalyzeBlock(switchStmt.DefaultBlockStatement, defaultScope, enclosingCallable, activeGenerics);
                }
                break;
            }

            case SelectStatement selectStmt:
            {
                foreach (var selectCase in selectStmt.Cases)
                {
                    AnalyzeExpression(selectCase.ChannelExpression, currentScope, activeGenerics);
                    if (selectCase.SendValueExpression != null)
                    {
                        AnalyzeExpression(selectCase.SendValueExpression, currentScope, activeGenerics);
                    }

                    var caseScope = FindChildScopeAt(currentScope, selectCase.BlockStatement.Position);
                    AnalyzeBlock(selectCase.BlockStatement, caseScope, enclosingCallable, activeGenerics);
                }

                if (selectStmt.DefaultCase != null)
                {
                    var defaultScope = FindChildScopeAt(currentScope, selectStmt.DefaultCase.Position);
                    AnalyzeBlock(selectStmt.DefaultCase, defaultScope, enclosingCallable, activeGenerics);
                }
                break;
            }

            case DeferStatement deferStmt:
                AnalyzeStatement(deferStmt.Statement, currentScope, enclosingCallable, activeGenerics);
                break;

            case BlockStatement nestedBlock:
            {
                var nestedScope = FindChildScopeAt(currentScope, nestedBlock.Position);
                AnalyzeBlock(nestedBlock, nestedScope, enclosingCallable, activeGenerics);
                break;
            }
        }
    }

    private void AnalyzeClass(ClassInit classInit, SymbolScope parentScope, HashSet<string> activeGenerics)
    {
        var classSymbol = parentScope.Symbols.TryGetValue(classInit.ClassName, out var sym) ? sym : null;
        var classScope = FindChildScopeForOwner(parentScope, ScopeKind.Class, classSymbol)
                         ?? FindChildScopeAt(parentScope, classInit.Position, ScopeKind.Class);
        var classGenerics = ExtendGenerics(activeGenerics, classSymbol?.GenericParameters);

        foreach (var memberStmt in classInit.Body.ImportStatements.Concat(classInit.Body.OtherStatements))
        {
            switch (memberStmt)
            {
                case ClassFieldSetStatement fieldSet:
                    AnalyzeFieldInitializer(fieldSet.Id, fieldSet.Value, classScope, classGenerics);
                    break;

                case SetStatement { Id: not null } plainField:
                    AnalyzeFieldInitializer(plainField.Id, plainField.Value, classScope, classGenerics);
                    break;

                case ClassFuncInitStatement methodStmt:
                {
                    var methodSymbol = classSymbol?.Members.GetValueOrDefault(methodStmt.Id.IdName);
                    var methodScope = FindChildScopeForOwner(classScope, ScopeKind.Method, methodSymbol)
                                      ?? FindChildScopeAt(classScope, methodStmt.Position, ScopeKind.Method);
                    var methodGenerics = ExtendGenerics(classGenerics, methodSymbol?.GenericParameters);
                    AnalyzeBlock(methodStmt.Function.Body, methodScope, methodSymbol, methodGenerics);
                    break;
                }

                case FuncInit plainMethod when plainMethod.Id != null:
                {
                    var methodSymbol = classSymbol?.Members.GetValueOrDefault(plainMethod.Id.IdName);
                    var methodScope = FindChildScopeForOwner(classScope, ScopeKind.Method, methodSymbol)
                                      ?? FindChildScopeAt(classScope, plainMethod.Position, ScopeKind.Method);
                    var methodGenerics = ExtendGenerics(classGenerics, methodSymbol?.GenericParameters);
                    AnalyzeBlock(plainMethod.Body, methodScope, methodSymbol, methodGenerics);
                    break;
                }

                case ClassInit nestedClass:
                    AnalyzeClass(nestedClass, classScope, classGenerics);
                    break;
            }
        }
    }

    private void AnalyzeFieldInitializer(
        LangId fieldId,
        LangExpression valueExpr,
        SymbolScope classScope,
        HashSet<string> activeGenerics)
    {
        // 解析器为无初始值的字段声明（public name: string）合成与 fieldId 同位置的 null 字面量，不视为赋值
        if (IsSyntheticNullInitializer(fieldId.Position, valueExpr))
        {
            return;
        }

        AnalyzeExpression(valueExpr, classScope, activeGenerics);

        if (!string.IsNullOrEmpty(fieldId.AssumptionType))
        {
            var actualType = InferExpressionType(valueExpr, classScope);
            if (!string.IsNullOrEmpty(actualType) &&
                !TypeResolver.IsAssignableTo(actualType, fieldId.AssumptionType, _combinedSymbols, workspaceIndex, document.Uri, activeGenerics))
            {
                AddDiagnostic(
                    DiagnosticSeverity.Error,
                    $"类型不匹配: 无法将 '{actualType}' 赋值给 '{fieldId.AssumptionType}' 类型的字段 '{fieldId.IdName}'",
                    valueExpr.Position.Line > 0 ? valueExpr.Position : fieldId.Position);
            }
        }
    }

    private void AnalyzeSetStatement(SetStatement setStmt, SymbolScope scope, HashSet<string> activeGenerics)
    {
        if (setStmt.Id != null && IsSyntheticNullInitializer(setStmt.Id.Position, setStmt.Value))
        {
            return;
        }

        // 分析右值表达式
        if (setStmt.Value != null)
        {
            AnalyzeExpression(setStmt.Value, scope, activeGenerics);
        }

        // 普通变量赋值：x <- expr 或 x: type <- expr
        if (setStmt.Id != null && !string.IsNullOrEmpty(setStmt.Id.IdName))
        {
            var varName = setStmt.Id.IdName;
            var rhsType = setStmt.Value != null ? InferExpressionType(setStmt.Value, scope) : null;

            string? expectedType = !string.IsNullOrEmpty(setStmt.Id.AssumptionType)
                ? setStmt.Id.AssumptionType
                : null;

            if (expectedType == null)
            {
                var line = Math.Max(0, setStmt.Id.Position.Line - 1);
                var col = Math.Max(0, setStmt.Id.Position.Column);
                var existing = scope.Resolve(varName, line, col);
                if (existing is { HasExplicitTypeAnnotation: true } && !string.IsNullOrEmpty(existing.Type))
                {
                    expectedType = existing.Type;
                }
            }

            if (!string.IsNullOrEmpty(expectedType) && !string.IsNullOrEmpty(rhsType) &&
                !TypeResolver.IsAssignableTo(rhsType, expectedType, _combinedSymbols, workspaceIndex, document.Uri, activeGenerics))
            {
                AddDiagnostic(
                    DiagnosticSeverity.Error,
                    $"类型不匹配: 无法将 '{rhsType}' 赋值给 '{expectedType}' 类型的变量 '{varName}'",
                    setStmt.Value?.Position.Line > 0 ? setStmt.Value.Position : setStmt.Id.Position);
            }

            return;
        }

        // 复杂左值赋值（成员访问 obj.field <- val 或索引 arr[i] <- val）
        if (setStmt.LeftExpression != null)
        {
            AnalyzeLeftExpression(setStmt.LeftExpression, setStmt.Value, scope, activeGenerics);
        }
    }

    private void AnalyzeLeftExpression(
        LangExpression leftExpr,
        LangExpression? valueExpr,
        SymbolScope scope,
        HashSet<string> activeGenerics)
    {
        switch (leftExpr)
        {
            case Operation { Opera: LangTokenType.Dot } dotOp:
            {
                var member = AnalyzeDotOperation(dotOp, scope, activeGenerics);
                if (member is { HasExplicitTypeAnnotation: true } && valueExpr != null)
                {
                    var rhsType = InferExpressionType(valueExpr, scope);
                    var memberType = TypeResolver.ExtractReturnType(member.Type);
                    if (!string.IsNullOrEmpty(rhsType) && !string.IsNullOrEmpty(memberType) &&
                        !TypeResolver.IsAssignableTo(rhsType, memberType, _combinedSymbols, workspaceIndex, document.Uri, activeGenerics))
                    {
                        AddDiagnostic(
                            DiagnosticSeverity.Error,
                            $"类型不匹配: 无法将 '{rhsType}' 赋值给 '{memberType}' 类型的成员 '{member.Name}'",
                            valueExpr.Position.Line > 0 ? valueExpr.Position : dotOp.Position);
                    }
                }
                break;
            }

            case Operation { Left: LangId { IdName: "array_destruct" or "object_destruct" } }:
                // 解构赋值的合成左值节点不作未定义检查
                break;

            default:
                AnalyzeExpression(leftExpr, scope, activeGenerics);
                break;
        }
    }

    private void AnalyzeReturnStatement(
        ReturnStatement returnStmt,
        SymbolScope scope,
        SymbolInfo? enclosingCallable,
        HashSet<string> activeGenerics)
    {
        if (returnStmt.Expression != null)
        {
            AnalyzeExpression(returnStmt.Expression, scope, activeGenerics);
        }

        if (enclosingCallable is not { HasExplicitTypeAnnotation: true })
        {
            return;
        }

        var expectedReturn = TypeResolver.ExtractReturnType(enclosingCallable.Type);
        if (string.IsNullOrEmpty(expectedReturn) ||
            string.Equals(expectedReturn, "void", StringComparison.OrdinalIgnoreCase) ||
            TypeResolver.IsDynamicType(expectedReturn))
        {
            return;
        }

        if (returnStmt.Expression == null)
        {
            return;
        }

        var actualType = InferExpressionType(returnStmt.Expression, scope);
        if (!string.IsNullOrEmpty(actualType) &&
            !TypeResolver.IsAssignableTo(actualType, expectedReturn, _combinedSymbols, workspaceIndex, document.Uri, activeGenerics))
        {
            AddDiagnostic(
                DiagnosticSeverity.Error,
                $"返回类型不匹配: 期望 '{expectedReturn}'，实际为 '{actualType}'",
                returnStmt.Expression.Position.Line > 0 ? returnStmt.Expression.Position : returnStmt.Position);
        }
    }

    private void AnalyzeExpression(LangExpression? expr, SymbolScope scope, HashSet<string> activeGenerics)
    {
        if (expr == null)
        {
            return;
        }

        switch (expr)
        {
            case ScalarLiteralExpression:
            case IntLangValue:
            case DoubleLangValue:
            case StringLangValue:
            case CharLangValue:
            case BoolLangValue:
            case NullLangValue:
            case ThisExpression:
            case SuperExpression:
                break;

            case LangId id:
                AnalyzeIdentifierRead(id, scope);
                break;

            case Operation { Opera: LangTokenType.Dot } dotOp:
                AnalyzeDotOperation(dotOp, scope, activeGenerics);
                break;

            case Operation { Opera: LangTokenType.As } asOp:
                AnalyzeExpression(asOp.Left, scope, activeGenerics);
                // 右侧是类型名，不做变量未定义检查
                break;

            case Operation op:
                AnalyzeExpression(op.Left, scope, activeGenerics);
                AnalyzeExpression(op.Right, scope, activeGenerics);
                break;

            case CallExpression call:
                AnalyzeDirectCall(call.Id, call.Arguments, call.NamedArguments, scope, activeGenerics);
                break;

            case Instance inst:
                AnalyzeDirectCall(inst.Id, inst.Ids, inst.NamedArgs, scope, activeGenerics);
                break;

            case FunctionCallExpression funcCall:
                AnalyzeFunctionCallExpression(funcCall, scope, activeGenerics);
                break;

            case GenericInstanceExpression genInst:
                if (genInst.BaseExpression is LangId genBaseId)
                {
                    AnalyzeIdentifierRead(genBaseId, scope, allowTypeOrFunction: true);
                }
                else
                {
                    AnalyzeExpression(genInst.BaseExpression, scope, activeGenerics);
                }
                break;

            case LambdaExpression lambda:
            {
                var lambdaScope = FindChildScopeAt(scope, lambda.Position, ScopeKind.Lambda);
                AnalyzeBlock(lambda.Body, lambdaScope, enclosingCallable: null, activeGenerics);
                break;
            }

            case TernaryExpression ternary:
                AnalyzeExpression(ternary.Condition, scope, activeGenerics);
                AnalyzeExpression(ternary.TrueExpression, scope, activeGenerics);
                AnalyzeExpression(ternary.FalseExpression, scope, activeGenerics);
                break;

            case AwaitExpression awaitExpr:
                AnalyzeExpression(awaitExpr.Expression, scope, activeGenerics);
                break;

            case StringTemplateValue template:
                foreach (var item in template.ExpressionList)
                {
                    AnalyzeExpression(item, scope, activeGenerics);
                }
                break;

            case ListLiteralExpression listLit:
                foreach (var el in listLit.Elements)
                {
                    AnalyzeExpression(el, scope, activeGenerics);
                }
                break;

            case ArrayLiteralExpression arrayLit:
                foreach (var el in arrayLit.Elements)
                {
                    AnalyzeExpression(el, scope, activeGenerics);
                }
                break;

            case DictionaryLiteralExpression dictLit:
                foreach (var (k, v) in dictLit.Entries)
                {
                    AnalyzeExpression(k, scope, activeGenerics);
                    AnalyzeExpression(v, scope, activeGenerics);
                }
                break;

            case TupleLiteralExpression tupleLit:
                foreach (var el in tupleLit.Elements)
                {
                    AnalyzeExpression(el, scope, activeGenerics);
                }
                break;

            case RangeLiteralExpression rangeLit:
                AnalyzeExpression(rangeLit.Start, scope, activeGenerics);
                AnalyzeExpression(rangeLit.End, scope, activeGenerics);
                break;

            case IndexExpression indexExpr:
                AnalyzeExpression(indexExpr.Target, scope, activeGenerics);
                AnalyzeExpression(indexExpr.Key, scope, activeGenerics);
                break;

            case SliceExpression sliceExpr:
                AnalyzeExpression(sliceExpr.Id, scope, activeGenerics);
                AnalyzeExpression(sliceExpr.Start, scope, activeGenerics);
                AnalyzeExpression(sliceExpr.End, scope, activeGenerics);
                AnalyzeExpression(sliceExpr.Step, scope, activeGenerics);
                break;

            case NestedIndexExpression nestedIdx:
                AnalyzeExpression(nestedIdx.Target, scope, activeGenerics);
                AnalyzeExpression(nestedIdx.Index, scope, activeGenerics);
                break;

            case NestedSliceExpression nestedSlice:
                AnalyzeExpression(nestedSlice.Target, scope, activeGenerics);
                AnalyzeExpression(nestedSlice.Start, scope, activeGenerics);
                AnalyzeExpression(nestedSlice.End, scope, activeGenerics);
                AnalyzeExpression(nestedSlice.Step, scope, activeGenerics);
                break;

            case MatchExpression matchExpr:
                AnalyzeMatchExpression(matchExpr, scope, activeGenerics);
                break;

            case ListComprehension comp:
                AnalyzeListComprehension(comp, scope, activeGenerics);
                break;
        }
    }

    private void AnalyzeMatchExpression(MatchExpression matchExpr, SymbolScope scope, HashSet<string> activeGenerics)
    {
        AnalyzeExpression(matchExpr.MatchValue, scope, activeGenerics);
        foreach (var matchCase in matchExpr.Cases)
        {
            var caseScope = new SymbolScope
            {
                Kind = ScopeKind.Block,
                Parent = scope,
                Range = scope.Range
            };

            if (!string.IsNullOrEmpty(matchCase.BindingVariable) && matchCase.BindingVariable != "_")
            {
                var binding = new SymbolInfo
                {
                    Name = matchCase.BindingVariable,
                    Kind = SymbolKind.Variable,
                    Type = !string.IsNullOrEmpty(matchCase.TypeAnnotation) ? matchCase.TypeAnnotation : "any",
                    Location = new SourceLocation
                    {
                        Uri = document.Uri,
                        Line = Math.Max(0, matchExpr.Position.Line - 1),
                        Column = Math.Max(0, matchExpr.Position.Column),
                        EndLine = Math.Max(0, matchExpr.Position.Line - 1),
                        EndColumn = Math.Max(0, matchExpr.Position.Column + matchCase.BindingVariable.Length)
                    }
                };
                caseScope.Declare(binding, recordDuplicateCheck: false);
                _readSymbols.Add(binding);
            }

            if (matchCase.TuplePattern != null)
            {
                DeclareTuplePatternBindings(matchCase.TuplePattern, caseScope, matchExpr.Position);
            }

            if (matchCase.GuardCondition != null)
            {
                AnalyzeExpression(matchCase.GuardCondition, caseScope, activeGenerics);
            }

            AnalyzeExpression(matchCase.ResultExpression, caseScope, activeGenerics);
        }
    }

    private void DeclareTuplePatternBindings(TuplePattern pattern, SymbolScope scope, SourcePosition fallbackPos)
    {
        foreach (var element in pattern.Elements)
        {
            if (!element.IsWildcard && !string.IsNullOrEmpty(element.Variable) && element.Variable != "_")
            {
                var binding = new SymbolInfo
                {
                    Name = element.Variable,
                    Kind = SymbolKind.Variable,
                    Type = "any",
                    Location = new SourceLocation
                    {
                        Uri = document.Uri,
                        Line = Math.Max(0, fallbackPos.Line - 1),
                        Column = Math.Max(0, fallbackPos.Column),
                        EndLine = Math.Max(0, fallbackPos.Line - 1),
                        EndColumn = Math.Max(0, fallbackPos.Column + element.Variable.Length)
                    }
                };
                scope.Declare(binding, recordDuplicateCheck: false);
                _readSymbols.Add(binding);
            }
        }
    }

    private void AnalyzeListComprehension(ListComprehension comp, SymbolScope scope, HashSet<string> activeGenerics)
    {
        AnalyzeExpression(comp.Iterable, scope, activeGenerics);

        var compScope = new SymbolScope
        {
            Kind = ScopeKind.Loop,
            Parent = scope,
            Range = scope.Range
        };

        var iterVar = new SymbolInfo
        {
            Name = comp.Variable.IdName,
            Kind = SymbolKind.Variable,
            Type = "any",
            Location = new SourceLocation
            {
                Uri = document.Uri,
                Line = Math.Max(0, comp.Variable.Position.Line - 1),
                Column = Math.Max(0, comp.Variable.Position.Column),
                EndLine = Math.Max(0, comp.Variable.Position.Line - 1),
                EndColumn = Math.Max(0, comp.Variable.Position.Column + comp.Variable.IdName.Length)
            }
        };
        compScope.Declare(iterVar, recordDuplicateCheck: false);
        _readSymbols.Add(iterVar);

        if (comp.Condition != null)
        {
            AnalyzeExpression(comp.Condition, compScope, activeGenerics);
        }

        if (comp.NestedLoops != null)
        {
            foreach (var nested in comp.NestedLoops)
            {
                AnalyzeListComprehension(nested, compScope, activeGenerics);
            }
        }

        AnalyzeExpression(comp.Expression, compScope, activeGenerics);
    }

    private SymbolInfo? AnalyzeDotOperation(Operation dotOp, SymbolScope scope, HashSet<string> activeGenerics)
    {
        // 分析左侧接收者
        AnalyzeExpression(dotOp.Left, scope, activeGenerics);

        // 提取右侧成员名及调用参数
        string? memberName = null;
        SourcePosition memberPos = dotOp.Position;
        List<LangExpression>? positionalArgs = null;
        List<NamedArgument>? namedArgs = null;
        var isCall = false;

        switch (dotOp.Right)
        {
            case LangId rightId:
                memberName = rightId.IdName;
                memberPos = rightId.Position.Line > 0 ? rightId.Position : dotOp.Position;
                break;

            case CallExpression rightCall:
                memberName = rightCall.Id.IdName;
                memberPos = rightCall.Id.Position.Line > 0 ? rightCall.Id.Position : rightCall.Position;
                positionalArgs = rightCall.Arguments;
                namedArgs = rightCall.NamedArguments;
                isCall = true;
                break;

            case Instance rightInst:
                memberName = rightInst.Id.IdName;
                memberPos = rightInst.Id.Position.Line > 0 ? rightInst.Id.Position : rightInst.Position;
                positionalArgs = rightInst.Ids;
                namedArgs = rightInst.NamedArgs;
                isCall = true;
                break;

            default:
                AnalyzeExpression(dotOp.Right, scope, activeGenerics);
                return null;
        }

        // 先分析成员调用的实参表达式
        if (positionalArgs != null)
        {
            foreach (var arg in positionalArgs)
            {
                AnalyzeExpression(arg, scope, activeGenerics);
            }
        }

        if (namedArgs != null)
        {
            foreach (var named in namedArgs)
            {
                AnalyzeExpression(named.Value, scope, activeGenerics);
            }
        }

        if (string.IsNullOrEmpty(memberName))
        {
            return null;
        }

        // 1. 若左侧是已导入的本地模块别名（import "./lib.old8" as lib），校验模块成员存在性
        if (dotOp.Left is LangId modId && _importedModules.TryGetValue(modId.IdName, out var moduleSymbols))
        {
            if (moduleSymbols.TryGetValue(memberName, out var exportedSymbol))
            {
                if (isCall && exportedSymbol.Kind == SymbolKind.Function && positionalArgs != null)
                {
                    ValidateCallArguments(exportedSymbol, positionalArgs, namedArgs ?? [], memberPos, scope, activeGenerics);
                }

                return exportedSymbol;
            }

            AddDiagnostic(
                DiagnosticSeverity.Error,
                $"模块 '{modId.IdName}' 不存在成员 '{memberName}'",
                memberPos);
            return null;
        }

        // 若左侧是标准库/内置命名空间，跳过静态成员校验
        if (dotOp.Left is LangId nsId &&
            (BuiltInRuntimeNamespaces.Contains(nsId.IdName) || _knownExternalModules.Contains(nsId.IdName)))
        {
            return null;
        }

        // 2. 推断接收者类型并查找用户定义类型（含继承、Mixin、接口、跨文件类）
        var receiverTypeName = dotOp.Left != null ? InferExpressionType(dotOp.Left, scope) : null;
        if (string.IsNullOrEmpty(receiverTypeName))
        {
            return null;
        }

        var targetType = TypeResolver.ResolveTypeSymbol(receiverTypeName, _combinedSymbols, workspaceIndex, document.Uri);
        if (targetType == null)
        {
            return null;
        }

        var isStaticAccess = dotOp.Left is LangId leftIdent && IsStaticTypeReference(leftIdent, scope);
        var isSuperAccess = dotOp.Left is SuperExpression;

        var found = TypeResolver.FindMember(targetType, memberName, _combinedSymbols, workspaceIndex, document.Uri);
        if (found == null)
        {
            // 允许所有对象上的通用内置方法（如 ToStr / GetType / Equals）
            if (!isStaticAccess && UniversalObjectMethods.Contains(memberName))
            {
                return null;
            }

            AddDiagnostic(
                DiagnosticSeverity.Error,
                $"类型 '{targetType.Name}' 不存在成员 '{memberName}'",
                memberPos);
            return null;
        }

        var (memberSymbol, declaringClass) = found.Value;

        // 校验静态访问 vs 实例成员
        if (isStaticAccess && !memberSymbol.IsStatic && targetType.Kind != SymbolKind.Enum)
        {
            AddDiagnostic(
                DiagnosticSeverity.Error,
                $"不能通过类型 '{targetType.Name}' 静态访问实例成员 '{memberName}'",
                memberPos);
        }

        // 校验访问修饰符（private / protected）
        var enclosingClass = scope.FindEnclosingClass();
        var accessLevel = TypeResolver.GetAccessLevel(
            enclosingClass, declaringClass, _combinedSymbols, workspaceIndex, document.Uri);
        if (isSuperAccess && accessLevel == MemberAccessLevel.Private)
        {
            accessLevel = MemberAccessLevel.Protected;
        }

        if (memberSymbol.AccessModifier == AccessModifier.Private && accessLevel != MemberAccessLevel.Private)
        {
            AddDiagnostic(
                DiagnosticSeverity.Error,
                $"无法访问类型 '{declaringClass.Name}' 的私有成员 '{memberName}'",
                memberPos);
        }
        else if (memberSymbol.AccessModifier == AccessModifier.Protected && accessLevel == MemberAccessLevel.External)
        {
            AddDiagnostic(
                DiagnosticSeverity.Error,
                $"无法访问类型 '{declaringClass.Name}' 的受保护成员 '{memberName}'",
                memberPos);
        }

        // 若是方法调用，校验实参数量与类型
        if (isCall && memberSymbol.Kind == SymbolKind.Method && positionalArgs != null)
        {
            ValidateCallArguments(memberSymbol, positionalArgs, namedArgs ?? [], memberPos, scope, activeGenerics);
        }

        return memberSymbol;
    }

    private void AnalyzeFunctionCallExpression(
        FunctionCallExpression funcCall,
        SymbolScope scope,
        HashSet<string> activeGenerics)
    {
        foreach (var arg in funcCall.Arguments)
        {
            AnalyzeExpression(arg, scope, activeGenerics);
        }

        if (funcCall.FunctionExpression is Operation { Opera: LangTokenType.Dot } dotOp)
        {
            var member = AnalyzeDotOperation(dotOp, scope, activeGenerics);
            if (member is { Kind: SymbolKind.Method })
            {
                ValidateCallArguments(member, funcCall.Arguments, [], funcCall.Position, scope, activeGenerics);
            }
            return;
        }

        if (funcCall.FunctionExpression is LangId funcId)
        {
            var resolved = AnalyzeIdentifierRead(funcId, scope, allowTypeOrFunction: true);
            if (resolved != null)
            {
                ValidateCallTarget(resolved, funcCall.Arguments, [], funcId.Position, scope, activeGenerics);
            }
            return;
        }

        AnalyzeExpression(funcCall.FunctionExpression, scope, activeGenerics);
    }

    private void AnalyzeDirectCall(
        LangId calleeId,
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs,
        SymbolScope scope,
        HashSet<string> activeGenerics)
    {
        foreach (var arg in positionalArgs)
        {
            AnalyzeExpression(arg, scope, activeGenerics);
        }

        foreach (var named in namedArgs)
        {
            AnalyzeExpression(named.Value, scope, activeGenerics);
        }

        var resolved = AnalyzeIdentifierRead(calleeId, scope, allowTypeOrFunction: true);
        if (resolved != null)
        {
            ValidateCallTarget(resolved, positionalArgs, namedArgs, calleeId.Position, scope, activeGenerics);
        }
    }

    private void ValidateCallTarget(
        SymbolInfo target,
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs,
        SourcePosition callPos,
        SymbolScope scope,
        HashSet<string> activeGenerics)
    {
        if (target.Kind is SymbolKind.Function or SymbolKind.Method)
        {
            ValidateCallArguments(target, positionalArgs, namedArgs, callPos, scope, activeGenerics);
            return;
        }

        if (target.Kind == SymbolKind.Class)
        {
            var initFound = TypeResolver.FindMember(target, "init", _combinedSymbols, workspaceIndex, document.Uri);
            if (initFound != null)
            {
                ValidateCallArguments(initFound.Value.Member, positionalArgs, namedArgs, callPos, scope, activeGenerics);
            }
        }
    }

    private void ValidateCallArguments(
        SymbolInfo callee,
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs,
        SourcePosition callPos,
        SymbolScope scope,
        HashSet<string> activeGenerics)
    {
        // 如果存在命名参数、泛型方法参数或同类重载方法，不做过严的位置实参校验，避免误报
        if (namedArgs.Count > 0)
        {
            return;
        }

        // 若该类存在同名重载方法（如多签名 init / 方法重载），跳过单一签名的参数个数校验
        if (callee.Parent != null && callee.DeclaringScope != null &&
            callee.DeclaringScope.AllDeclarations.Count(d => d.Name == callee.Name && d.Kind == callee.Kind) > 1)
        {
            return;
        }

        var parameters = callee.Parameters;
        var hasParams = parameters.Any(p => p.IsParams);
        var minArgs = parameters.Count(p => !p.HasDefaultValue && !p.IsParams);
        var maxArgs = hasParams ? int.MaxValue : parameters.Count;

        if (positionalArgs.Count < minArgs || positionalArgs.Count > maxArgs)
        {
            var expectedDesc = minArgs == maxArgs
                ? $"{minArgs}"
                : hasParams ? $"至少 {minArgs}" : $"{minArgs}..{maxArgs}";
            AddDiagnostic(
                DiagnosticSeverity.Error,
                $"参数数量不匹配: '{callee.Name}' 期望 {expectedDesc} 个参数，实际传入 {positionalArgs.Count} 个",
                callPos);
            return;
        }

        var calleeGenerics = ExtendGenerics(activeGenerics, callee.GenericParameters);
        if (callee.Parent != null)
        {
            calleeGenerics = ExtendGenerics(calleeGenerics, callee.Parent.GenericParameters);
        }

        for (var i = 0; i < positionalArgs.Count && i < parameters.Count; i++)
        {
            var param = parameters[i];
            if (param.IsParams || !param.HasExplicitTypeAnnotation || string.IsNullOrEmpty(param.Type))
            {
                continue;
            }

            var argExpr = positionalArgs[i];
            var argType = InferExpressionType(argExpr, scope);
            if (string.IsNullOrEmpty(argType))
            {
                continue;
            }

            if (!TypeResolver.IsAssignableTo(argType, param.Type, _combinedSymbols, workspaceIndex, document.Uri, calleeGenerics))
            {
                AddDiagnostic(
                    DiagnosticSeverity.Error,
                    $"参数类型不匹配: 参数 '{param.Name}' 期望 '{param.Type}'，实际传入 '{argType}'",
                    argExpr.Position.Line > 0 ? argExpr.Position : callPos);
            }
        }
    }

    private SymbolInfo? AnalyzeIdentifierRead(LangId id, SymbolScope scope, bool allowTypeOrFunction = false)
    {
        var name = id.IdName;
        if (string.IsNullOrWhiteSpace(name) ||
            name is "_" or "this" or "super" or "null" or "true" or "false")
        {
            return null;
        }

        var line = Math.Max(0, id.Position.Line - 1);
        var col = Math.Max(0, id.Position.Column);

        // 1. 沿词法作用域链查找
        var resolved = scope.Resolve(name, line, col);
        if (resolved != null)
        {
            _readSymbols.Add(resolved);
            return resolved;
        }

        // 2. 在类方法内检查继承自父类/Mixin/接口的裸成员
        var enclosingClass = scope.FindEnclosingClass();
        if (enclosingClass != null)
        {
            var inherited = TypeResolver.FindMember(
                enclosingClass, name, _combinedSymbols, workspaceIndex, document.Uri);
            if (inherited != null)
            {
                _readSymbols.Add(inherited.Value.Member);
                return inherited.Value.Member;
            }
        }

        // 3. 已导入的跨文件符号或模块
        if (_importedSymbols.TryGetValue(name, out var importedSymbol))
        {
            return importedSymbol;
        }

        if (_importedModules.ContainsKey(name) || _knownExternalModules.Contains(name))
        {
            return null;
        }

        // 4. 内置函数、内置类型、内置运行时命名空间
        if (_builtInFunctions.Contains(name) ||
            BuiltInTypes.Contains(name) ||
            BuiltInRuntimeNamespaces.Contains(name))
        {
            return null;
        }

        // 5. 工作区其他文件的顶层符号（跨文件语义分析）
        if (workspaceIndex != null &&
            workspaceIndex.TryResolveSymbol(name, document.Uri, out var workspaceSymbol, out _) &&
            workspaceSymbol != null)
        {
            return workspaceSymbol;
        }

        AddDiagnostic(
            DiagnosticSeverity.Error,
            $"未定义的符号 '{name}'",
            id.Position);

        return null;
    }

    private void CheckUnusedLocalVariables(SymbolScope rootScope)
    {
        foreach (var scope in rootScope.EnumerateAllScopes())
        {
            if (scope.Kind is ScopeKind.Global or ScopeKind.Class)
            {
                continue;
            }

            foreach (var symbol in scope.Symbols.Values)
            {
                if (symbol.Kind != SymbolKind.Variable)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(symbol.Name) || symbol.Name.StartsWith('_'))
                {
                    continue;
                }

                if (!_readSymbols.Contains(symbol))
                {
                    AddDiagnostic(
                        DiagnosticSeverity.Warning,
                        $"未使用的变量 '{symbol.Name}'",
                        new SourcePosition(symbol.Location.Line + 1, symbol.Location.Column));
                }
            }
        }
    }

    private string? InferExpressionType(LangExpression expr, SymbolScope scope)
    {
        // 支持导入模块别名成员调用的返回类型推断（如 lib.createUser()）
        if (expr is Operation { Opera: LangTokenType.Dot, Left: LangId modId } dotOp &&
            _importedModules.TryGetValue(modId.IdName, out var moduleSymbols))
        {
            var memberName = dotOp.Right switch
            {
                LangId id => id.IdName,
                CallExpression call => call.Id.IdName,
                Instance inst => inst.Id.IdName,
                _ => null
            };
            if (memberName != null && moduleSymbols.TryGetValue(memberName, out var exported))
            {
                return exported.Kind == SymbolKind.Function
                    ? TypeResolver.ExtractReturnType(exported.Type)
                    : TypeResolver.NormalizeTypeName(exported.Type);
            }
        }

        return TypeResolver.InferExpressionType(expr, scope, _combinedSymbols, workspaceIndex, document.Uri);
    }

    private bool IsStaticTypeReference(LangId id, SymbolScope scope)
    {
        var line = Math.Max(0, id.Position.Line - 1);
        var col = Math.Max(0, id.Position.Column);
        var sym = scope.Resolve(id.IdName, line, col);
        if (sym != null)
        {
            return TypeResolver.IsTypeSymbol(sym);
        }

        if (_importedSymbols.TryGetValue(id.IdName, out var imported))
        {
            return TypeResolver.IsTypeSymbol(imported);
        }

        if (workspaceIndex != null &&
            workspaceIndex.TryResolveSymbol(id.IdName, document.Uri, out var wsSym, out _) &&
            wsSym != null)
        {
            return TypeResolver.IsTypeSymbol(wsSym);
        }

        return false;
    }

    private static SymbolScope? FindChildScopeForOwner(SymbolScope parent, ScopeKind kind, SymbolInfo? owner)
    {
        if (owner == null)
        {
            return null;
        }

        return parent.Children.FirstOrDefault(c => c.Kind == kind && ReferenceEquals(c.OwnerSymbol, owner));
    }

    private static SymbolScope FindChildScopeAt(SymbolScope parent, SourcePosition pos, ScopeKind? preferredKind = null)
    {
        if (pos.Line <= 0)
        {
            return parent;
        }

        var line = pos.Line - 1;
        var col = Math.Max(0, pos.Column);

        if (preferredKind.HasValue)
        {
            var exact = parent.Children.FirstOrDefault(c =>
                c.Kind == preferredKind.Value && c.ContainsPosition(line, col));
            if (exact != null)
            {
                return exact;
            }
        }

        var matched = parent.Children.FirstOrDefault(c => c.ContainsPosition(line, col));
        return matched ?? parent;
    }

    private static HashSet<string> ExtendGenerics(HashSet<string> current, IEnumerable<string>? additional)
    {
        if (additional == null)
        {
            return current;
        }

        var list = additional as IList<string> ?? additional.ToList();
        if (list.Count == 0)
        {
            return current;
        }

        var next = new HashSet<string>(current, StringComparer.Ordinal);
        foreach (var g in list)
        {
            next.Add(g);
        }

        return next;
    }

    private static bool IsSyntheticNullInitializer(SourcePosition idPosition, LangExpression valueExpr)
    {
        return valueExpr is ScalarLiteralExpression { Kind: LiteralKind.Null } &&
               valueExpr.Position.Line == idPosition.Line &&
               valueExpr.Position.Column == idPosition.Column;
    }

    private static bool IsStandardOrBuiltinModule(string moduleName)
    {
        return BuiltInRuntimeNamespaces.Contains(moduleName) ||
               StandardLibraryRegistry.GetLibraryInfo(moduleName) != null;
    }

    private static bool LooksLikeLocalFileImport(string moduleName)
    {
        return moduleName.StartsWith("./", StringComparison.Ordinal) ||
               moduleName.StartsWith("../", StringComparison.Ordinal) ||
               moduleName.EndsWith(".old8", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetExistingDocumentDirectory(string uri, out string filePath)
    {
        filePath = string.Empty;
        try
        {
            if (!uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var path = new Uri(uri).LocalPath;
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return false;
            }

            filePath = path;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void AddDiagnostic(DiagnosticSeverity severity, string message, SourcePosition position)
    {
        _diagnostics.Add(new DiagnosticInfo
        {
            Severity = severity,
            Message = message,
            Line = Math.Max(1, position.Line),
            Column = Math.Max(0, position.Column),
            Source = DiagnosticSource
        });
    }
}
