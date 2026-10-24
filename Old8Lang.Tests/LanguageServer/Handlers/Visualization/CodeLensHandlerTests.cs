using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Old8Lang.Tests.LanguageServer.Handlers.Visualization;

/// <summary>
/// CodeLens 处理器测试
/// </summary>
public class CodeLensHandlerTests
{
    private const string Source = """
                                 func greet() -> void {
                                     PrintLine("hi")
                                 }

                                 greet()
                                 greet()

                                 class Greeter {
                                     public func hello() -> void {
                                         PrintLine("hello")
                                     }
                                 }

                                 g <- Greeter()
                                 g.hello()
                                 """;

    private static (CodeLensHandler Handler, string Uri) CreateHandler()
    {
        const string uri = "file:///codelens.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, Source);
        return (new CodeLensHandler(documentManager), uri);
    }

    [Fact]
    public async Task Handle_ProducesLensForFunctionAndClassAndMethod()
    {
        var (handler, uri) = CreateHandler();

        var result = await handler.Handle(new CodeLensParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        }, CancellationToken.None);

        Assert.NotNull(result);
        var lenses = result!.ToList();

        Assert.Equal(3, lenses.Count);
        Assert.Single(lenses, lens => lens.Command!.Title == "3 处引用");
        Assert.Equal(2, lenses.Count(lens => lens.Command!.Title == "2 处引用"));
    }

    [Fact]
    public async Task Handle_CommandUsesShowReferencesWithLocations()
    {
        var (handler, uri) = CreateHandler();

        var result = await handler.Handle(new CodeLensParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        }, CancellationToken.None);

        var lens = Assert.Single(result!, candidate => candidate.Command?.Title == "3 处引用");
        var arguments = lens.Command!.Arguments;

        Assert.NotNull(arguments);
        Assert.Equal(CodeLensHandler.ShowReferencesCommand, lens.Command!.Name);
        Assert.Equal(uri, arguments![0].ToString());

        var position = (Newtonsoft.Json.Linq.JObject)arguments[1]!;
        Assert.Equal(0, (int)position["line"]!);

        var locations = (Newtonsoft.Json.Linq.JArray)arguments[2]!;
        Assert.Equal(3, locations.Count);
        Assert.All(locations, location => Assert.Equal(uri, location["uri"]!.ToString()));
    }

    [Fact]
    public async Task Handle_LensRangeStartsAtDeclaration()
    {
        var (handler, uri) = CreateHandler();

        var result = await handler.Handle(new CodeLensParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        }, CancellationToken.None);

        // 三个声明：greet 函数、Greeter 类、hello 方法
        Assert.Equal(3, result!.Count());
        Assert.All(result!, lens => Assert.True(lens.Range.Start.Line >= 0));
    }

    [Fact]
    public async Task Handle_UnknownDocument_ReturnsEmptyContainer()
    {
        var handler = new CodeLensHandler(new DocumentManager());

        var result = await handler.Handle(new CodeLensParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri("file:///missing.old8") }
        }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!);
    }
}
