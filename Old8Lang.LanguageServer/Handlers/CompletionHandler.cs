using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LangParser;
using Old8Lang.GlobalFunctions.Core;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 自动补全处理器
/// </summary>
public class CompletionHandler(DocumentManager documentManager) : ICompletionHandler
{
    public CompletionRegistrationOptions GetRegistrationOptions(
        CompletionCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            TriggerCharacters = new[] { ".", "<" },
            ResolveProvider = false
        };
    }

    public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);

        Console.WriteLine($"[CompletionHandler] Requested URI: {uri}");
        Console.WriteLine($"[CompletionHandler] Document is null: {document == null}");
        if (document != null)
        {
            Console.WriteLine($"[CompletionHandler] SymbolTable is null: {document.SymbolTable == null}");
            if (document.SymbolTable != null)
            {
                Console.WriteLine($"[CompletionHandler] SymbolTable count: {document.SymbolTable.Count}");
            }
        }

        var completionItems = new List<CompletionItem>();

        // 检查是否是成员访问补全（obj.）
        Console.WriteLine($"[CompletionHandler] Checking member access completion...");
        if (document?.Tokens != null && document.SymbolTable != null)
        {
            Console.WriteLine($"[CompletionHandler] Calling GetMemberCompletions...");
            var memberCompletions = GetMemberCompletions(document, request.Position);
            Console.WriteLine($"[CompletionHandler] GetMemberCompletions returned: {memberCompletions?.Count ?? 0} items");
            if (memberCompletions != null && memberCompletions.Any())
            {
                Console.WriteLine($"[CompletionHandler] Returning member completions only");
                // 如果是成员访问，只返回成员补全
                return Task.FromResult(new CompletionList(memberCompletions, isIncomplete: false));
            }
        }
        else
        {
            Console.WriteLine($"[CompletionHandler] Skipping member completion (Tokens={document?.Tokens != null}, SymbolTable={document?.SymbolTable != null})");
        }

        // 添加关键字补全
        completionItems.AddRange(GetKeywordCompletions());

        // 添加代码片段补全
        completionItems.AddRange(GetSnippetCompletions());

        // 添加内置函数补全（所有70+个全局函数）
        completionItems.AddRange(GetBuiltInFunctionCompletions());

        // 添加符号补全
        if (document?.SymbolTable != null)
        {
            var symbols = GetSymbolCompletions(document.SymbolTable).ToList();
            Console.WriteLine($"[CompletionHandler] Symbol completions count: {symbols.Count}");
            completionItems.AddRange(symbols);
        }
        else
        {
            Console.WriteLine($"[CompletionHandler] Skipping symbol completions (document or SymbolTable is null)");
        }

        // 智能排序：设置排序优先级
        ApplySmartSorting(completionItems);

        return Task.FromResult(new CompletionList(completionItems, isIncomplete: false));
    }

    /// <summary>
    /// 获取成员补全（obj. 后面的补全）
    /// </summary>
    private static List<CompletionItem>? GetMemberCompletions(
        Models.DocumentParseResult document,
        Position position)
    {
        var tokens = document.Tokens!;
        var line = position.Line + 1; // LSP 从 0 开始，token 从 1 开始
        var column = position.Character + 1;

        Console.WriteLine($"[GetMemberCompletions] Looking for dot at Line={line}, Column={column}");
        Console.WriteLine($"[GetMemberCompletions] Total tokens: {tokens.Count}");

        // 查找光标前的 token
        LangToken? dotToken = null;
        LangToken? objectToken = null;

        // 输出最后10个 tokens 用于调试
        Console.WriteLine($"[GetMemberCompletions] Last 10 tokens:");
        foreach (var token in tokens.Skip(Math.Max(0, tokens.Count - 10)))
        {
            Console.WriteLine($"[GetMemberCompletions]   Token: '{token.Value}' Type={token.Type} Line={token.Line} Column={token.Column}");
        }

        for (int i = tokens.Count - 1; i >= 1; i--)
        {
            var token = tokens[i];
            var prevToken = tokens[i - 1];

            // 查找光标位置附近的 dot token
            if (token.Line == line && token.Type == LangTokenType.Dot)
            {
                Console.WriteLine($"[GetMemberCompletions] Found dot at Line={token.Line}, Column={token.Column}");
                if (column >= token.Column)
                {
                    dotToken = token;
                    objectToken = prevToken;
                    Console.WriteLine($"[GetMemberCompletions] Object token: {prevToken.Value} (Type={prevToken.Type})");
                    break;
                }
            }
        }

        // 如果没找到点号，说明不是成员访问
        if (dotToken == null || objectToken == null ||
            objectToken.Value.Type != LangTokenType.Identifier)
        {
            Console.WriteLine($"[GetMemberCompletions] Not a member access (dotToken={dotToken != null}, objectToken={objectToken != null})");
            return null;
        }

        var objectName = objectToken.Value.Value;
        if (objectName == null || document.SymbolTable == null)
        {
            Console.WriteLine($"[GetMemberCompletions] objectName or SymbolTable is null");
            return null;
        }

        Console.WriteLine($"[GetMemberCompletions] Looking for object: {objectName}");

        // 查找对象的符号
        if (!document.SymbolTable.TryGetValue(objectName, out var objectSymbol))
        {
            Console.WriteLine($"[GetMemberCompletions] Object '{objectName}' not found in symbol table");
            return null;
        }

        Console.WriteLine($"[GetMemberCompletions] Object symbol found: Kind={objectSymbol.Kind}, Type={objectSymbol.Type}");

        // 获取类符号
        Models.SymbolInfo? classSymbol = null;

        if (objectSymbol.Kind == Models.SymbolKind.Class)
        {
            // 如果对象本身就是类（例如静态成员访问 User.create()）
            classSymbol = objectSymbol;
        }
        else if (objectSymbol.Kind == Models.SymbolKind.Variable && !string.IsNullOrEmpty(objectSymbol.Type))
        {
            // 如果对象是变量，尝试根据类型查找类符号
            if (document.SymbolTable.TryGetValue(objectSymbol.Type, out var typeSymbol) &&
                typeSymbol.Kind == Models.SymbolKind.Class)
            {
                classSymbol = typeSymbol;
                Console.WriteLine($"[GetMemberCompletions] Found class '{objectSymbol.Type}' for variable '{objectName}'");
            }
            else
            {
                Console.WriteLine($"[GetMemberCompletions] Type '{objectSymbol.Type}' not found or not a class");
                return null;
            }
        }
        else
        {
            Console.WriteLine($"[GetMemberCompletions] Object '{objectName}' is not a class or typed variable (Kind={objectSymbol.Kind})");
            return null;
        }

        Console.WriteLine($"[GetMemberCompletions] Found class '{classSymbol.Name}' with {classSymbol.Members.Count} members");

        // 返回类的所有成员
        return classSymbol.Members.Values.Select(member =>
        {
            // 构建访问修饰符信息
            var modifiers = new List<string>();
            if (member.AccessModifier != Models.AccessModifier.Public)
            {
                modifiers.Add(member.AccessModifier.ToString().ToLower());
            }
            if (member.IsStatic)
            {
                modifiers.Add("static");
            }

            var labelDetails = modifiers.Count > 0
                ? new CompletionItemLabelDetails { Description = string.Join(" ", modifiers) }
                : null;

            // 根据成员类型构建不同的 CompletionItem
            if (member.Kind == Models.SymbolKind.Method)
            {
                return new CompletionItem
                {
                    Label = member.Name,
                    Kind = ConvertSymbolKind(member.Kind),
                    Detail = BuildSimpleFunctionSignature(member),
                    Documentation = member.Documentation,
                    InsertText = $"{member.Name}($0)",
                    InsertTextFormat = InsertTextFormat.Snippet,
                    LabelDetails = labelDetails
                };
            }
            else
            {
                return new CompletionItem
                {
                    Label = member.Name,
                    Kind = ConvertSymbolKind(member.Kind),
                    Detail = member.Type ?? member.Kind.ToString(),
                    Documentation = member.Documentation,
                    InsertText = member.Name,
                    LabelDetails = labelDetails
                };
            }
        }).ToList();
    }

    /// <summary>
    /// 获取内置函数补全（从 GlobalFunctionRegistry 动态获取）
    /// </summary>
    private static IEnumerable<CompletionItem> GetBuiltInFunctionCompletions()
    {
        // 全局函数已在 Program.cs 启动时初始化，这里直接获取即可
        var functionNames = GlobalFunctionRegistry.Instance.GetAllFunctionNames();

        return functionNames.Select(name => new CompletionItem
        {
            Label = name,
            Kind = CompletionItemKind.Function,
            Detail = "内置函数",
            Documentation = $"Old8Lang 内置全局函数：{name}",
            InsertText = $"{name}($0)",
            InsertTextFormat = InsertTextFormat.Snippet
        });
    }

    private static IEnumerable<CompletionItem> GetKeywordCompletions()
    {
        // 从 KeywordType 枚举动态获取所有关键字
        var keywords = Enum.GetNames<LangParser.KeywordType>()
            .Select(name => name.ToLower())
            .ToList();

        // 添加类型关键字（不在 KeywordType 中但是语言的关键字）
        var typeKeywords = new[] { "int", "double", "string", "bool", "char", "void", "var" };
        keywords.AddRange(typeKeywords);

        return keywords.Distinct().Select(keyword => new CompletionItem
        {
            Label = keyword,
            Kind = CompletionItemKind.Keyword,
            Detail = "Old8Lang 关键字",
            InsertText = keyword
        });
    }

    private static IEnumerable<CompletionItem> GetSymbolCompletions(
        Dictionary<string, Models.SymbolInfo> symbolTable)
    {
        return symbolTable.Values.Select(symbol =>
        {
            // 根据符号类型构建不同的 CompletionItem
            if (symbol.Kind == Models.SymbolKind.Function || symbol.Kind == Models.SymbolKind.Method)
            {
                // 函数/方法：使用 Snippet 格式
                return new CompletionItem
                {
                    Label = symbol.Name,
                    Kind = ConvertSymbolKind(symbol.Kind),
                    Detail = BuildSimpleFunctionSignature(symbol),
                    Documentation = symbol.Documentation,
                    InsertText = $"{symbol.Name}($0)",
                    InsertTextFormat = InsertTextFormat.Snippet
                };
            }
            else if (symbol.Kind == Models.SymbolKind.Class)
            {
                // 类：普通文本
                return new CompletionItem
                {
                    Label = symbol.Name,
                    Kind = ConvertSymbolKind(symbol.Kind),
                    Detail = $"class {symbol.Name}",
                    Documentation = symbol.Documentation,
                    InsertText = symbol.Name
                };
            }
            else
            {
                // 其他符号（变量、常量等）：普通文本
                return new CompletionItem
                {
                    Label = symbol.Name,
                    Kind = ConvertSymbolKind(symbol.Kind),
                    Detail = symbol.Type ?? symbol.Kind.ToString(),
                    Documentation = symbol.Documentation,
                    InsertText = symbol.Name
                };
            }
        });
    }

    /// <summary>
    /// 构建简单的函数签名字符串（不包含参数详情）
    /// </summary>
    private static string BuildSimpleFunctionSignature(Models.SymbolInfo symbol)
    {
        var returnType = !string.IsNullOrEmpty(symbol.Type) ? symbol.Type : "void";
        return $"{symbol.Name}(...) -> {returnType}";
    }

    /// <summary>
    /// 应用智能排序
    /// </summary>
    private static void ApplySmartSorting(List<CompletionItem> items)
    {
        // 为每个补全项创建新的对象，设置排序文本
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var priority = GetCompletionPriority(item.Kind);

            // 创建新的 CompletionItem，保留原有属性并添加 SortText
            items[i] = new CompletionItem
            {
                Label = item.Label,
                Kind = item.Kind,
                Detail = item.Detail,
                Documentation = item.Documentation,
                InsertText = item.InsertText,
                InsertTextFormat = item.InsertTextFormat,
                LabelDetails = item.LabelDetails,
                // 使用两位数字前缀确保正确排序
                SortText = $"{priority:D2}_{item.Label}"
            };
        }
    }

    /// <summary>
    /// 获取补全项的优先级（数字越小优先级越高）
    /// </summary>
    private static int GetCompletionPriority(CompletionItemKind kind)
    {
        return kind switch
        {
            // 最高优先级：变量和参数（最常用）
            CompletionItemKind.Variable => 10,
            CompletionItemKind.Property => 11,

            // 高优先级：函数和方法
            CompletionItemKind.Function => 20,
            CompletionItemKind.Method => 21,

            // 中优先级：类和常量
            CompletionItemKind.Class => 30,
            CompletionItemKind.Constant => 31,

            // 低优先级：代码片段
            CompletionItemKind.Snippet => 40,

            // 最低优先级：关键字
            CompletionItemKind.Keyword => 50,

            // 默认
            _ => 99
        };
    }

    /// <summary>
    /// 获取代码片段补全
    /// </summary>
    private static IEnumerable<CompletionItem> GetSnippetCompletions()
    {
        return new[]
        {
            new CompletionItem
            {
                Label = "func",
                Kind = CompletionItemKind.Snippet,
                Detail = "函数定义",
                Documentation = "创建一个新函数",
                InsertText = "func ${1:functionName}(${2:params}) -> ${3:void} {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "asyncfunc",
                Kind = CompletionItemKind.Snippet,
                Detail = "异步函数定义",
                Documentation = "创建一个新的异步函数",
                InsertText = "async func ${1:functionName}(${2:params}) -> ${3:void} {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "class",
                Kind = CompletionItemKind.Snippet,
                Detail = "类定义",
                Documentation = "创建一个新类",
                InsertText = "class ${1:ClassName} {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "if",
                Kind = CompletionItemKind.Snippet,
                Detail = "if 语句",
                Documentation = "创建 if 条件语句",
                InsertText = "if ${1:condition} {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "ifelse",
                Kind = CompletionItemKind.Snippet,
                Detail = "if-else 语句",
                Documentation = "创建 if-else 条件语句",
                InsertText = "if ${1:condition} {\n\t$0\n} else {\n\t\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "for",
                Kind = CompletionItemKind.Snippet,
                Detail = "for 循环",
                Documentation = "创建 for 循环",
                InsertText = "for ${1:i} <- ${2:0}, ${1:i} < ${3:10}, ${1:i} <- ${1:i} + 1 {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "forin",
                Kind = CompletionItemKind.Snippet,
                Detail = "for-in 循环",
                Documentation = "创建 for-in 循环",
                InsertText = "for ${1:item} in ${2:collection} {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "while",
                Kind = CompletionItemKind.Snippet,
                Detail = "while 循环",
                Documentation = "创建 while 循环",
                InsertText = "while ${1:condition} {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "try",
                Kind = CompletionItemKind.Snippet,
                Detail = "try-catch 语句",
                Documentation = "创建异常处理",
                InsertText = "try {\n\t$0\n} catch ${1:e} {\n\t\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "switch",
                Kind = CompletionItemKind.Snippet,
                Detail = "switch 语句",
                Documentation = "创建 switch 分支",
                InsertText = "switch ${1:value} {\n\tcase ${2:value1}:\n\t\t$0\n\t\tbreak\n\tdefault:\n\t\tbreak\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            }
        };
    }

    private static CompletionItemKind ConvertSymbolKind(Models.SymbolKind kind)
    {
        return kind switch
        {
            Models.SymbolKind.Variable => CompletionItemKind.Variable,
            Models.SymbolKind.Function => CompletionItemKind.Function,
            Models.SymbolKind.Class => CompletionItemKind.Class,
            Models.SymbolKind.Method => CompletionItemKind.Method,
            Models.SymbolKind.Property => CompletionItemKind.Property,
            Models.SymbolKind.Parameter => CompletionItemKind.Variable,
            Models.SymbolKind.Constant => CompletionItemKind.Constant,
            _ => CompletionItemKind.Text
        };
    }
}