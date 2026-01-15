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
        // 检查取消令牌
        cancellationToken.ThrowIfCancellationRequested();

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

        // 添加符号补全（使用作用域感知）
        if (document?.SymbolTable != null && document.Ast != null)
        {
            // 使用作用域分析器获取当前位置可见的符号
            var scopeAnalyzer = new ScopeAnalyzer(document.Ast, request.Position, document.SymbolTable, uri);
            var visibleSymbols = scopeAnalyzer.GetVisibleSymbols();

            Console.WriteLine($"[CompletionHandler] Visible symbols count: {visibleSymbols.Count}");

            // 将符号转换为补全项
            var symbolCompletions = ConvertSymbolsToCompletionItems(visibleSymbols);
            completionItems.AddRange(symbolCompletions);
        }
        else
        {
            Console.WriteLine($"[CompletionHandler] Skipping symbol completions (document, SymbolTable, or Ast is null)");
        }

        // 智能排序：设置排序优先级
        ApplySmartSorting(completionItems);

        return Task.FromResult(new CompletionList(completionItems, isIncomplete: false));
    }

    /// <summary>
    /// 获取成员补全（obj. 后面的补全）
    /// 支持成员链：obj.getB().getC().
    /// </summary>
    private static List<CompletionItem>? GetMemberCompletions(
        Models.DocumentParseResult document,
        Position position)
    {
        var tokens = document.Tokens!;
        var symbolTable = document.SymbolTable!;

        Console.WriteLine($"[GetMemberCompletions] Analyzing member chain at position Line={position.Line}, Column={position.Character}");

        // 使用成员链分析器来推断类型
        var analyzer = new MemberChainAnalyzer(tokens, symbolTable, position);
        var classSymbol = analyzer.AnalyzeChain();

        if (classSymbol == null)
        {
            Console.WriteLine($"[GetMemberCompletions] Failed to analyze member chain");
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
        var completions = new List<CompletionItem>();

        // 1. 添加全局函数注册表中的函数
        GlobalFunctionInitializer.EnsureInitialized();
        var globalFunctions = GlobalFunctionRegistry.Instance.GetAllFunctionNames();

        completions.AddRange(globalFunctions.Select(name => new CompletionItem
        {
            Label = name,
            Kind = CompletionItemKind.Function,
            Detail = "全局函数",
            Documentation = $"Old8Lang 内置全局函数：{name}",
            InsertText = $"{name}($0)",
            InsertTextFormat = InsertTextFormat.Snippet
        }));

        // 2. 添加原生库函数（Old8LangLib）
        var nativeFunctions = NativeLibraryRegistry.GetAllFunctionNames();

        completions.AddRange(nativeFunctions.Select(name => new CompletionItem
        {
            Label = name,
            Kind = CompletionItemKind.Function,
            Detail = "原生库函数",
            Documentation = $"Old8LangLib 原生函数：{name}",
            InsertText = $"{name}($0)",
            InsertTextFormat = InsertTextFormat.Snippet
        }));

        return completions;
    }

    private static IEnumerable<CompletionItem> GetKeywordCompletions()
    {
        // 从 KeywordType 枚举动态获取所有关键字
        var keywords = Enum.GetNames<LangParser.KeywordType>()
            .Select(name => name.ToLower())
            .ToList();

        // 添加类型关键字（不在 KeywordType 中但是语言的关键字）
        var typeKeywords = new[] { "int", "double", "string", "bool", "char", "void", "var", "any" };
        keywords.AddRange(typeKeywords);

        return keywords.Distinct().Select(keyword => new CompletionItem
        {
            Label = keyword,
            Kind = CompletionItemKind.Keyword,
            Detail = "Old8Lang 关键字",
            InsertText = keyword
        });
    }

    /// <summary>
    /// 将符号列表转换为补全项（替换原来的 GetSymbolCompletions）
    /// </summary>
    private static IEnumerable<CompletionItem> ConvertSymbolsToCompletionItems(List<Models.SymbolInfo> symbols)
    {
        return symbols.Select(symbol =>
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
                // 其他符号（变量、常量、参数等）：普通文本
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
                Label = "interface",
                Kind = CompletionItemKind.Snippet,
                Detail = "接口定义",
                Documentation = "创建一个新接口",
                InsertText = "interface ${1:InterfaceName} {\n\tfunc ${2:methodName}(${3:params}) -> ${4:void}\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "enum",
                Kind = CompletionItemKind.Snippet,
                Detail = "枚举定义",
                Documentation = "创建一个新枚举",
                InsertText = "enum ${1:EnumName} {\n\t${2:Value1},\n\t${3:Value2}\n}",
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
            },
            new CompletionItem
            {
                Label = "match",
                Kind = CompletionItemKind.Snippet,
                Detail = "match 表达式",
                Documentation = "创建 match 模式匹配表达式",
                InsertText = "${1:result} <- match ${2:value} {\n\tcase ${3:pattern1} -> ${4:expression1}\n\tcase _ -> ${5:defaultExpression}\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "defer",
                Kind = CompletionItemKind.Snippet,
                Detail = "defer 语句",
                Documentation = "创建延迟执行语句",
                InsertText = "defer ${1:statement}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "using",
                Kind = CompletionItemKind.Snippet,
                Detail = "using 语句",
                Documentation = "创建资源管理语句",
                InsertText = "using ${1:resource} <- ${2:CreateResource()} {\n\t$0\n}",
                InsertTextFormat = InsertTextFormat.Snippet
            },
            new CompletionItem
            {
                Label = "select",
                Kind = CompletionItemKind.Snippet,
                Detail = "select 语句",
                Documentation = "创建 channel 多路选择语句",
                InsertText = "select {\n\tcase ${1:ch1} <- ${2:value} -> {\n\t\t$0\n\t}\n\tcase ${3:val} from ${4:ch2} -> {\n\t\t\n\t}\n\tdefault -> {\n\t\t\n\t}\n}",
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
            Models.SymbolKind.Field => CompletionItemKind.Field,
            Models.SymbolKind.Keyword => CompletionItemKind.Keyword,
            _ => CompletionItemKind.Text
        };
    }
}