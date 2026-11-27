using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.Tests.LanguageServer.Handlers.Navigation;

/// <summary>
/// CallHierarchyHandler 单元测试：覆盖函数/方法 prepare、单文件及跨文件 incomingCalls / outgoingCalls
/// 与顶层调用点。
/// </summary>
public class CallHierarchyHandlerTests
{
    [Fact]
    public async Task PrepareAndIncomingOutgoingCalls_SingleFileFunctionsAndMethods()
    {
        var code =
            "func leaf(x: int) -> int {\n" +
            "    return x * 2\n" +
            "}\n" +
            "\n" +
            "func middle(n: int) -> int {\n" +
            "    a <- leaf(n)\n" +
            "    b <- leaf(n + 1)\n" +
            "    return a + b\n" +
            "}\n" +
            "\n" +
            "func entry() -> int {\n" +
            "    return middle(5)\n" +
            "}\n" +
            "\n" +
            "res <- entry()\n";

        var documentManager = new DocumentManager();
        var workspaceIndex = new WorkspaceIndex(documentManager);
        documentManager.AttachWorkspaceIndex(workspaceIndex);

        const string uri = "file:///call-hierarchy-single.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CallHierarchyHandler(documentManager, workspaceIndex);

        // Prepare on `middle` (line 4, col 5)
        var prepared = await handler.Handle(new CallHierarchyPrepareParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.From(uri)),
            Position = new Position(4, 6)
        }, CancellationToken.None);

        Assert.NotNull(prepared);
        var middleItem = Assert.Single(prepared);
        Assert.Equal("middle", middleItem.Name);
        Assert.Equal(SymbolKind.Function, middleItem.Kind);

        // Incoming calls to `middle`: called by `entry`
        var incoming = await handler.Handle(new CallHierarchyIncomingCallsParams
        {
            Item = middleItem
        }, CancellationToken.None);

        Assert.NotNull(incoming);
        var caller = Assert.Single(incoming);
        Assert.Equal("entry", caller.From.Name);
        Assert.Single(caller.FromRanges);

        // Outgoing calls from `middle`: calls `leaf` twice
        var outgoing = await handler.Handle(new CallHierarchyOutgoingCallsParams
        {
            Item = middleItem
        }, CancellationToken.None);

        Assert.NotNull(outgoing);
        var callee = Assert.Single(outgoing);
        Assert.Equal("leaf", callee.To.Name);
        Assert.Equal(2, callee.FromRanges.Count());

        // Incoming calls to `entry`: called from top-level script
        var entryPrepared = await handler.Handle(new CallHierarchyPrepareParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.From(uri)),
            Position = new Position(10, 6)
        }, CancellationToken.None);
        var entryItem = Assert.Single(entryPrepared!);
        var entryIncoming = await handler.Handle(new CallHierarchyIncomingCallsParams
        {
            Item = entryItem
        }, CancellationToken.None);

        Assert.NotNull(entryIncoming);
        Assert.Contains(entryIncoming, c => c.From.Kind == SymbolKind.File && c.FromRanges.Count() == 1);
    }

    [Fact]
    public async Task CrossFileAndClassMethod_CallHierarchyWorks()
    {
        var libCode =
            "class Service {\n" +
            "    public func format(v: int) -> string {\n" +
            "        return v.ToStr()\n" +
            "    }\n" +
            "    public func process(v: int) -> string {\n" +
            "        return this.format(v)\n" +
            "    }\n" +
            "}\n" +
            "func helper(x: int) -> int {\n" +
            "    return x + 1\n" +
            "}\n";

        var appCode =
            "func run() -> string {\n" +
            "    svc <- Service()\n" +
            "    n <- helper(10)\n" +
            "    return svc.process(n)\n" +
            "}\n";

        var documentManager = new DocumentManager();
        var workspaceIndex = new WorkspaceIndex(documentManager);
        documentManager.AttachWorkspaceIndex(workspaceIndex);

        const string libUri = "file:///call-lib.old8";
        const string appUri = "file:///call-app.old8";
        documentManager.UpdateDocument(libUri, libCode);
        documentManager.UpdateDocument(appUri, appCode);

        var handler = new CallHierarchyHandler(documentManager, workspaceIndex);

        // Prepare on `Service.process` (line 4, col 16)
        var processPrepared = await handler.Handle(new CallHierarchyPrepareParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.From(libUri)),
            Position = new Position(4, 17)
        }, CancellationToken.None);
        var processItem = Assert.Single(processPrepared!);
        Assert.Equal("process", processItem.Name);
        Assert.Equal(SymbolKind.Method, processItem.Kind);

        // Outgoing from `Service.process` should include `this.format`
        var processOutgoing = await handler.Handle(new CallHierarchyOutgoingCallsParams
        {
            Item = processItem
        }, CancellationToken.None);
        Assert.Contains(processOutgoing!, o => o.To.Name == "format");

        // Incoming to `Service.process` should include cross-file `run`
        var processIncoming = await handler.Handle(new CallHierarchyIncomingCallsParams
        {
            Item = processItem
        }, CancellationToken.None);
        Assert.Contains(processIncoming!, i => i.From.Name == "run" && i.From.Uri.ToString() == appUri);

        // Outgoing from `run` should include both `helper` and `process`
        var runPrepared = await handler.Handle(new CallHierarchyPrepareParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.From(appUri)),
            Position = new Position(0, 6)
        }, CancellationToken.None);
        var runItem = Assert.Single(runPrepared!);
        var runOutgoing = await handler.Handle(new CallHierarchyOutgoingCallsParams
        {
            Item = runItem
        }, CancellationToken.None);

        Assert.Contains(runOutgoing!, o => o.To.Name == "helper" && o.To.Uri.ToString() == libUri);
        Assert.Contains(runOutgoing!, o => o.To.Name == "process" && o.To.Uri.ToString() == libUri);
    }
}
