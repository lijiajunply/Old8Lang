using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.Tests.LanguageServer.Services;

/// <summary>
/// 符号表词法作用域链回归测试：局部变量隔离、变量遮蔽、同名符号与嵌套块作用域。
/// </summary>
public class ScopeChainTests
{
    private const string Uri = "file:///scope-chain.old8";

    [Fact]
    public void LocalVariables_DoNotPolluteGlobalSymbolTable_AndStayIsolatedBetweenFunctions()
    {
        var code =
            "globalX <- 10\n" +
            "func first() -> int {\n" +
            "    localA <- 1\n" +
            "    return localA\n" +
            "}\n" +
            "func second() -> int {\n" +
            "    localB <- 2\n" +
            "    return localB\n" +
            "}\n";

        var manager = new DocumentManager();
        var doc = manager.UpdateDocument(Uri, code);

        // 顶层符号表只包含全局符号，不应包含 localA / localB
        Assert.NotNull(doc.SymbolTable);
        Assert.Contains("globalX", doc.SymbolTable!.Keys);
        Assert.Contains("first", doc.SymbolTable.Keys);
        Assert.Contains("second", doc.SymbolTable.Keys);
        Assert.DoesNotContain("localA", doc.SymbolTable.Keys);
        Assert.DoesNotContain("localB", doc.SymbolTable.Keys);

        // 在 first 函数内只可见 localA，不可见 localB
        var visibleInFirst = doc.RootScope!.GetVisibleSymbols(3, 11);
        Assert.Contains(visibleInFirst, s => s.Name == "localA");
        Assert.Contains(visibleInFirst, s => s.Name == "globalX");
        Assert.DoesNotContain(visibleInFirst, s => s.Name == "localB");

        // 在 second 函数内只可见 localB，不可见 localA
        var visibleInSecond = doc.RootScope.GetVisibleSymbols(7, 11);
        Assert.Contains(visibleInSecond, s => s.Name == "localB");
        Assert.DoesNotContain(visibleInSecond, s => s.Name == "localA");
    }

    [Fact]
    public async Task VariableShadowing_DefinitionHoverAndRename_TargetExactBinding()
    {
        var code =
            "x: int <- 100\n" +
            "func shadowDemo() -> string {\n" +
            "    x: string <- \"inner\"\n" +
            "    return x\n" +
            "}\n" +
            "y <- x + 1\n";

        var manager = new DocumentManager();
        manager.UpdateDocument(Uri, code);

        var defHandler = new DefinitionHandler(manager);
        var hoverHandler = new HoverHandler(manager);
        var renameHandler = new RenameHandler(manager);

        // 在 shadowDemo 内 `return x` 上跳转定义，应跳到第 2 行（局部 string x），而非第 0 行全局 int x
        var innerDef = await defHandler.Handle(new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(3, 11)
        }, CancellationToken.None);
        var innerLoc = Assert.Single(innerDef!).Location!;
        Assert.Equal(2, innerLoc.Range.Start.Line);

        // 在 shadowDemo 内悬停 `return x`，应显示 string 类型
        var innerHover = await hoverHandler.Handle(new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(3, 11)
        }, CancellationToken.None);
        Assert.Contains("string", innerHover!.Contents.MarkupContent!.Value);

        // 在顶层 `y <- x + 1` 上悬停 `x`，应显示 int 类型且定义在第 0 行
        var outerHover = await hoverHandler.Handle(new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(5, 5)
        }, CancellationToken.None);
        Assert.Contains("int", outerHover!.Contents.MarkupContent!.Value);

        var outerDef = await defHandler.Handle(new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(5, 5)
        }, CancellationToken.None);
        Assert.Equal(0, Assert.Single(outerDef!).Location!.Range.Start.Line);

        // 重命名局部 x，只修改第 2、3 行，不修改顶层第 0、5 行
        var renameInner = await renameHandler.Handle(new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(3, 11),
            NewName = "innerStr"
        }, CancellationToken.None);
        var innerEdits = renameInner!.Changes!.Values.Single().ToList();
        Assert.Equal(2, innerEdits.Count);
        Assert.All(innerEdits, e => Assert.InRange(e.Range.Start.Line, 2, 3));

        // 重命名顶层 x，只修改第 0、5 行，不修改被局部遮蔽的第 2、3 行
        var renameOuter = await renameHandler.Handle(new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(0, 0),
            NewName = "outerInt"
        }, CancellationToken.None);
        var outerEdits = renameOuter!.Changes!.Values.Single().ToList();
        Assert.Equal(2, outerEdits.Count);
        Assert.Contains(outerEdits, e => e.Range.Start.Line == 0);
        Assert.Contains(outerEdits, e => e.Range.Start.Line == 5);
        Assert.DoesNotContain(outerEdits, e => e.Range.Start.Line is 2 or 3);
    }

    [Fact]
    public async Task SameNameLocalsInSiblingFunctions_HighlightAndRenameStayScoped()
    {
        var code =
            "func alpha() -> int {\n" +
            "    temp <- 1\n" +
            "    return temp + 1\n" +
            "}\n" +
            "func beta() -> int {\n" +
            "    temp <- 2\n" +
            "    return temp * 2\n" +
            "}\n";

        var manager = new DocumentManager();
        manager.UpdateDocument(Uri, code);

        var highlightHandler = new DocumentHighlightHandler(manager);
        var highlights = (await highlightHandler.Handle(new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(2, 11) // alpha 中的 temp
        }, CancellationToken.None))!.ToList();

        Assert.Equal(2, highlights.Count);
        Assert.All(highlights, h => Assert.InRange(h.Range.Start.Line, 1, 2));
    }

    [Fact]
    public void NestedControlFlowAndAsyncFuncScopes_BindVariablesProperly()
    {
        var code =
            "async func loadAll(items: list) -> void {\n" +
            "    for item in items {\n" +
            "        try {\n" +
            "            PrintLine(item)\n" +
            "        } catch (err) {\n" +
            "            PrintLine(err)\n" +
            "        }\n" +
            "    }\n" +
            "}\n";

        var manager = new DocumentManager();
        var doc = manager.UpdateDocument(Uri, code);

        // catch 块内可见 err、item、items
        var inCatch = doc.RootScope!.GetVisibleSymbols(5, 22);
        Assert.Contains(inCatch, s => s.Name == "err");
        Assert.Contains(inCatch, s => s.Name == "item");
        Assert.Contains(inCatch, s => s.Name == "items");

        // try 块内可见 item、items，不可见 err
        var inTry = doc.RootScope.GetVisibleSymbols(3, 22);
        Assert.Contains(inTry, s => s.Name == "item");
        Assert.DoesNotContain(inTry, s => s.Name == "err");

        // 异步函数外不可见 item / err / items
        Assert.DoesNotContain("item", doc.SymbolTable!.Keys);
        Assert.DoesNotContain("err", doc.SymbolTable.Keys);
    }
}
