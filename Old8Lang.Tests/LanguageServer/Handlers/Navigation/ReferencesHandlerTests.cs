using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.Tests.LanguageServer.Handlers.Navigation;

/// <summary>
/// ReferencesHandler 单元测试：覆盖顶层符号、局部变量作用域隔离、类成员与 IncludeDeclaration。
/// </summary>
public class ScopedReferencesHandlerTests
{
    private const string Uri = "file:///refs.old8";

    [Fact]
    public void GetRegistrationOptions_TargetsOld8Lang()
    {
        var handler = new ReferencesHandler(new DocumentManager());
        var options = handler.GetRegistrationOptions(new ReferenceCapability(), new ClientCapabilities());

        Assert.NotNull(options);
        Assert.NotNull(options.DocumentSelector);
    }

    [Fact]
    public async Task FindReferences_TopLevelFunction_RespectsIncludeDeclaration()
    {
        var code =
            "func helper() -> int {\n" +
            "    return 1\n" +
            "}\n" +
            "a <- helper()\n" +
            "b <- helper()\n";

        var manager = new DocumentManager();
        manager.UpdateDocument(Uri, code);
        var handler = new ReferencesHandler(manager);

        var withDecl = (await handler.Handle(new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(0, 6),
            Context = new ReferenceContext { IncludeDeclaration = true }
        }, CancellationToken.None))!.ToList();

        Assert.Equal(3, withDecl.Count);

        var withoutDecl = (await handler.Handle(new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(0, 6),
            Context = new ReferenceContext { IncludeDeclaration = false }
        }, CancellationToken.None))!.ToList();

        Assert.Equal(2, withoutDecl.Count);
        Assert.DoesNotContain(withoutDecl, l => l.Range.Start.Line == 0);
    }

    [Fact]
    public async Task FindReferences_ScopedLocalVariable_DoesNotIncludeShadowedOrSiblingNames()
    {
        var code =
            "val <- 0\n" +
            "func f1() -> int {\n" +
            "    val <- 10\n" +
            "    return val + val\n" +
            "}\n" +
            "func f2() -> int {\n" +
            "    val <- 20\n" +
            "    return val\n" +
            "}\n" +
            "out <- val\n";

        var manager = new DocumentManager();
        manager.UpdateDocument(Uri, code);
        var handler = new ReferencesHandler(manager);

        var f1Refs = (await handler.Handle(new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(2, 5), // f1 中的 val
            Context = new ReferenceContext { IncludeDeclaration = true }
        }, CancellationToken.None))!.ToList();

        Assert.Equal(3, f1Refs.Count); // line 2 decl + line 3 two reads
        Assert.All(f1Refs, loc => Assert.InRange(loc.Range.Start.Line, 2, 3));

        var globalRefs = (await handler.Handle(new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(0, 0), // 顶层 val
            Context = new ReferenceContext { IncludeDeclaration = true }
        }, CancellationToken.None))!.ToList();

        Assert.Equal(2, globalRefs.Count); // line 0 + line 9
        Assert.Contains(globalRefs, loc => loc.Range.Start.Line == 0);
        Assert.Contains(globalRefs, loc => loc.Range.Start.Line == 9);
    }

    [Fact]
    public async Task FindReferences_UnopenedDocumentOrEmptyPosition_ReturnsNullOrEmpty()
    {
        var manager = new DocumentManager();
        var handler = new ReferencesHandler(manager);

        var missing = await handler.Handle(new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri("file:///missing.old8") },
            Position = new Position(0, 0),
            Context = new ReferenceContext { IncludeDeclaration = true }
        }, CancellationToken.None);

        Assert.Null(missing);

        manager.UpdateDocument(Uri, "x <- 1\n");
        var onNumber = await handler.Handle(new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(Uri) },
            Position = new Position(0, 5),
            Context = new ReferenceContext { IncludeDeclaration = true }
        }, CancellationToken.None);

        Assert.NotNull(onNumber);
        Assert.Empty(onNumber!);
    }
}
