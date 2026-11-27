using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SymbolInfo = Old8Lang.LanguageServer.Models.SymbolInfo;
using SymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 符号查找器 - 基于词法作用域链和成员类型推断查找位置处的符号与作用域感知引用。
/// </summary>
public class SymbolFinder
{
    /// <summary>
    /// 根据 LSP 0-based 位置查找符号（支持作用域链、变量遮蔽、成员链、继承成员、this/super 与跨文件类）。
    /// </summary>
    public static SymbolInfo? FindSymbolAtPosition(
        DocumentParseResult document,
        int line,
        int column,
        WorkspaceIndex? workspaceIndex = null)
    {
        if (document.SymbolTable == null || document.Tokens == null)
        {
            return null;
        }

        var tokenMatch = document.TokenIndex.FindTokenAtPosition(line, column);
        if (tokenMatch == null)
        {
            return null;
        }

        var (token, tokenIndex) = tokenMatch.Value;
        var scope = document.RootScope?.FindInnermostScope(line, column);

        // 支持对 this / super 悬停与跳转
        if (token.Type == LangTokenType.This)
        {
            return scope?.FindEnclosingClass();
        }

        if (token.Type == LangTokenType.Super)
        {
            var enclosingClass = scope?.FindEnclosingClass();
            if (enclosingClass != null && !string.IsNullOrEmpty(enclosingClass.BaseClassName))
            {
                return TypeResolver.ResolveTypeSymbol(
                    enclosingClass.BaseClassName, document.SymbolTable, workspaceIndex, document.Uri);
            }

            return null;
        }

        if (token.Type != LangTokenType.Identifier)
        {
            return null;
        }

        var symbolName = token.Value;

        // 1. 成员访问（obj.member、this.member、super.member、ClassName.staticMember、a.b.c）
        if (tokenIndex > 0 && document.Tokens[tokenIndex - 1].Type == LangTokenType.Dot)
        {
            var chainAnalyzer = new MemberChainAnalyzer(
                document,
                new Position(token.Line - 1, token.Column),
                workspaceIndex);

            var chainResult = chainAnalyzer.AnalyzeChainAtDotIndex(tokenIndex - 1);
            if (chainResult != null)
            {
                var resolvedMember = TypeResolver.FindMember(
                    chainResult.TargetType,
                    symbolName,
                    document.SymbolTable,
                    workspaceIndex,
                    document.Uri);
                if (resolvedMember != null)
                {
                    return resolvedMember.Value.Member;
                }
            }

            // 兼容动态类型对象（如 var/any）的单层兜底：扫描本文件类成员
            if (tokenIndex > 1 && document.Tokens[tokenIndex - 2].Type == LangTokenType.Identifier)
            {
                var objectName = document.Tokens[tokenIndex - 2].Value;
                var objectSym = scope?.Resolve(objectName, line, column)
                    ?? (document.SymbolTable.TryGetValue(objectName, out var s) ? s : null);

                if (objectSym is { Kind: SymbolKind.Variable or SymbolKind.Parameter } &&
                    TypeResolver.IsDynamicType(objectSym.Type))
                {
                    foreach (var kvp in document.SymbolTable)
                    {
                        if (TypeResolver.IsTypeDeclaration(kvp.Value) &&
                            kvp.Value.Members.TryGetValue(symbolName, out var memberSymbol))
                        {
                            return memberSymbol;
                        }
                    }
                }
            }

            return null;
        }

        // 2. 基于作用域链解析（优先命中本层参数、局部变量、类自身成员，正确处理变量遮蔽）
        if (scope != null)
        {
            // 若光标正好落在当前作用域内某个成员或声明自身上（例如类定义行/方法定义行）
            var exactDecl = FindDeclarationAtExactPosition(document.RootScope!, line, column, symbolName);
            if (exactDecl != null)
            {
                return exactDecl;
            }

            var resolved = scope.Resolve(symbolName, line, column);
            if (resolved != null)
            {
                return resolved;
            }

            // 在类方法内通过裸名称访问继承来的父类/Mixin成员
            var enclosingClass = scope.FindEnclosingClass();
            if (enclosingClass != null)
            {
                var inherited = TypeResolver.FindMember(
                    enclosingClass, symbolName, document.SymbolTable, workspaceIndex, document.Uri);
                if (inherited != null)
                {
                    return inherited.Value.Member;
                }
            }
        }

        // 3. 回退到全局符号表（兼容未构建 RootScope 的手工构造测试文档）
        if (document.SymbolTable.TryGetValue(symbolName, out var symbol))
        {
            return symbol;
        }

        // 4. 在没有 RootScope 时的参数与类成员兜底
        if (document.RootScope == null)
        {
            foreach (var kvp in document.SymbolTable)
            {
                if (kvp.Value.Kind is SymbolKind.Function or SymbolKind.Method)
                {
                    foreach (var param in kvp.Value.Parameters)
                    {
                        if (param.Name == symbolName)
                        {
                            return param;
                        }
                    }
                }
            }

            foreach (var kvp in document.SymbolTable)
            {
                if (TypeResolver.IsTypeDeclaration(kvp.Value))
                {
                    if (kvp.Value.Members.TryGetValue(symbolName, out var memberSymbol))
                    {
                        return memberSymbol;
                    }

                    foreach (var member in kvp.Value.Members.Values)
                    {
                        if (member.Kind == SymbolKind.Method)
                        {
                            foreach (var param in member.Parameters)
                            {
                                if (param.Name == symbolName)
                                {
                                    return param;
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 作用域感知引用查找：仅返回真正绑定到 <paramref name="targetSymbol"/> 的出现位置，
    /// 正确隔离局部变量、参数与被遮蔽的同名符号。
    /// </summary>
    public static List<SourceLocation> FindScopedReferences(
        DocumentParseResult document,
        SymbolInfo targetSymbol,
        WorkspaceIndex? workspaceIndex = null)
    {
        if (document.Tokens == null)
        {
            return [];
        }

        if (document.RootScope == null)
        {
            return FindReferences(document, targetSymbol.Name);
        }

        var references = new List<SourceLocation>();
        var targetName = targetSymbol.Name;

        for (int i = 0; i < document.Tokens.Count; i++)
        {
            var token = document.Tokens[i];
            if (token.Type != LangTokenType.Identifier || token.Value != targetName)
            {
                continue;
            }

            var line = token.Line - 1;
            var column = token.Column;

            // 命中目标符号的定义本身
            if (IsSameLocation(targetSymbol.Location, document.Uri, line, column))
            {
                references.Add(TokenToLocation(document.Uri, token));
                continue;
            }

            var isMemberAccess = i > 0 && document.Tokens[i - 1].Type == LangTokenType.Dot;

            // 类成员（方法/属性/字段/枚举成员）
            if (targetSymbol.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.EnumMember)
            {
                if (isMemberAccess)
                {
                    var resolved = FindSymbolAtPosition(document, line, column, workspaceIndex);
                    if (resolved != null && IsSameSymbol(resolved, targetSymbol))
                    {
                        references.Add(TokenToLocation(document.Uri, token));
                    }
                }
                else
                {
                    var resolved = FindSymbolAtPosition(document, line, column, workspaceIndex);
                    if (resolved != null && IsSameSymbol(resolved, targetSymbol))
                    {
                        references.Add(TokenToLocation(document.Uri, token));
                    }
                }

                continue;
            }

            // 非成员符号不能匹配 `.member`
            if (isMemberAccess)
            {
                continue;
            }

            // 跳过声明语法里的参数/成员定义位置（除非前面已作为定义本身加入）
            var resolvedAtPos = FindSymbolAtPosition(document, line, column, workspaceIndex);
            if (resolvedAtPos != null && IsSameSymbol(resolvedAtPos, targetSymbol))
            {
                references.Add(TokenToLocation(document.Uri, token));
            }
        }

        return references;
    }

    /// <summary>
    /// 判断两个 SymbolInfo 是否指向同一个符号定义
    /// </summary>
    public static bool IsSameSymbol(SymbolInfo left, SymbolInfo right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        return left.Name == right.Name &&
               left.Kind == right.Kind &&
               left.Location.Uri == right.Location.Uri &&
               left.Location.Line == right.Location.Line &&
               left.Location.Column == right.Location.Column;
    }

    /// <summary>
    /// 根据 LSP 位置查找 token（行列号均从 0 开始，使用文档按行索引实现 O(1) 行定位）。
    /// </summary>
    public static LangToken? FindTokenAtPosition(DocumentParseResult document, int line, int column)
    {
        return document.TokenIndex.FindTokenAtPosition(line, column)?.Token;
    }

    /// <summary>
    /// 根据 LSP 位置查找 token（行列号均从 0 开始）
    /// </summary>
    public static LangToken? FindTokenAtPosition(List<LangToken> tokens, int line, int column, string? sourceCode = null)
    {
        var targetLineToken = line + 1;
        var targetColumnToken = column;

        foreach (var token in tokens)
        {
            if (token.Line == targetLineToken)
            {
                var tokenEndColumn = token.Column + token.Value.Length - 1;
                if (targetColumnToken >= token.Column && targetColumnToken <= tokenEndColumn)
                {
                    return token;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 获取光标位置处的标识符名称，不要求该标识符已出现在符号表中
    /// </summary>
    public static string? FindIdentifierNameAtPosition(DocumentParseResult document, int line, int column)
    {
        if (document.Tokens == null)
        {
            return null;
        }

        var token = FindTokenAtPosition(document, line, column);
        return token is { Type: LangTokenType.Identifier } ? token.Value.Value : null;
    }

    /// <summary>
    /// 获取符号的引用位置（按名称的文本级查找，保留兼容）
    /// </summary>
    public static List<SourceLocation> FindReferences(DocumentParseResult document, string symbolName)
    {
        var references = new List<SourceLocation>();

        if (document.Tokens == null)
        {
            return references;
        }

        foreach (var token in document.Tokens)
        {
            if (token.Type == LangTokenType.Identifier && token.Value == symbolName)
            {
                references.Add(TokenToLocation(document.Uri, token));
            }
        }

        return references;
    }

    private static SymbolInfo? FindDeclarationAtExactPosition(SymbolScope rootScope, int line, int column, string name)
    {
        foreach (var scope in rootScope.EnumerateAllScopes())
        {
            foreach (var decl in scope.AllDeclarations)
            {
                if (decl.Name == name &&
                    decl.Location.Line == line &&
                    column >= decl.Location.Column &&
                    column <= Math.Max(decl.Location.Column, decl.Location.EndColumn - 1))
                {
                    return decl;
                }
            }

            foreach (var sym in scope.Symbols.Values)
            {
                if (sym.Name == name &&
                    sym.Location.Line == line &&
                    column >= sym.Location.Column &&
                    column <= Math.Max(sym.Location.Column, sym.Location.EndColumn - 1))
                {
                    return sym;
                }
            }
        }

        return null;
    }

    private static int FindTokenIndex(List<LangToken> tokens, LangToken target)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Line == target.Line &&
                t.Column == target.Column &&
                t.Value == target.Value &&
                t.Type == target.Type)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsSameLocation(SourceLocation loc, string uri, int line, int column) =>
        loc.Uri == uri && loc.Line == line && loc.Column == column;

    private static SourceLocation TokenToLocation(string uri, LangToken token) => new()
    {
        Uri = uri,
        Line = token.Line - 1,
        Column = token.Column,
        EndLine = token.Line - 1,
        EndColumn = token.Column + token.Value.Length
    };
}
