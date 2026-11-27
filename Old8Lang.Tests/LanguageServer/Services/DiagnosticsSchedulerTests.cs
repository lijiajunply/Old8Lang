using System.Collections.Concurrent;
using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using MediatR;
using Moq;

namespace Old8Lang.Tests.LanguageServer.Services;

/// <summary>
/// 实时诊断防抖与后台调度测试：验证连续编辑合并、陈旧任务取消、
/// 按需解析最新文本、保存立即刷新与关闭清理诊断。
/// </summary>
public class DiagnosticsSchedulerTests
{
    [Fact]
    public async Task RapidChanges_CoalesceIntoSingleFinalDiagnosticPublish()
    {
        var documentManager = new DocumentManager();
        using var scheduler = new DiagnosticsScheduler(documentManager, new DiagnosticsSchedulerOptions
        {
            DebounceDelay = TimeSpan.FromMilliseconds(80),
            RunInBackground = true
        });

        var published = new ConcurrentQueue<(string Uri, DocumentParseResult Result)>();
        var finalPublished = new TaskCompletionSource<DocumentParseResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        const string uri = "file:///debounce-test.old8";

        // 连续快速发送 15 次编辑，中间穿插语法错误和未定义符号，最后一次为合法代码
        for (var i = 0; i < 14; i++)
        {
            await scheduler.ScheduleAsync(uri, $"x <- unknownVar_{i}\n", (u, r) => published.Enqueue((u, r)));
        }

        await scheduler.ScheduleAsync(uri, "x <- 42\nPrintLine(x)\n", (u, r) =>
        {
            published.Enqueue((u, r));
            finalPublished.TrySetResult(r);
        });

        var completed = await finalPublished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        // 等待少量缓冲确认不会有滞后的旧发布
        await Task.Delay(120);

        Assert.Equal("x <- 42\nPrintLine(x)\n", completed.Text);
        Assert.Empty(completed.Diagnostics);
        Assert.True(published.Count <= 2, $"期望防抖合并为 1~2 次发布，实际发布 {published.Count} 次");
    }

    [Fact]
    public async Task GetDocument_DuringDebounceWindow_ParsesStagedTextOnDemand()
    {
        var documentManager = new DocumentManager();
        using var scheduler = new DiagnosticsScheduler(documentManager, new DiagnosticsSchedulerOptions
        {
            DebounceDelay = TimeSpan.FromMilliseconds(500),
            RunInBackground = true
        });

        const string uri = "file:///ondemand.old8";
        documentManager.UpdateDocument(uri, "oldVar <- 1\n");

        // 触发防抖更新（500ms 后才会在后台发布诊断）
        await scheduler.ScheduleAsync(uri, "newVar <- 99\n", (_, _) => { });
        Assert.True(documentManager.HasPendingUpdate(uri));

        // 立即请求文档（模拟补全/悬停在防抖窗口内到达）应立刻拿到最新符号
        var latest = documentManager.GetDocument(uri);
        Assert.NotNull(latest);
        Assert.NotNull(latest.SymbolTable);
        Assert.True(latest.SymbolTable.ContainsKey("newVar"));
        Assert.False(latest.SymbolTable.ContainsKey("oldVar"));
        Assert.False(documentManager.HasPendingUpdate(uri));
    }

    [Fact]
    public async Task CloseDocument_CancelsPendingDebounceAndClearsDiagnostics()
    {
        var documentManager = new DocumentManager();
        using var scheduler = new DiagnosticsScheduler(documentManager, new DiagnosticsSchedulerOptions
        {
            DebounceDelay = TimeSpan.FromMilliseconds(120),
            RunInBackground = true
        });

        var published = new ConcurrentQueue<PublishDiagnosticsParams>();
        var textDocument = new Mock<ITextDocumentLanguageServer>();
        textDocument
            .Setup(x => x.SendNotification(It.IsAny<IRequest>()))
            .Callback<IRequest>(req =>
            {
                if (req is PublishDiagnosticsParams p)
                {
                    published.Enqueue(p);
                }
            });

        var facade = new Mock<ILanguageServerFacade>();
        facade.SetupGet(x => x.TextDocument).Returns(textDocument.Object);

        var handler = new TextDocumentSyncHandler(documentManager, facade.Object, scheduler);
        var uri = DocumentUri.From("file:///close-cancel.old8");

        await handler.Handle(new DidOpenTextDocumentParams
        {
            TextDocument = new TextDocumentItem
            {
                Uri = uri,
                LanguageId = "old8lang",
                Version = 1,
                Text = "x <- missingBeforeClose\n"
            }
        }, CancellationToken.None);

        // 触发防抖变更后立即关闭文档
        await handler.Handle(new DidChangeTextDocumentParams
        {
            TextDocument = new OptionalVersionedTextDocumentIdentifier { Uri = uri, Version = 2 },
            ContentChanges = new Container<TextDocumentContentChangeEvent>(
                new TextDocumentContentChangeEvent { Text = "y <- anotherMissing\n" })
        }, CancellationToken.None);

        await handler.Handle(new DidCloseTextDocumentParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = uri }
        }, CancellationToken.None);

        // 等过防抖窗口，确认被取消的后台任务不会在关闭后再次推送旧错误
        await Task.Delay(200);

        var list = published.ToArray();
        Assert.True(list.Length >= 2);
        Assert.Empty(list[^1].Diagnostics);
        Assert.Null(documentManager.GetDocument(uri.ToString()));
    }
}
