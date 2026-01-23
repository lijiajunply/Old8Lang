using Old8Lang.LangParser;
using SymbolInfo = Old8Lang.LanguageServer.Models.SymbolInfo;
using SymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 成员链分析器 - 负责解析和推断成员访问链的类型
/// 例如: obj.getB().getC().getValue()
/// </summary>
public class MemberChainAnalyzer(
    List<LangToken> tokens,
    Dictionary<string, SymbolInfo> symbolTable,
    Position position)
{
    /// <summary>
    /// 分析光标位置的成员链并返回最终类型
    /// </summary>
    public SymbolInfo? AnalyzeChain()
    {
        var line = position.Line + 1; // LSP 从 0 开始，token 从 1 开始
        var column = position.Character + 1;

        Console.WriteLine($"[MemberChainAnalyzer] Analyzing chain at Line={line}, Column={column}");

        // 1. 找到光标位置的点号
        var dotIndex = FindDotAtPosition(line, column);
        if (dotIndex == -1)
        {
            Console.WriteLine($"[MemberChainAnalyzer] No dot found at position");
            return null;
        }

        Console.WriteLine($"[MemberChainAnalyzer] Found dot at token index {dotIndex}");

        // 2. 向前回溯，找到完整的成员链
        var chainTokens = ExtractChainTokens(dotIndex);
        if (chainTokens.Count == 0)
        {
            Console.WriteLine($"[MemberChainAnalyzer] Failed to extract chain tokens");
            return null;
        }

        Console.WriteLine($"[MemberChainAnalyzer] Extracted {chainTokens.Count} chain tokens");
        foreach (var token in chainTokens)
        {
            Console.WriteLine($"[MemberChainAnalyzer]   Token: '{token.Value}' Type={token.Type}");
        }

        // 3. 逐步推断链中每个部分的类型
        var finalType = InferChainType(chainTokens);

        if (finalType != null)
        {
            Console.WriteLine($"[MemberChainAnalyzer] Final type inferred: {finalType.Name}");
        }
        else
        {
            Console.WriteLine($"[MemberChainAnalyzer] Failed to infer type");
        }

        return finalType;
    }

    /// <summary>
    /// 找到光标位置之前最近的点号 token 索引
    /// </summary>
    private int FindDotAtPosition(int line, int column)
    {
        Console.WriteLine($"[FindDotAtPosition] Searching for dot before Line={line}, Column={column}");
        Console.WriteLine($"[FindDotAtPosition] Total tokens: {tokens.Count}");

        // 打印光标附近的 tokens 以便调试
        Console.WriteLine($"[FindDotAtPosition] Tokens around cursor:");
        for (int j = Math.Max(0, tokens.Count - 30); j < tokens.Count; j++)
        {
            var t = tokens[j];
            Console.WriteLine($"[FindDotAtPosition]   [{j}] '{t.Value}' Type={t.Type} Line={t.Line} Column={t.Column}");
        }

        // 从后向前查找，找到光标位置之前或所在位置的最近一个点号
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            var token = tokens[i];

            // 如果 token 在光标位置之后，跳过
            // 注意：点号占一个字符，光标可能在点号后面（column = token.Column + 1）
            // 但是如果光标正好在点号位置，我们也应该考虑这个点号
            if (token.Line > line || (token.Line == line && token.Column > column))
            {
                Console.WriteLine($"[FindDotAtPosition] Skipping token at/after cursor: {token.Value} at Line={token.Line}, Column={token.Column}");
                continue;
            }

            // 如果找到点号
            if (token.Type == LangTokenType.Dot)
            {
                Console.WriteLine($"[FindDotAtPosition] Found dot at index {i}, Line={token.Line}, Column={token.Column}");
                return i;
            }
        }

        Console.WriteLine($"[FindDotAtPosition] No dot found");
        return -1;
    }

    /// <summary>
    /// 提取成员链的所有 token（从起点到光标处的点号）
    /// 例如: obj.getB().getC(). 会提取 [obj, ., getB, (, ), ., getC, (, ), .]
    /// 跳过测试标记（$和数字）
    /// </summary>
    private List<LangToken> ExtractChainTokens(int dotIndex)
    {
        var chainTokens = new List<LangToken>();
        var currentIndex = dotIndex;

        // 向前回溯找到链的起点
        while (currentIndex >= 0)
        {
            var token = tokens[currentIndex];

            // 跳过测试标记（$1, $2 等）
            if (token.Type == LangTokenType.Dollar ||
                (token.Type == LangTokenType.Number && currentIndex > 0 &&
                 tokens[currentIndex - 1].Type == LangTokenType.Dollar))
            {
                currentIndex--;
                continue;
            }

            // 如果遇到不属于链的 token，停止
            if (!IsChainToken(token.Type))
            {
                break;
            }

            chainTokens.Insert(0, token);
            currentIndex--;

            // 如果是标识符且前面不是点号，说明到达链的起点
            if (token.Type == LangTokenType.Identifier)
            {
                // 检查前面的 token（跳过可能的测试标记）
                int prevIndex = currentIndex;
                while (prevIndex >= 0)
                {
                    var prevToken = tokens[prevIndex];

                    // 跳过测试标记
                    if (prevToken.Type == LangTokenType.Dollar ||
                        (prevToken.Type == LangTokenType.Number && prevIndex > 0 &&
                         tokens[prevIndex - 1].Type == LangTokenType.Dollar))
                    {
                        prevIndex--;
                        continue;
                    }

                    // 如果前面不是点号，到达链的起点
                    if (prevToken.Type != LangTokenType.Dot)
                    {
                        return chainTokens;
                    }

                    // 前面是点号，继续向前
                    break;
                }

                // 如果没有更多 token 了，也返回
                if (prevIndex < 0)
                {
                    return chainTokens;
                }
            }
        }

        return chainTokens;
    }

    /// <summary>
    /// 判断 token 是否属于成员链
    /// </summary>
    private bool IsChainToken(LangTokenType type)
    {
        return type == LangTokenType.Identifier ||
               type == LangTokenType.Dot ||
               type == LangTokenType.LeftParen ||
               type == LangTokenType.RightParen ||
               type == LangTokenType.Comma ||
               type == LangTokenType.String ||
               type == LangTokenType.Number ||
               type == LangTokenType.True ||
               type == LangTokenType.False;
    }

    /// <summary>
    /// 推断成员链的类型
    /// 例如: obj.getB().getC()
    /// - obj 是 A 类型
    /// - getB() 返回 B 类型
    /// - getC() 返回 C 类型
    /// </summary>
    private SymbolInfo? InferChainType(List<LangToken> chainTokens)
    {
        if (chainTokens.Count == 0)
        {
            return null;
        }

        // 1. 第一个 token 必须是标识符（变量名或类名）
        var firstToken = chainTokens[0];
        if (firstToken.Type != LangTokenType.Identifier)
        {
            Console.WriteLine($"[InferChainType] First token is not identifier: {firstToken.Type}");
            return null;
        }

        var objectName = firstToken.Value;
        if (!symbolTable.TryGetValue(objectName, out var currentSymbol))
        {
            Console.WriteLine($"[InferChainType] Symbol '{objectName}' not found in symbol table");
            return null;
        }

        Console.WriteLine($"[InferChainType] Starting with symbol '{objectName}' (Kind={currentSymbol.Kind}, Type={currentSymbol.Type})");

        // 2. 获取当前对象的类型
        var currentType = GetSymbolType(currentSymbol);
        if (currentType == null)
        {
            Console.WriteLine($"[InferChainType] Failed to get type for '{objectName}'");
            return null;
        }

        Console.WriteLine($"[InferChainType] Initial type: {currentType.Name}");

        // 3. 遍历链中的每个成员访问
        int i = 1; // 跳过第一个标识符
        while (i < chainTokens.Count)
        {
            var token = chainTokens[i];

            // 跳过点号
            if (token.Type == LangTokenType.Dot)
            {
                i++;
                continue;
            }

            // 处理成员访问
            if (token.Type == LangTokenType.Identifier)
            {
                var memberName = token.Value;
                Console.WriteLine($"[InferChainType] Accessing member '{memberName}' on type '{currentType.Name}'");

                // 查找成员
                if (!currentType.Members.TryGetValue(memberName, out var member))
                {
                    Console.WriteLine($"[InferChainType] Member '{memberName}' not found in type '{currentType.Name}'");
                    return null;
                }

                Console.WriteLine($"[InferChainType] Found member '{memberName}' (Kind={member.Kind}, Type={member.Type})");

                // 检查下一个 token 是否是括号（方法调用）
                bool isMethodCall = i + 1 < chainTokens.Count &&
                                   chainTokens[i + 1].Type == LangTokenType.LeftParen;

                if (isMethodCall)
                {
                    // 方法调用：跳过参数列表
                    i = SkipMethodArguments(chainTokens, i + 1);

                    // 获取方法的返回类型（从签名中提取）
                    var returnType = ExtractReturnType(member.Type);
                    if (string.IsNullOrEmpty(returnType) || returnType == "void")
                    {
                        Console.WriteLine($"[InferChainType] Method '{memberName}' returns void or has no return type");
                        return null;
                    }

                    // 查找返回类型的符号
                    if (!symbolTable.TryGetValue(returnType, out currentType))
                    {
                        Console.WriteLine($"[InferChainType] Return type '{returnType}' not found in symbol table");
                        return null;
                    }

                    Console.WriteLine($"[InferChainType] Method '{memberName}' returns type '{currentType.Name}'");
                }
                else
                {
                    // 属性访问（也可能从签名中提取类型）
                    var propertyType = ExtractReturnType(member.Type);
                    if (string.IsNullOrEmpty(propertyType))
                    {
                        Console.WriteLine($"[InferChainType] Property '{memberName}' has no type");
                        return null;
                    }

                    // 查找属性类型的符号
                    if (!symbolTable.TryGetValue(propertyType, out currentType))
                    {
                        Console.WriteLine($"[InferChainType] Property type '{propertyType}' not found in symbol table");
                        return null;
                    }

                    Console.WriteLine($"[InferChainType] Property '{memberName}' has type '{currentType.Name}'");
                }

                i++;
            }
            else
            {
                Console.WriteLine($"[InferChainType] Unexpected token type: {token.Type}");
                i++;
            }
        }

        return currentType;
    }

    /// <summary>
    /// 跳过方法调用的参数列表
    /// </summary>
    private int SkipMethodArguments(List<LangToken> tokens, int startIndex)
    {
        if (startIndex >= tokens.Count || tokens[startIndex].Type != LangTokenType.LeftParen)
        {
            return startIndex;
        }

        int depth = 1;
        int i = startIndex + 1;

        while (i < tokens.Count && depth > 0)
        {
            if (tokens[i].Type == LangTokenType.LeftParen)
            {
                depth++;
            }
            else if (tokens[i].Type == LangTokenType.RightParen)
            {
                depth--;
            }
            i++;
        }

        return i;
    }

    /// <summary>
    /// 获取符号的类型信息
    /// </summary>
    private SymbolInfo? GetSymbolType(SymbolInfo symbol)
    {
        if (symbol.Kind == SymbolKind.Class)
        {
            // 符号本身就是类
            return symbol;
        }
        else if (symbol.Kind == SymbolKind.Variable && !string.IsNullOrEmpty(symbol.Type))
        {
            // 变量：查找其类型
            if (symbolTable.TryGetValue(symbol.Type, out var typeSymbol) &&
                typeSymbol.Kind == SymbolKind.Class)
            {
                return typeSymbol;
            }
        }

        return null;
    }

    /// <summary>
    /// 从方法签名中提取返回类型
    /// 例如：
    /// - "func getB() -> B" => "B"
    /// - "func getValue() -> int" => "int"
    /// - "B" => "B"
    /// </summary>
    private string? ExtractReturnType(string? typeOrSignature)
    {
        if (string.IsNullOrEmpty(typeOrSignature))
        {
            return null;
        }

        // 如果包含 "->", 这是一个方法签名
        var arrowIndex = typeOrSignature.IndexOf("->");
        if (arrowIndex != -1)
        {
            // 提取箭头后的返回类型
            var returnType = typeOrSignature.Substring(arrowIndex + 2).Trim();
            Console.WriteLine($"[ExtractReturnType] Extracted '{returnType}' from '{typeOrSignature}'");
            return returnType;
        }

        // 否则直接返回类型字符串
        return typeOrSignature;
    }
}
