using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LangParser;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 签名帮助处理器 - 提供函数参数提示
/// </summary>
public class SignatureHelpHandler(DocumentManager documentManager) : ISignatureHelpHandler
{
    public SignatureHelpRegistrationOptions GetRegistrationOptions(
        SignatureHelpCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new SignatureHelpRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            TriggerCharacters = new[] { "(", "," },
            RetriggerCharacters = new[] { "," }
        };
    }

    public Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);

        if (document?.Tokens == null || document.SymbolTable == null)
        {
            return Task.FromResult<SignatureHelp?>(null);
        }

        var line = request.Position.Line + 1; // LSP 从 0 开始
        var column = request.Position.Character + 1;

        // 查找当前光标所在的函数调用
        var functionCall = FindFunctionCall(document, line, column);

        if (functionCall == null)
        {
            return Task.FromResult<SignatureHelp?>(null);
        }

        // 查找函数符号
        if (!document.SymbolTable.TryGetValue(functionCall.FunctionName, out var functionSymbol))
        {
            // 检查内置函数
            var builtInSignature = GetBuiltInFunctionSignature(functionCall.FunctionName);
            if (builtInSignature != null)
            {
                return Task.FromResult<SignatureHelp?>(new SignatureHelp
                {
                    Signatures = new Container<SignatureInformation>(builtInSignature),
                    ActiveSignature = 0,
                    ActiveParameter = functionCall.CurrentParameterIndex
                });
            }

            return Task.FromResult<SignatureHelp?>(null);
        }

        // 构建函数签名
        var signature = BuildSignatureInformation(functionSymbol);

        var signatureHelp = new SignatureHelp
        {
            Signatures = new Container<SignatureInformation>(signature),
            ActiveSignature = 0,
            ActiveParameter = functionCall.CurrentParameterIndex
        };

        return Task.FromResult<SignatureHelp?>(signatureHelp);
    }

    /// <summary>
    /// 查找当前光标所在的函数调用
    /// </summary>
    private FunctionCallContext? FindFunctionCall(Models.DocumentParseResult document, int line, int column)
    {
        var tokens = document.Tokens!;
        int currentIndex = -1;

        // 找到光标位置的 token 索引
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Line == line && token.Column <= column)
            {
                currentIndex = i;
            }
            else if (token.Line > line)
            {
                break;
            }
        }

        if (currentIndex < 0)
        {
            return null;
        }

        // 向前查找最近的左括号和函数名
        int parenDepth = 0;
        string? functionName = null;
        int parameterIndex = 0;
        int commaCount = 0;

        for (int i = currentIndex; i >= 0; i--)
        {
            var token = tokens[i];

            if (token.Type == LangTokenType.RightParen)
            {
                parenDepth++;
            }
            else if (token.Type == LangTokenType.LeftParen)
            {
                if (parenDepth == 0)
                {
                    // 找到对应的左括号，查找函数名
                    if (i > 0 && tokens[i - 1].Type == LangTokenType.Identifier)
                    {
                        functionName = tokens[i - 1].Value;
                        parameterIndex = commaCount;
                        break;
                    }
                }
                else
                {
                    parenDepth--;
                }
            }
            else if (token.Type == LangTokenType.Comma && parenDepth == 0)
            {
                commaCount++;
            }
        }

        if (functionName == null)
        {
            return null;
        }

        return new FunctionCallContext
        {
            FunctionName = functionName,
            CurrentParameterIndex = parameterIndex
        };
    }

    /// <summary>
    /// 构建函数签名信息
    /// </summary>
    private SignatureInformation BuildSignatureInformation(Models.SymbolInfo functionSymbol)
    {
        var parameters = new List<ParameterInformation>();
        var signatureLabel = $"{functionSymbol.Name}(";

        // 提取参数信息（从成员中过滤参数）
        var paramSymbols = functionSymbol.Members.Values
            .Where(m => m.Kind == Models.SymbolKind.Parameter)
            .OrderBy(m => m.Location.Column)
            .ToList();

        for (int i = 0; i < paramSymbols.Count; i++)
        {
            var param = paramSymbols[i];
            var paramLabel = !string.IsNullOrEmpty(param.Type)
                ? $"{param.Name}:{param.Type}"
                : param.Name;

            if (i > 0)
            {
                signatureLabel += ", ";
            }

            signatureLabel += paramLabel;

            parameters.Add(new ParameterInformation
            {
                Label = paramLabel,
                Documentation = param.Documentation
            });
        }

        signatureLabel += ")";

        // 添加返回类型
        if (!string.IsNullOrEmpty(functionSymbol.Type))
        {
            signatureLabel += $" -> {functionSymbol.Type}";
        }

        return new SignatureInformation
        {
            Label = signatureLabel,
            Documentation = functionSymbol.Documentation,
            Parameters = new Container<ParameterInformation>(parameters)
        };
    }

    /// <summary>
    /// 获取内置函数的签名
    /// </summary>
    private SignatureInformation? GetBuiltInFunctionSignature(string functionName)
    {
        var builtInFunctions = new Dictionary<string, (string signature, string? doc, List<(string label, string? doc)> parameters)>
        {
            ["PrintLine"] = ("PrintLine(value)", "打印一行并换行", [("value", "要打印的值")]),
            ["Print"] = ("Print(value)", "打印不换行", [("value", "要打印的值")]),
            ["Input"] = ("Input(prompt:string) -> string", "读取用户输入", [("prompt:string", "提示信息")]),
            ["ReadLine"] = ("ReadLine() -> string", "读取一行输入", []),
            ["Sleep"] = ("Sleep(milliseconds:int)", "休眠指定毫秒数", [("milliseconds:int", "休眠时间（毫秒）")]),
            ["ToInt"] = ("ToInt(value) -> int", "转换为整数", [("value", "要转换的值")]),
            ["ToDouble"] = ("ToDouble(value) -> double", "转换为浮点数", [("value", "要转换的值")]),
            ["ToStr"] = ("ToStr(value) -> string", "转换为字符串", [("value", "要转换的值")]),
            ["ToBool"] = ("ToBool(value) -> bool", "转换为布尔值", [("value", "要转换的值")]),
            ["Len"] = ("Len(collection) -> int", "获取集合长度", [("collection", "集合对象")]),
            ["Type"] = ("Type(value) -> string", "获取值的类型", [("value", "要检查的值")]),
            ["Range"] = ("Range(start:int, end:int, step:int) -> range", "创建范围对象", [
                ("start:int", "起始值"),
                ("end:int", "结束值"),
                ("step:int", "步长")
            ])
        };

        if (builtInFunctions.TryGetValue(functionName, out var info))
        {
            var parameters = info.parameters.Select(p => new ParameterInformation
            {
                Label = p.label,
                Documentation = p.doc
            }).ToList();

            return new SignatureInformation
            {
                Label = info.signature,
                Documentation = info.doc,
                Parameters = new Container<ParameterInformation>(parameters)
            };
        }

        return null;
    }

    /// <summary>
    /// 函数调用上下文
    /// </summary>
    private class FunctionCallContext
    {
        public required string FunctionName { get; set; }
        public int CurrentParameterIndex { get; set; }
    }
}
