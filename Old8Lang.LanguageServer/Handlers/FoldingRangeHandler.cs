using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LangParser;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 代码折叠处理器 - 提供代码块、函数、类的折叠功能
/// </summary>
public class FoldingRangeHandler : FoldingRangeHandlerBase
{
    private readonly DocumentManager _documentManager;

    public FoldingRangeHandler(DocumentManager documentManager)
    {
        _documentManager = documentManager;
    }

    public override Task<Container<FoldingRange>?> Handle(FoldingRangeRequestParam request, CancellationToken cancellationToken)
    {
        var document = _documentManager.GetDocument(request.TextDocument.Uri.ToString());
        if (document == null || document.Tokens == null)
        {
            return Task.FromResult<Container<FoldingRange>?>(null);
        }

        var foldingRanges = new List<FoldingRange>();

        // 1. 处理花括号折叠（函数、类、代码块）
        ProcessBraceFolding(document.Tokens, foldingRanges);

        // 2. 处理注释折叠
        ProcessCommentFolding(document.Tokens, foldingRanges);

        return Task.FromResult<Container<FoldingRange>?>(foldingRanges);
    }

    protected override FoldingRangeRegistrationOptions CreateRegistrationOptions(
        FoldingRangeCapability capability, ClientCapabilities clientCapabilities)
    {
        return new FoldingRangeRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    /// <summary>
    /// 处理花括号折叠
    /// </summary>
    private void ProcessBraceFolding(List<LangToken> tokens, List<FoldingRange> foldingRanges)
    {
        var braceStack = new Stack<(int line, int column, LangTokenType? contextType)>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            // 记录左花括号及其上下文
            if (token.Type == LangTokenType.LeftBrace)
            {
                // 查找上下文类型（func, class, if, for, while等）
                LangTokenType? contextType = null;
                for (int j = i - 1; j >= 0 && j >= i - 10; j--)
                {
                    var prevToken = tokens[j];
                    if (prevToken.Type == LangTokenType.Func ||
                        prevToken.Type == LangTokenType.Async ||
                        prevToken.Type == LangTokenType.Class ||
                        prevToken.Type == LangTokenType.If ||
                        prevToken.Type == LangTokenType.Elif ||
                        prevToken.Type == LangTokenType.Else ||
                        prevToken.Type == LangTokenType.For ||
                        prevToken.Type == LangTokenType.While ||
                        prevToken.Type == LangTokenType.Switch ||
                        prevToken.Type == LangTokenType.Try ||
                        prevToken.Type == LangTokenType.Using ||
                        prevToken.Type == LangTokenType.Select)
                    {
                        contextType = prevToken.Type;
                        break;
                    }
                }

                braceStack.Push((token.Line - 1, token.Column - 1, contextType));
            }
            // 匹配右花括号
            else if (token.Type == LangTokenType.RightBrace)
            {
                if (braceStack.Count > 0)
                {
                    var (startLine, startColumn, contextType) = braceStack.Pop();
                    var endLine = token.Line - 1; // 转换为 0-based

                    // 只有当开始和结束不在同一行时才创建折叠区域
                    if (endLine > startLine)
                    {
                        var foldingKind = DetermineFoldingKind(contextType);

                        foldingRanges.Add(new FoldingRange
                        {
                            StartLine = startLine,
                            StartCharacter = startColumn,
                            EndLine = endLine,
                            EndCharacter = token.Column - 1 + token.Value.Length, // 转换为 0-based
                            Kind = foldingKind
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// 处理注释折叠
    /// </summary>
    private void ProcessCommentFolding(List<LangToken> tokens, List<FoldingRange> foldingRanges)
    {
        int? commentBlockStart = null;
        int? previousCommentLine = null;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            // 文档注释
            if (token.Type == LangTokenType.DocComment)
            {
                var currentLine = token.Line - 1; // 转换为 0-based

                // 如果是连续的注释行
                if (previousCommentLine.HasValue && currentLine == previousCommentLine.Value + 1)
                {
                    // 继续注释块
                    previousCommentLine = currentLine;
                }
                else
                {
                    // 如果之前有注释块，并且有多行，创建折叠区域
                    if (commentBlockStart.HasValue && previousCommentLine.HasValue &&
                        previousCommentLine.Value > commentBlockStart.Value)
                    {
                        foldingRanges.Add(new FoldingRange
                        {
                            StartLine = commentBlockStart.Value,
                            EndLine = previousCommentLine.Value,
                            Kind = FoldingRangeKind.Comment
                        });
                    }

                    // 开始新的注释块
                    commentBlockStart = currentLine;
                    previousCommentLine = currentLine;
                }
            }
        }

        // 处理最后一个注释块
        if (commentBlockStart.HasValue && previousCommentLine.HasValue &&
            previousCommentLine.Value > commentBlockStart.Value)
        {
            foldingRanges.Add(new FoldingRange
            {
                StartLine = commentBlockStart.Value,
                EndLine = previousCommentLine.Value,
                Kind = FoldingRangeKind.Comment
            });
        }
    }

    /// <summary>
    /// 根据上下文类型确定折叠类型
    /// </summary>
    private FoldingRangeKind? DetermineFoldingKind(LangTokenType? contextType)
    {
        if (contextType == null)
        {
            return FoldingRangeKind.Region;
        }

        return contextType switch
        {
            LangTokenType.Class => FoldingRangeKind.Region,
            LangTokenType.Func => FoldingRangeKind.Region,
            LangTokenType.Async => FoldingRangeKind.Region,
            _ => FoldingRangeKind.Region
        };
    }
}
