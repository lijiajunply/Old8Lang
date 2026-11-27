using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using ModelSymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// Inline Values 处理器（LSP 3.17 <c>textDocument/inlineValue</c>）。
/// 与调试运行时状态建立双层协议映射：
/// 1. 若 <see cref="DebugProfilerService"/> 中存在当前文档/栈帧的活跃变量快照（由 DAP 跟踪器或 LSP 调试会话提供），
///    直接输出 <see cref="InlineValueText"/> 显示运行时值；
/// 2. 对其余可见变量与成员表达式输出标准 <see cref="InlineValueVariableLookup"/> 与
///    <see cref="InlineValueEvaluatableExpression"/>，交由 VS Code 向活跃 DAP 会话查询/求值。
/// </summary>
public class InlineValuesHandler(
    DocumentManager documentManager,
    DebugProfilerService? debugProfilerService = null,
    WorkspaceIndex? workspaceIndex = null) : InlineValuesHandlerBase
{
    protected override InlineValueRegistrationOptions CreateRegistrationOptions(
        InlineValueClientCapabilities capability,
        ClientCapabilities clientCapabilities)
    {
        return new InlineValueRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public override Task<Container<InlineValueBase>?> Handle(
        InlineValueParams request,
        CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);
        if (document?.Tokens == null || document.Tokens.Count == 0)
        {
            return Task.FromResult<Container<InlineValueBase>?>(new Container<InlineValueBase>());
        }

        var startLine = Math.Max(0, (int)request.Range.Start.Line);
        var endLine = Math.Max(startLine, (int)request.Range.End.Line);

        // 只展示暂停位置及之前的变量值，不提前剧透未执行代码
        if (request.Context?.StoppedLocation != null)
        {
            var stoppedEndLine = (int)request.Context.StoppedLocation.End.Line;
            if (stoppedEndLine < endLine)
            {
                endLine = stoppedEndLine;
            }
        }

        if (endLine < startLine)
        {
            return Task.FromResult<Container<InlineValueBase>?>(new Container<InlineValueBase>());
        }

        var snapshot = debugProfilerService?.GetVariableSnapshot(uri, request.Context?.FrameId);
        var effectiveWorkspace = workspaceIndex ?? documentManager.WorkspaceIndex;
        var tokens = document.Tokens;
        var (startIdx, endIdx) = document.TokenIndex.GetTokenRangeForLines(startLine, endLine);

        var results = new List<InlineValueBase>();
        var emittedPerLine = new HashSet<(int Line, string Key)>();

        for (var i = startIdx; i < endIdx; i++)
        {
            var token = tokens[i];
            if (token.Type != LangTokenType.Identifier)
            {
                continue;
            }

            var line = token.Line - 1;
            var col = token.Column;

            if (line < startLine || line > endLine)
            {
                continue;
            }

            // 跳过位于 StoppedLocation 之后的同行 token
            if (request.Context?.StoppedLocation != null &&
                line == request.Context.StoppedLocation.End.Line &&
                col > request.Context.StoppedLocation.End.Character)
            {
                continue;
            }

            // 跳过声明关键字后直接跟随的符号名（func foo / class Bar / interface / enum / mixin）
            if (i > 0 && tokens[i - 1].Type is LangTokenType.Func or LangTokenType.Class
                or LangTokenType.Interface or LangTokenType.Enum or LangTokenType.Mixin)
            {
                continue;
            }

            // 跳过函数/方法调用名 `foo(...)`
            if (i + 1 < tokens.Count && tokens[i + 1].Type == LangTokenType.LeftParen)
            {
                continue;
            }

            // 跳过类型注解冒号后的类型名（如 `x: int`、`a: MyClass`）
            if (i > 0 && tokens[i - 1].Type is LangTokenType.Colon or LangTokenType.Arrow)
            {
                continue;
            }

            // 处理简单成员读取表达式 `obj.prop` -> InlineValueEvaluatableExpression 或快照 InlineValueText
            if (i > 1 && tokens[i - 1].Type == LangTokenType.Dot &&
                tokens[i - 2].Type is LangTokenType.Identifier or LangTokenType.This)
            {
                var receiverToken = tokens[i - 2];
                var exprText = $"{receiverToken.Value}.{token.Value}";
                if (!emittedPerLine.Add((line, exprText)))
                {
                    continue;
                }

                var exprRange = new LspRange(
                    new Position(receiverToken.Line - 1, receiverToken.Column),
                    new Position(line, col + token.Value.Length));

                if (snapshot != null && snapshot.Variables.TryGetValue(exprText, out var memberVal))
                {
                    results.Add(new InlineValueText
                    {
                        Range = exprRange,
                        Text = $"{exprText} = {memberVal}"
                    });
                }
                else
                {
                    results.Add(new InlineValueEvaluatableExpression
                    {
                        Range = exprRange,
                        Expression = exprText
                    });
                }

                continue;
            }

            // 普通标识符：确认在作用域中解析为变量/参数/字段/常量
            var symbol = SymbolFinder.FindSymbolAtPosition(document, line, col, effectiveWorkspace);
            if (symbol == null ||
                symbol.Kind is not (ModelSymbolKind.Variable or ModelSymbolKind.Parameter
                    or ModelSymbolKind.Constant or ModelSymbolKind.Field or ModelSymbolKind.Property))
            {
                continue;
            }

            if (!emittedPerLine.Add((line, token.Value)))
            {
                continue;
            }

            var range = new LspRange(
                new Position(line, col),
                new Position(line, col + token.Value.Length));

            if (snapshot != null && snapshot.Variables.TryGetValue(token.Value, out var runtimeValue))
            {
                results.Add(new InlineValueText
                {
                    Range = range,
                    Text = $"{token.Value} = {runtimeValue}"
                });
            }
            else
            {
                results.Add(new InlineValueVariableLookup
                {
                    Range = range,
                    VariableName = token.Value,
                    CaseSensitiveLookup = true
                });
            }
        }

        return Task.FromResult<Container<InlineValueBase>?>(new Container<InlineValueBase>(results));
    }
}
