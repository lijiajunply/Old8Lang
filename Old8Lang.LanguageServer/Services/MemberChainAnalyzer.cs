using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SymbolInfo = Old8Lang.LanguageServer.Models.SymbolInfo;
using SymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 成员链分析结果：包含最终目标类型、是否为通过类名发起的静态访问、是否为 super 访问，以及调用点所在的类上下文。
/// </summary>
public sealed record MemberChainResult(
    SymbolInfo TargetType,
    bool IsStaticAccess,
    SymbolInfo? EnclosingClass,
    bool IsSuperAccess = false);

/// <summary>
/// 成员链分析器 - 负责解析和推断成员访问链的类型。
/// 支持多级属性链、方法调用链、继承/Mixin/接口成员、静态访问、this/super 及作用域内局部变量与参数。
/// </summary>
public class MemberChainAnalyzer(
    List<LangToken> tokens,
    Dictionary<string, SymbolInfo> symbolTable,
    Position position,
    SymbolScope? rootScope = null,
    WorkspaceIndex? workspaceIndex = null,
    string? documentUri = null,
    TokenLineIndex? tokenIndex = null)
{
    public MemberChainAnalyzer(
        DocumentParseResult document,
        Position position,
        WorkspaceIndex? workspaceIndex = null)
        : this(
            document.Tokens ?? [],
            document.SymbolTable ?? new Dictionary<string, SymbolInfo>(),
            position,
            document.RootScope,
            workspaceIndex,
            document.Uri,
            document.TokenIndex)
    {
    }

    /// <summary>
    /// 分析光标位置的成员链并返回最终类型符号（向后兼容入口）。
    /// </summary>
    public SymbolInfo? AnalyzeChain() => AnalyzeChainDetailed()?.TargetType;

    /// <summary>
    /// 分析光标位置的成员链并返回完整上下文（与 <see cref="AnalyzeChainDetailed"/> 等价）。
    /// </summary>
    public MemberChainResult? AnalyzeChainWithContext() => AnalyzeChainDetailed();

    /// <summary>
    /// 分析以指定点号索引结尾的成员链并返回完整上下文。
    /// </summary>
    public MemberChainResult? AnalyzeChainAtDotIndex(int dotIndex)
    {
        if (dotIndex < 0 || dotIndex >= tokens.Count || tokens[dotIndex].Type != LangTokenType.Dot)
        {
            return null;
        }

        var chainTokens = ExtractChainTokens(dotIndex);
        if (chainTokens.Count == 0)
        {
            return null;
        }

        var dotToken = tokens[dotIndex];
        return InferChainType(chainTokens, dotToken.Line - 1, dotToken.Column);
    }

    /// <summary>
    /// 分析光标位置的成员链并返回完整上下文（目标类型、静态访问标记、所在类）。
    /// </summary>
    public MemberChainResult? AnalyzeChainDetailed()
    {
        var line = position.Line + 1; // LSP 行号从 0 开始，token 行号从 1 开始
        var column = position.Character; // 两者的列号都是 0 起始，无需转换

        var dotIndex = FindDotAtPosition(line, column);
        if (dotIndex == -1)
        {
            return null;
        }

        return AnalyzeChainAtDotIndex(dotIndex);
    }

    /// <summary>
    /// 找到光标位置之前最近且与光标相连的点号 token 索引。
    /// </summary>
    private int FindDotAtPosition(int line, int column)
    {
        // 优先通过按行索引 O(1) 定位光标之前最近的 token，回退到线性扫描（手工传入 List<LangToken> 时）
        int cursorTokenIndex;
        if (tokenIndex != null)
        {
            cursorTokenIndex = tokenIndex.FindLastTokenAtOrBefore(line - 1, column);
        }
        else
        {
            cursorTokenIndex = -1;
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                var token = tokens[i];
                if (token.Line > line || (token.Line == line && token.Column > column))
                {
                    continue;
                }

                cursorTokenIndex = i;
                break;
            }
        }

        if (cursorTokenIndex < 0)
        {
            return -1;
        }

        // 1. 光标就在点号上或紧跟在点号后面：obj.|
        if (tokens[cursorTokenIndex].Type == LangTokenType.Dot && tokens[cursorTokenIndex].Line == line)
        {
            return cursorTokenIndex;
        }

        // 2. 光标正在同一行输入成员名前缀：obj.get| 或 obj.$1get
        if (tokens[cursorTokenIndex].Line == line)
        {
            if (tokens[cursorTokenIndex].Type == LangTokenType.Identifier && cursorTokenIndex > 0)
            {
                var prev = SkipTestMarkersBackward(cursorTokenIndex - 1);
                if (prev >= 0 && tokens[prev].Type == LangTokenType.Dot && tokens[prev].Line == line)
                {
                    return prev;
                }
            }

            if (IsTestMarkerToken(cursorTokenIndex))
            {
                var prev = SkipTestMarkersBackward(cursorTokenIndex);
                if (prev >= 0 && tokens[prev].Type == LangTokenType.Dot && tokens[prev].Line == line)
                {
                    return prev;
                }
            }

            // 同行直接紧跟在点号后方的调用（如 obj.$1getValue()）
            for (int i = cursorTokenIndex; i >= 0 && tokens[i].Line == line; i--)
            {
                if (tokens[i].Type == LangTokenType.Dot)
                {
                    return i;
                }
            }
        }

        // 3. 兼容带 $1 占位标记的历史补全测试（光标行号可能落在源码末尾之后）
        for (int i = cursorTokenIndex; i >= 1; i--)
        {
            if (tokens[i].Type == LangTokenType.Dollar && tokens[i - 1].Type == LangTokenType.Dot)
            {
                return i - 1;
            }
        }

        return -1;
    }

    private int SkipTestMarkersBackward(int index)
    {
        while (index >= 0 && IsTestMarkerToken(index))
        {
            index--;
        }

        return index;
    }

    private bool IsTestMarkerToken(int index)
    {
        var token = tokens[index];
        return token.Type == LangTokenType.Dollar ||
               (token.Type == LangTokenType.Number && index > 0 && tokens[index - 1].Type == LangTokenType.Dollar);
    }

    /// <summary>
    /// 提取成员链的所有 token（从起点到目标点号）。
    /// 例如: obj.getB().getC(). 会提取 [obj, ., getB, (, ), ., getC, (, ), .]
    /// </summary>
    private List<LangToken> ExtractChainTokens(int dotIndex)
    {
        var chainTokens = new List<LangToken>();
        var currentIndex = dotIndex;
        var parenDepth = 0;

        while (currentIndex >= 0)
        {
            var token = tokens[currentIndex];

            if (IsTestMarkerToken(currentIndex))
            {
                currentIndex--;
                continue;
            }

            if (token.Type == LangTokenType.RightParen)
            {
                parenDepth++;
                chainTokens.Insert(0, token);
                currentIndex--;
                continue;
            }

            if (token.Type == LangTokenType.LeftParen)
            {
                if (parenDepth == 0)
                {
                    // 到达包围整个表达式的左括号，例如 getFullName(p.name) 中的 '('
                    break;
                }

                parenDepth--;
                chainTokens.Insert(0, token);
                currentIndex--;
                continue;
            }

            if (parenDepth > 0)
            {
                // 括号内参数列表原样保留
                chainTokens.Insert(0, token);
                currentIndex--;
                continue;
            }

            if (!IsChainToken(token.Type))
            {
                break;
            }

            chainTokens.Insert(0, token);
            currentIndex--;

            if (token.Type is LangTokenType.Identifier or LangTokenType.This or LangTokenType.Super)
            {
                var prevIndex = SkipTestMarkersBackward(currentIndex);
                if (prevIndex < 0 || tokens[prevIndex].Type != LangTokenType.Dot)
                {
                    return chainTokens;
                }
            }
        }

        return chainTokens;
    }

    private static bool IsChainToken(LangTokenType type)
    {
        return type is LangTokenType.Identifier
            or LangTokenType.This
            or LangTokenType.Super
            or LangTokenType.Dot
            or LangTokenType.LeftParen
            or LangTokenType.RightParen
            or LangTokenType.Comma
            or LangTokenType.String
            or LangTokenType.Number
            or LangTokenType.True
            or LangTokenType.False;
    }

    private MemberChainResult? InferChainType(List<LangToken> chainTokens, int scopeLine, int scopeColumn)
    {
        if (chainTokens.Count == 0)
        {
            return null;
        }

        var scope = rootScope?.FindInnermostScope(scopeLine, scopeColumn);
        var enclosingClass = scope?.FindEnclosingClass();

        SymbolInfo? currentType;
        bool isStaticAccess = false;
        bool isSuperAccess = false;
        int nextIndex = 1;

        var firstToken = chainTokens[0];
        if (firstToken.Type == LangTokenType.This)
        {
            currentType = enclosingClass;
            if (currentType == null)
            {
                return null;
            }
        }
        else if (firstToken.Type == LangTokenType.Super)
        {
            if (enclosingClass == null || string.IsNullOrEmpty(enclosingClass.BaseClassName))
            {
                return null;
            }

            currentType = TypeResolver.ResolveTypeSymbol(
                enclosingClass.BaseClassName, symbolTable, workspaceIndex, documentUri);
            if (currentType == null)
            {
                return null;
            }

            isSuperAccess = true;
        }
        else if (firstToken.Type == LangTokenType.Identifier)
        {
            var rootName = firstToken.Value;
            var rootSymbol = ResolveRootSymbol(rootName, scope, scopeLine, scopeColumn);
            if (rootSymbol == null)
            {
                return null;
            }

            var isRootCall = nextIndex < chainTokens.Count && chainTokens[nextIndex].Type == LangTokenType.LeftParen;
            if (isRootCall)
            {
                nextIndex = SkipMethodArguments(chainTokens, nextIndex);
                if (TypeResolver.IsTypeDeclaration(rootSymbol))
                {
                    // 构造函数调用：User().
                    currentType = rootSymbol;
                    isStaticAccess = false;
                }
                else if (rootSymbol.Kind is SymbolKind.Function or SymbolKind.Method)
                {
                    var retType = TypeResolver.ExtractRawReturnType(rootSymbol.Type);
                    currentType = TypeResolver.ResolveTypeSymbol(retType, symbolTable, workspaceIndex, documentUri);
                    isStaticAccess = false;
                }
                else
                {
                    return null;
                }
            }
            else if (TypeResolver.IsTypeDeclaration(rootSymbol))
            {
                // 直接通过类名/枚举名访问静态成员：MathUtil.
                currentType = rootSymbol;
                isStaticAccess = true;
            }
            else
            {
                currentType = GetSymbolType(rootSymbol);
                isStaticAccess = false;
            }

            if (currentType == null)
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        // 逐步遍历成员访问链
        int i = nextIndex;
        while (i < chainTokens.Count)
        {
            var token = chainTokens[i];
            if (token.Type == LangTokenType.Dot)
            {
                i++;
                continue;
            }

            if (token.Type == LangTokenType.Identifier)
            {
                var memberName = token.Value;
                var resolvedMember = TypeResolver.FindMember(
                    currentType, memberName, symbolTable, workspaceIndex, documentUri);
                if (resolvedMember == null)
                {
                    return null;
                }

                var member = resolvedMember.Value.Member;
                bool isMethodCall = i + 1 < chainTokens.Count &&
                                    chainTokens[i + 1].Type == LangTokenType.LeftParen;

                if (isMethodCall)
                {
                    i = SkipMethodArguments(chainTokens, i + 1);
                    var returnType = TypeResolver.ExtractRawReturnType(member.Type);
                    if (string.IsNullOrEmpty(returnType) || returnType == "void")
                    {
                        return null;
                    }

                    currentType = TypeResolver.ResolveTypeSymbol(returnType, symbolTable, workspaceIndex, documentUri);
                    if (currentType == null)
                    {
                        return null;
                    }

                    isStaticAccess = false;
                    isSuperAccess = false;
                }
                else
                {
                    var propertyType = TypeResolver.ExtractRawReturnType(member.Type);
                    if (string.IsNullOrEmpty(propertyType))
                    {
                        return null;
                    }

                    currentType = TypeResolver.ResolveTypeSymbol(propertyType, symbolTable, workspaceIndex, documentUri);
                    if (currentType == null)
                    {
                        return null;
                    }

                    isStaticAccess = false;
                    isSuperAccess = false;
                    i++;
                }
            }
            else
            {
                i++;
            }
        }

        return new MemberChainResult(currentType, isStaticAccess, enclosingClass, isSuperAccess);
    }

    private SymbolInfo? ResolveRootSymbol(string name, SymbolScope? scope, int scopeLine, int scopeColumn)
    {
        if (scope != null)
        {
            var scoped = scope.Resolve(name, scopeLine, scopeColumn);
            if (scoped != null)
            {
                return scoped;
            }
        }

        if (symbolTable.TryGetValue(name, out var local))
        {
            return local;
        }

        if (workspaceIndex != null &&
            workspaceIndex.TryResolveSymbol(name, documentUri, out var wsSymbol, out _) &&
            wsSymbol != null)
        {
            return wsSymbol;
        }

        return null;
    }

    private static int SkipMethodArguments(List<LangToken> chainTokens, int startIndex)
    {
        if (startIndex >= chainTokens.Count || chainTokens[startIndex].Type != LangTokenType.LeftParen)
        {
            return startIndex;
        }

        int depth = 1;
        int i = startIndex + 1;
        while (i < chainTokens.Count && depth > 0)
        {
            if (chainTokens[i].Type == LangTokenType.LeftParen)
            {
                depth++;
            }
            else if (chainTokens[i].Type == LangTokenType.RightParen)
            {
                depth--;
            }

            i++;
        }

        return i;
    }

    private SymbolInfo? GetSymbolType(SymbolInfo symbol)
    {
        if (TypeResolver.IsTypeDeclaration(symbol))
        {
            return symbol;
        }

        if (!string.IsNullOrEmpty(symbol.Type))
        {
            return TypeResolver.ResolveTypeSymbol(symbol.Type, symbolTable, workspaceIndex, documentUri);
        }

        return null;
    }
}
