using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Old8Lang.Tests.LanguageServer.Handlers.Navigation;

/// <summary>
/// 跨文件定义/引用/重命名测试
/// </summary>
public class CrossFileNavigationTests : IDisposable
{
    private readonly string _root;
    private readonly DocumentManager _documentManager = new();
    private readonly WorkspaceIndex _index;

    private readonly string _definitionUri;
    private readonly string _usageUri;

    private const string Definitions = """
                                      func sharedAdd(a: int, b: int) -> int {
                                          return a + b
                                      }
                                      """;

    private const string Usage = """
                                 result <- sharedAdd(1, 2)
                                 PrintLine(result)
                                 """;

    public CrossFileNavigationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "old8lang-cross-file-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        var definitionPath = Path.Combine(_root, "defs.old8");
        var usagePath = Path.Combine(_root, "usage.old8");
        File.WriteAllText(definitionPath, Definitions);
        File.WriteAllText(usagePath, Usage);

        _definitionUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.FromFileSystemPath(definitionPath).ToString();
        _usageUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.FromFileSystemPath(usagePath).ToString();

        // 仅打开调用方文件，定义方只存在于磁盘，验证索引确实扫描了工作区
        _documentManager.UpdateDocument(_usageUri, Usage);

        _index = new WorkspaceIndex(_documentManager);
        _index.SetRoots([_root]);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    private ReferenceParams CreateReferenceRequest(bool includeDeclaration = true)
    {
        return new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(_usageUri) },
            Position = new Position(0, 12),
            Context = new ReferenceContext { IncludeDeclaration = includeDeclaration }
        };
    }

    [Fact]
    public async Task References_FromCallSite_ResolvesDefinitionInAnotherFile()
    {
        var handler = new ReferencesHandler(_documentManager, _index);

        var result = await handler.Handle(CreateReferenceRequest(), CancellationToken.None);

        Assert.NotNull(result);
        var locations = result!.ToList();
        Assert.Equal(2, locations.Count);
        Assert.Contains(locations, location => location.Uri.ToString() == _definitionUri);
        Assert.Contains(locations, location => location.Uri.ToString() == _usageUri);
    }

    [Fact]
    public async Task References_ExcludeDeclaration_OmitsDefinitionLocation()
    {
        var handler = new ReferencesHandler(_documentManager, _index);

        var result = await handler.Handle(CreateReferenceRequest(includeDeclaration: false), CancellationToken.None);

        var location = Assert.Single(result!.ToList());
        Assert.Equal(_usageUri, location.Uri.ToString());
    }

    [Fact]
    public async Task Definition_FromCallSite_JumpsToAnotherFile()
    {
        var handler = new DefinitionHandler(_documentManager, _index);

        var result = await handler.Handle(new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(_usageUri) },
            Position = new Position(0, 12)
        }, CancellationToken.None);

        Assert.NotNull(result);
        var location = Assert.Single(result!.ToList());
        Assert.True(location.IsLocation);
        Assert.Equal(_definitionUri, location.Location!.Uri.ToString());
        Assert.Equal(0, location.Location.Range.Start.Line);
    }

    [Fact]
    public async Task Rename_FromCallSite_EditsBothFiles()
    {
        var handler = new RenameHandler(_documentManager, _index);

        var result = await handler.Handle(new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(_usageUri) },
            Position = new Position(0, 12),
            NewName = "combine"
        }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.Changes);
        Assert.Equal(2, result.Changes!.Count());

        foreach (var edits in result.Changes.Values)
        {
            var edit = Assert.Single(edits);
            Assert.Equal("combine", edit.NewText);
        }
    }

    [Fact]
    public async Task WorkspaceSymbols_IncludesFilesNotOpened()
    {
        var handler = new WorkspaceSymbolHandler(_documentManager, _index);

        var result = await handler.Handle(new WorkspaceSymbolParams { Query = "sharedAdd" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(result!, symbol => symbol.Name == "sharedAdd");
    }
}
