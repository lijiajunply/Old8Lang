using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 符号查找器 - 根据位置查找符号
/// </summary>
public class SymbolFinder
{
    /// <summary>
    /// 根据位置查找符号
    /// </summary>
    /// <param name="document">文档解析结果</param>
    /// <param name="line">行号（从0开始）</param>
    /// <param name="column">列号（从0开始）</param>
    /// <returns>找到的符号信息，如果没找到则返回null</returns>
    public static SymbolInfo? FindSymbolAtPosition(DocumentParseResult document, int line, int column)
    {
        if (document.SymbolTable == null || document.Tokens == null)
        {
            return null;
        }

        // 1. 找到光标位置的token
        var tokenAtPosition = FindTokenAtPosition(document.Tokens, line, column);
        if (tokenAtPosition == null || tokenAtPosition.Value.Type != LangTokenType.Identifier)
        {
            return null;
        }

        var symbolName = tokenAtPosition.Value.Value;
        var tokenIndex = document.Tokens.IndexOf(tokenAtPosition.Value);

        // 2. 检查是否是成员访问（obj.member）
        if (tokenIndex > 1 && document.Tokens[tokenIndex - 1].Type == LangTokenType.Dot)
        {
            // 这是成员访问，查找所属类
            var objectToken = document.Tokens[tokenIndex - 2];
            if (objectToken.Type == LangTokenType.Identifier)
            {
                // 先查找对象的类型
                if (document.SymbolTable.TryGetValue(objectToken.Value, out var objectSymbol))
                {
                    // 如果对象是一个类，查找其成员
                    if (objectSymbol.Kind == SymbolKind.Class)
                    {
                        if (objectSymbol.Members.TryGetValue(symbolName, out var memberSymbol))
                        {
                            return memberSymbol;
                        }
                    }
                }
            }
        }

        // 3. 在符号表中查找该标识符（全局符号）
        if (document.SymbolTable.TryGetValue(symbolName, out var symbol))
        {
            return symbol;
        }

        return null;
    }

    /// <summary>
    /// 根据位置查找token
    /// </summary>
    private static LangToken? FindTokenAtPosition(List<LangToken> tokens, int line, int column)
    {
        // LSP的行列号从0开始，LangToken的从1开始
        var targetLine = line + 1;
        var targetColumn = column + 1;

        foreach (var token in tokens)
        {
            // 检查token是否在目标位置
            if (token.Line == targetLine)
            {
                // 计算token的结束列
                var tokenEndColumn = token.Column + token.Value.Length;

                if (targetColumn >= token.Column && targetColumn <= tokenEndColumn)
                {
                    return token;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 获取符号的引用位置
    /// </summary>
    public static List<SourceLocation> FindReferences(DocumentParseResult document, string symbolName)
    {
        var references = new List<SourceLocation>();

        if (document.Tokens == null)
        {
            return references;
        }

        // 遍历所有token，查找符号的引用
        foreach (var token in document.Tokens)
        {
            if (token.Type == LangTokenType.Identifier && token.Value == symbolName)
            {
                references.Add(new SourceLocation
                {
                    Uri = document.Uri,
                    Line = token.Line - 1, // 转换为从0开始
                    Column = token.Column - 1, // 转换为从0开始
                    EndLine = token.Line - 1,
                    EndColumn = token.Column + token.Value.Length - 1
                });
            }
        }

        return references;
    }
}
