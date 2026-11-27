using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Old8Lang.Tests.LanguageServer.Handlers.Visualization;

/// <summary>
/// InlineValuesHandler 单元测试：验证无快照时的标准 DAP 协议映射（VariableLookup / EvaluatableExpression）、
/// 调试变量快照联动时的 InlineValueText 渲染、StoppedLocation 截断以及快照清理。
/// </summary>
public class InlineValuesHandlerTests
{
    [Fact]
    public async Task WithoutSnapshot_EmitsVariableLookupAndEvaluatableExpressionUpToStoppedLocation()
    {
        var code =
            "class Box {\n" +           // 0
            "    public size: int <- 0\n" + // 1
            "}\n" +                     // 2
            "func calc(a: int, box: Box) -> int {\n" + // 3
            "    total <- a + box.size\n" + // 4
            "    afterStop <- total * 2\n" + // 5
            "    return afterStop\n" +   // 6
            "}\n";

        var documentManager = new DocumentManager();
        var debugService = new DebugProfilerService();
        const string uri = "file:///inline-values.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new InlineValuesHandler(documentManager, debugService);
        var result = await handler.Handle(new InlineValueParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.From(uri)),
            Range = new LspRange(new Position(3, 0), new Position(7, 0)),
            Context = new InlineValueContext
            {
                FrameId = 1,
                StoppedLocation = new LspRange(new Position(4, 0), new Position(4, 26))
            }
        }, CancellationToken.None);

        Assert.NotNull(result);
        var items = result.ToList();

        // 参数 a / 局部变量 total 应生成 InlineValueVariableLookup
        Assert.Contains(items, i => i is InlineValueVariableLookup { VariableName: "a" });
        Assert.Contains(items, i => i is InlineValueVariableLookup { VariableName: "total" });
        // 成员表达式 box.size 应生成 InlineValueEvaluatableExpression
        Assert.Contains(items, i => i is InlineValueEvaluatableExpression { Expression: "box.size" });
        // 暂停点之后的行（line 5 的 afterStop）不得提前输出
        Assert.DoesNotContain(items, i => i is InlineValueVariableLookup { VariableName: "afterStop" });
    }

    [Fact]
    public async Task WithDebugSnapshot_EmitsInlineValueTextAndClearsOnResume()
    {
        var code =
            "func add(x: int, y: int) -> int {\n" + // 0
            "    sum <- x + y\n" +                   // 1
            "    return sum\n" +                     // 2
            "}\n";

        var documentManager = new DocumentManager();
        var debugService = new DebugProfilerService();
        const string uri = "file:///inline-snapshot.old8";
        documentManager.UpdateDocument(uri, code);

        var snapshotHandler = new UpdateDebugSnapshotHandler(debugService);
        var updated = await snapshotHandler.Handle(new UpdateDebugSnapshotRequest
        {
            Uri = uri,
            FrameId = 1,
            StoppedLine = 2,
            Variables = new Dictionary<string, string>
            {
                ["x"] = "10",
                ["y"] = "25",
                ["sum"] = "35"
            }
        }, CancellationToken.None);
        Assert.True(updated);

        var handler = new InlineValuesHandler(documentManager, debugService);
        var result = await handler.Handle(new InlineValueParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.From(uri)),
            Range = new LspRange(new Position(0, 0), new Position(3, 0)),
            Context = new InlineValueContext
            {
                FrameId = 1,
                StoppedLocation = new LspRange(new Position(2, 0), new Position(2, 14))
            }
        }, CancellationToken.None);

        Assert.NotNull(result);
        var items = result.ToList();
        Assert.Contains(items, i => i is InlineValueText { Text: "x = 10" });
        Assert.Contains(items, i => i is InlineValueText { Text: "y = 25" });
        Assert.Contains(items, i => i is InlineValueText { Text: "sum = 35" });

        // 清除快照后回退为标准 VariableLookup
        await snapshotHandler.Handle(new UpdateDebugSnapshotRequest
        {
            Uri = uri,
            FrameId = 1,
            Clear = true
        }, CancellationToken.None);

        var afterClear = await handler.Handle(new InlineValueParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.From(uri)),
            Range = new LspRange(new Position(0, 0), new Position(3, 0)),
            Context = new InlineValueContext
            {
                FrameId = 1,
                StoppedLocation = new LspRange(new Position(2, 0), new Position(2, 14))
            }
        }, CancellationToken.None);

        Assert.DoesNotContain(afterClear!, i => i is InlineValueText);
        Assert.Contains(afterClear!, i => i is InlineValueVariableLookup { VariableName: "sum" });
    }
}
