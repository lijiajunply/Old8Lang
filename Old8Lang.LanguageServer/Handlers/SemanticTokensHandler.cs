using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 语义标记处理器 - 提供基于语义的精确语法高亮
/// </summary>
public class SemanticTokensHandler(DocumentManager documentManager) : SemanticTokensHandlerBase
{
    // 语义标记类型（按LSP协议标准定义）
    private static readonly string[] TokenTypes =
    [
        SemanticTokenType.Namespace,   // 0
        SemanticTokenType.Class,       // 1
        SemanticTokenType.Enum,        // 2
        SemanticTokenType.Interface,   // 3
        SemanticTokenType.Struct,      // 4
        SemanticTokenType.TypeParameter, // 5
        SemanticTokenType.Type,        // 6
        SemanticTokenType.Parameter,   // 7
        SemanticTokenType.Variable,    // 8
        SemanticTokenType.Property,    // 9
        SemanticTokenType.EnumMember,  // 10
        SemanticTokenType.Decorator,   // 11
        SemanticTokenType.Event,       // 12
        SemanticTokenType.Function,    // 13
        SemanticTokenType.Method,      // 14
        SemanticTokenType.Macro,       // 15
        SemanticTokenType.Keyword,     // 16
        SemanticTokenType.Modifier,    // 17
        SemanticTokenType.Comment,     // 18
        SemanticTokenType.String,      // 19
        SemanticTokenType.Number,      // 20
        SemanticTokenType.Regexp,      // 21
        SemanticTokenType.Operator     // 22
    ];

    // 语义标记修饰符
    private static readonly string[] TokenModifiers =
    [
        SemanticTokenModifier.Declaration,    // 0
        SemanticTokenModifier.Definition,     // 1
        SemanticTokenModifier.Readonly,       // 2
        SemanticTokenModifier.Static,         // 3
        SemanticTokenModifier.Deprecated,     // 4
        SemanticTokenModifier.Abstract,       // 5
        SemanticTokenModifier.Async,          // 6
        SemanticTokenModifier.Modification,   // 7
        SemanticTokenModifier.Documentation,  // 8
        SemanticTokenModifier.DefaultLibrary  // 9
    ];

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
    {
        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            Legend = new SemanticTokensLegend
            {
                TokenTypes = new Container<SemanticTokenType>(TokenTypes.Select(t => new SemanticTokenType(t))),
                TokenModifiers = new Container<SemanticTokenModifier>(TokenModifiers.Select(m => new SemanticTokenModifier(m)))
            },
            Full = new SemanticTokensCapabilityRequestFull
            {
                Delta = false
            },
            Range = true
        };
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
    }

    protected override async Task Tokenize(
        SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
    {
        var document = documentManager.GetDocument(identifier.TextDocument.Uri.ToString());
        if (document == null || document.Tokens == null)
        {
            return;
        }

        // 遍历所有token，生成语义标记
        foreach (var token in document.Tokens)
        {
            var (tokenTypeIndex, modifiers) = ClassifyToken(token, document);

            if (tokenTypeIndex.HasValue && tokenTypeIndex.Value >= 0 && tokenTypeIndex.Value < TokenTypes.Length)
            {
                var line = token.Line - 1; // 转换为 0-based
                var column = token.Column - 1; // 转换为 0-based
                var length = token.Value.Length;

                // Push 方法的签名是: Push(line, column, length, tokenTypeIndex, modifiersBitfield)
                // modifiersBitfield 是一个 int，表示修饰符的位字段
                var modifiersBitfield = 0;
                for (int i = 0; i < modifiers.Length; i++)
                {
                    // 查找修饰符在数组中的索引
                    for (int j = 0; j < TokenModifiers.Length; j++)
                    {
                        if (TokenModifiers[j] == modifiers[i].ToString())
                        {
                            modifiersBitfield |= (1 << j);
                            break;
                        }
                    }
                }

                builder.Push(line, column, length, tokenTypeIndex.Value, modifiersBitfield);
            }
        }
    }

    /// <summary>
    /// 对token进行分类，返回语义类型和修饰符
    /// </summary>
    private (int? tokenType, SemanticTokenModifier[] modifiers) ClassifyToken(LangToken token, DocumentParseResult document)
    {
        var modifiers = new List<SemanticTokenModifier>();

        switch (token.Type)
        {
            // 关键字
            case LangTokenType.Func:
            case LangTokenType.Async:
            case LangTokenType.Class:
            case LangTokenType.If:
            case LangTokenType.Elif:
            case LangTokenType.Else:
            case LangTokenType.For:
            case LangTokenType.While:
            case LangTokenType.Return:
            case LangTokenType.Break:
            case LangTokenType.Continue:
            case LangTokenType.Switch:
            case LangTokenType.Case:
            case LangTokenType.Default:
            case LangTokenType.Try:
            case LangTokenType.Catch:
            case LangTokenType.Finally:
            case LangTokenType.Throw:
            case LangTokenType.Import:
            case LangTokenType.Native:
            case LangTokenType.Yield:
            case LangTokenType.Using:
            case LangTokenType.Select:
            case LangTokenType.From:
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Keyword), modifiers.ToArray());

            // 修饰符
            case LangTokenType.Public:
            case LangTokenType.Private:
            case LangTokenType.Protected:
            case LangTokenType.Static:
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Modifier), modifiers.ToArray());

            // 文档注释
            case LangTokenType.DocComment:
                modifiers.Add(new SemanticTokenModifier(SemanticTokenModifier.Documentation));
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Comment), modifiers.ToArray());

            // 字符串
            case LangTokenType.String:
                return (Array.IndexOf(TokenTypes, SemanticTokenType.String), modifiers.ToArray());

            // 数字
            case LangTokenType.Number:
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Number), modifiers.ToArray());

            // 运算符
            case LangTokenType.Plus:
            case LangTokenType.Minus:
            case LangTokenType.Star:        // *
            case LangTokenType.Slash:       // /
            case LangTokenType.Percent:     // %
            case LangTokenType.Caret:       // ^
            case LangTokenType.Assignment:
            case LangTokenType.Equals:      // ==
            case LangTokenType.NotEquals:   // !=
            case LangTokenType.GreaterThan: // >
            case LangTokenType.GreaterThanEquals: // >=
            case LangTokenType.LessThan:    // <
            case LangTokenType.LessThanEquals:    // <=
            case LangTokenType.And:
            case LangTokenType.Or:
            case LangTokenType.Not:
            case LangTokenType.Ampersand:   // &
            case LangTokenType.Pipe:        // |
            case LangTokenType.Xor:
            case LangTokenType.Wavy:        // ~
            case LangTokenType.Dot:
            case LangTokenType.Arrow:
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Operator), modifiers.ToArray());

            // 标识符 - 需要根据符号表进一步分类
            case LangTokenType.Identifier:
                return ClassifyIdentifier(token, document, modifiers);

            default:
                return (null, modifiers.ToArray());
        }
    }

    /// <summary>
    /// 对标识符进行语义分类
    /// </summary>
    private (int? tokenType, SemanticTokenModifier[] modifiers) ClassifyIdentifier(
        LangToken token, DocumentParseResult document, List<SemanticTokenModifier> modifiers)
    {
        if (document.SymbolTable == null)
        {
            return (Array.IndexOf(TokenTypes, SemanticTokenType.Variable), modifiers.ToArray());
        }

        var symbolName = token.Value;

        // 先在符号表中查找
        if (document.SymbolTable.TryGetValue(symbolName, out var symbol))
        {
            return ClassifySymbol(symbol, modifiers);
        }

        // 如果没找到，可能是成员访问，查找所有类的成员
        foreach (var kvp in document.SymbolTable)
        {
            if (kvp.Value.Kind == Old8Lang.LanguageServer.Models.SymbolKind.Class)
            {
                if (kvp.Value.Members.TryGetValue(symbolName, out var memberSymbol))
                {
                    return ClassifySymbol(memberSymbol, modifiers);
                }
            }
        }

        // 默认为变量
        return (Array.IndexOf(TokenTypes, SemanticTokenType.Variable), modifiers.ToArray());
    }

    /// <summary>
    /// 根据符号信息分类
    /// </summary>
    private (int? tokenType, SemanticTokenModifier[] modifiers) ClassifySymbol(
        SymbolInfo symbol, List<SemanticTokenModifier> modifiers)
    {
        // 添加静态修饰符
        if (symbol.IsStatic)
        {
            modifiers.Add(new SemanticTokenModifier(SemanticTokenModifier.Static));
        }

        // 根据符号类型分类
        switch (symbol.Kind)
        {
            case Old8Lang.LanguageServer.Models.SymbolKind.Class:
                modifiers.Add(new SemanticTokenModifier(SemanticTokenModifier.Declaration));
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Class), modifiers.ToArray());

            case Old8Lang.LanguageServer.Models.SymbolKind.Function:
                modifiers.Add(new SemanticTokenModifier(SemanticTokenModifier.Declaration));
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Function), modifiers.ToArray());

            case Old8Lang.LanguageServer.Models.SymbolKind.Method:
                modifiers.Add(new SemanticTokenModifier(SemanticTokenModifier.Declaration));
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Method), modifiers.ToArray());

            case Old8Lang.LanguageServer.Models.SymbolKind.Property:
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Property), modifiers.ToArray());

            case Old8Lang.LanguageServer.Models.SymbolKind.Parameter:
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Parameter), modifiers.ToArray());

            case Old8Lang.LanguageServer.Models.SymbolKind.Variable:
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Variable), modifiers.ToArray());

            case Old8Lang.LanguageServer.Models.SymbolKind.Constant:
                modifiers.Add(new SemanticTokenModifier(SemanticTokenModifier.Readonly));
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Variable), modifiers.ToArray());

            default:
                return (Array.IndexOf(TokenTypes, SemanticTokenType.Variable), modifiers.ToArray());
        }
    }
}
