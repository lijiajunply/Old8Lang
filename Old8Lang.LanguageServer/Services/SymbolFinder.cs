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
        var tokenAtPosition = FindTokenAtPosition(document.Tokens, line, column, document.Text);
        if (tokenAtPosition == null || tokenAtPosition.Value.Type != LangTokenType.Identifier)
        {
            System.Diagnostics.Debug.WriteLine($"No identifier token found at ({line},{column})");
            return null;
        }

        var symbolName = tokenAtPosition.Value.Value;

        // 找到token在列表中的索引
        // 注意:不能使用 IndexOf,因为可能有多个同名token
        // 需要根据行号和位置精确匹配
        var tokenIndex = -1;
        for (int i = 0; i < document.Tokens.Count; i++)
        {
            var t = document.Tokens[i];
            if (t.Line == tokenAtPosition.Value.Line &&
                t.Column == tokenAtPosition.Value.Column &&
                t.Value == tokenAtPosition.Value.Value &&
                t.Type == tokenAtPosition.Value.Type)
            {
                tokenIndex = i;
                break;
            }
        }

        if (tokenIndex == -1)
        {
            System.Diagnostics.Debug.WriteLine($"Token not found in list");
            return null;
        }

        // Debug: 调试信息
        System.Diagnostics.Debug.WriteLine($"FindSymbolAtPosition: Line={line}, Column={column}, FoundToken='{symbolName}', TokenType={tokenAtPosition.Value.Type}");


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
                    
                    // 如果对象是变量，尝试推断其类型并查找对应类的成员
                    if (objectSymbol.Kind == SymbolKind.Variable && !string.IsNullOrEmpty(objectSymbol.Type))
                    {
                        // 如果变量的类型是类名，查找该类
                        if (document.SymbolTable.TryGetValue(objectSymbol.Type, out var classSymbol))
                        {
                            if (classSymbol.Members.TryGetValue(symbolName, out var memberSymbol))
                            {
                                return memberSymbol;
                            }
                        }
                    }

                    // Fallback: 如果对象是变量但类型无法推断（如 "var"），
                    // 遍历所有类，查找包含该成员的类
                    if (objectSymbol.Kind == SymbolKind.Variable)
                    {
                        foreach (var kvp in document.SymbolTable)
                        {
                            if (kvp.Value.Kind == SymbolKind.Class)
                            {
                                if (kvp.Value.Members.TryGetValue(symbolName, out var memberSymbol))
                                {
                                    return memberSymbol;
                                }
                            }
                        }
                    }
                }
                
                // 也可能是静态方法调用 MathUtil.add
                if (objectToken.Type == LangTokenType.Identifier)
                {
                    var className = objectToken.Value;
                    if (document.SymbolTable.TryGetValue(className, out var classSymbol))
                    {
                        if (classSymbol.Members.TryGetValue(symbolName, out var memberSymbol))
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

        // 4. Fallback: 在所有类的成员中查找
        // 用于处理在类成员定义位置悬停的情况
        foreach (var kvp in document.SymbolTable)
        {
            if (kvp.Value.Kind == SymbolKind.Class)
            {
                if (kvp.Value.Members.TryGetValue(symbolName, out var memberSymbol))
                {
                    return memberSymbol;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 根据位置查找token
    /// </summary>
    private static LangToken? FindTokenAtPosition(List<LangToken> tokens, int line, int column, string? sourceCode = null)
    {
        // LSP的行列号从0开始
        var targetLine = line;
        var targetColumn = column;

        System.Diagnostics.Debug.WriteLine($"FindTokenAtPosition: LSP({line},{column})");

        // 如果有源代码，直接从源代码中查找
        if (sourceCode != null)
        {
            var lines = sourceCode.Split('\n');
            if (targetLine < 0 || targetLine >= lines.Length)
            {
                System.Diagnostics.Debug.WriteLine($"  -> Line out of range");
                return null;
            }

            var lineText = lines[targetLine];
            if (targetColumn < 0 || targetColumn >= lineText.Length)
            {
                System.Diagnostics.Debug.WriteLine($"  -> Column out of range");
                return null;
            }

            // 找到该行上的所有 tokens
            var lineTokens = tokens.Where(t => t.Line == targetLine + 1).ToList(); // token Line 是1-based

            // 在源代码中查找光标位置对应的 token
            foreach (var token in lineTokens)
            {
                // 在行文本中查找token的位置
                var tokenIndex = lineText.IndexOf(token.Value, StringComparison.Ordinal);
                while (tokenIndex >= 0)
                {
                    var tokenEnd = tokenIndex + token.Value.Length - 1;
                    if (targetColumn >= tokenIndex && targetColumn <= tokenEnd)
                    {
                        System.Diagnostics.Debug.WriteLine($"  -> Found: '{token.Value}' at column {tokenIndex}");
                        return token;
                    }

                    // 继续查找下一个出现的位置
                    tokenIndex = lineText.IndexOf(token.Value, tokenIndex + 1, StringComparison.Ordinal);
                }
            }

            System.Diagnostics.Debug.WriteLine($"  -> Not found in source");
            return null;
        }

        // 旧逻辑（使用 token.Column，已知有问题）
        var targetLineToken = line + 1;
        var targetColumnToken = column + 1;

        foreach (var token in tokens)
        {
            if (token.Line == targetLineToken)
            {
                var tokenEndColumn = token.Column + token.Value.Length - 1;

                System.Diagnostics.Debug.WriteLine($"  Checking token: '{token.Value}' at ({token.Line},{token.Column})-({token.Line},{tokenEndColumn})");

                if (targetColumnToken >= token.Column && targetColumnToken <= tokenEndColumn)
                {
                    System.Diagnostics.Debug.WriteLine($"  -> Found: '{token.Value}'");
                    return token;
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"  -> Not found");
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
                    EndColumn = token.Column + token.Value.Length - 2 // 修正结束列计算
                });
            }
        }

        return references;
    }
}
