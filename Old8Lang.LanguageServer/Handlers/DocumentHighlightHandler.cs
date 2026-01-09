using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LangParser;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 文档高亮处理器 - 高亮当前符号的所有出现位置
/// </summary>
public class DocumentHighlightHandler : DocumentHighlightHandlerBase
{
    private readonly DocumentManager _documentManager;

    public DocumentHighlightHandler(DocumentManager documentManager)
    {
        _documentManager = documentManager;
    }

    public override Task<DocumentHighlightContainer?> Handle(DocumentHighlightParams request, CancellationToken cancellationToken)
    {
        var document = _documentManager.GetDocument(request.TextDocument.Uri.ToString());
        if (document == null)
        {
            return Task.FromResult<DocumentHighlightContainer?>(null);
        }

        var line = (int)request.Position.Line;
        var column = (int)request.Position.Character;

        // 查找光标位置的符号
        var symbol = SymbolFinder.FindSymbolAtPosition(document, line, column);
        if (symbol == null)
        {
            return Task.FromResult<DocumentHighlightContainer?>(null);
        }

        // 查找符号的所有引用
        var references = SymbolFinder.FindReferences(document, symbol.Name);

        // 将引用转换为高亮
        var highlights = new List<DocumentHighlight>();

        // 添加定义位置
        highlights.Add(new DocumentHighlight
        {
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(symbol.Location.Line, symbol.Location.Column),
                new Position(symbol.Location.EndLine, symbol.Location.EndColumn)
            ),
            Kind = DocumentHighlightKind.Write // 定义位置视为写入
        });

        // 添加所有引用位置
        foreach (var reference in references)
        {
            // 跳过定义位置（避免重复）
            if (reference.Line == symbol.Location.Line &&
                reference.Column == symbol.Location.Column)
            {
                continue;
            }

            // 判断是读还是写
            var kind = DetermineAccessKind(document, reference);

            highlights.Add(new DocumentHighlight
            {
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(reference.Line, reference.Column),
                    new Position(reference.EndLine, reference.EndColumn)
                ),
                Kind = kind
            });
        }

        return Task.FromResult<DocumentHighlightContainer?>(highlights);
    }

    protected override DocumentHighlightRegistrationOptions CreateRegistrationOptions(
        DocumentHighlightCapability capability, ClientCapabilities clientCapabilities)
    {
        return new DocumentHighlightRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    /// <summary>
    /// 判断符号引用是读还是写
    /// </summary>
    private DocumentHighlightKind DetermineAccessKind(DocumentParseResult document, SourceLocation location)
    {
        if (document.Tokens == null)
        {
            return DocumentHighlightKind.Read;
        }

        // 查找对应位置的 token
        var tokenLine = location.Line + 1; // 转换为 1-based
        var tokenColumn = location.Column + 1; // 转换为 1-based

        for (int i = 0; i < document.Tokens.Count; i++)
        {
            var token = document.Tokens[i];
            if (token.Line == tokenLine && token.Column == tokenColumn)
            {
                // 检查下一个 token 是否是赋值运算符
                if (i + 1 < document.Tokens.Count)
                {
                    var nextToken = document.Tokens[i + 1];
                    if (nextToken.Type == LangTokenType.Assignment) // <-
                    {
                        return DocumentHighlightKind.Write;
                    }
                }

                // 默认为读取
                return DocumentHighlightKind.Read;
            }
        }

        return DocumentHighlightKind.Read;
    }
}
