using Old8Lang.LanguageServer.Services;
using Xunit;

namespace Old8Lang.Tests.LanguageServer.Services;

/// <summary>
/// WorkspaceIndex 跨文件索引测试
/// </summary>
public class WorkspaceIndexTests : IDisposable
{
    private readonly string _root;

    public WorkspaceIndexTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "old8lang-workspace-index-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
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

    private string WriteFile(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void GetFiles_ScansDiskAndSkipsExcludedDirectories()
    {
        WriteFile("a.old8", "func alpha() -> int {\n    return 1\n}\n");
        WriteFile("bin/skip.old8", "func skipped() -> int {\n    return 2\n}\n");

        var documentManager = new DocumentManager();
        var index = new WorkspaceIndex(documentManager);
        index.SetRoots([_root]);

        var files = index.GetFiles();

        Assert.Single(files);
        Assert.Contains(files, file => file.FilePath != null && file.FilePath.EndsWith("a.old8"));
        Assert.Contains(files, file => file.Symbols.ContainsKey("alpha"));
    }

    [Fact]
    public void GetFiles_OpenDocumentOverridesDiskContent()
    {
        var path = WriteFile("a.old8", "func onDisk() -> int {\n    return 1\n}\n");
        var uri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.FromFileSystemPath(path).ToString();

        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, "func inEditor() -> int {\n    return 2\n}\n");

        var index = new WorkspaceIndex(documentManager);
        index.SetRoots([_root]);

        var file = Assert.Single(index.GetFiles());
        Assert.True(file.IsOpen);
        Assert.Contains("inEditor", file.Symbols.Keys);
        Assert.DoesNotContain("onDisk", file.Symbols.Keys);
    }

    [Fact]
    public void TryResolveSymbol_FindsSymbolInAnotherFile()
    {
        var definitionPath = WriteFile("defs.old8", "func sharedAdd(a: int, b: int) -> int {\n    return a + b\n}\n");
        var definitionUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.FromFileSystemPath(definitionPath).ToString();

        var documentManager = new DocumentManager();
        var index = new WorkspaceIndex(documentManager);
        index.SetRoots([_root]);

        Assert.True(index.TryResolveSymbol("sharedAdd", null, out var symbol, out var file));
        Assert.NotNull(symbol);
        Assert.NotNull(file);
        Assert.Equal(definitionUri, symbol!.Location.Uri);
        Assert.Equal(0, symbol.Location.Line);
    }

    [Fact]
    public void FindReferences_CollectsAcrossFiles()
    {
        var definitionPath = WriteFile("defs.old8", "func sharedAdd(a: int, b: int) -> int {\n    return a + b\n}\n");
        var definitionUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.FromFileSystemPath(definitionPath).ToString();
        WriteFile("usage.old8", "result <- sharedAdd(1, 2)\nPrintLine(result)\n");

        var documentManager = new DocumentManager();
        var index = new WorkspaceIndex(documentManager);
        index.SetRoots([_root]);

        var references = index.FindReferences("sharedAdd", definitionUri);

        Assert.Equal(2, references.Count);
        Assert.Contains(references, location => location.Uri == definitionUri);
        Assert.Contains(references, location => location.Uri != definitionUri);
    }

    [Fact]
    public void FindReferences_SkipsParameterDeclarations()
    {
        WriteFile("defs.old8",
            "func sharedAdd(a: int, b: int) -> int {\n    return a + b\n}\n");

        var documentManager = new DocumentManager();
        var index = new WorkspaceIndex(documentManager);
        index.SetRoots([_root]);

        // 签名中的参数声明不应计入引用，但函数体里对 a 的使用应计入
        var references = index.FindReferences("a");

        var location = Assert.Single(references);
        Assert.Equal(1, location.Line);
    }

    [Fact]
    public void FindReferences_SkipsFilesDeclaringTheirOwnSymbol()
    {
        var definitionPath = WriteFile("defs.old8", "func helper() -> int {\n    return 1\n}\n");
        var definitionUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.FromFileSystemPath(definitionPath).ToString();

        // 另一个独立脚本也定义了 helper，应整体跳过，避免跨程序误报
        WriteFile("other.old8", "func helper() -> int {\n    return 2\n}\n\nhelper()\n");

        var documentManager = new DocumentManager();
        var index = new WorkspaceIndex(documentManager);
        index.SetRoots([_root]);

        var references = index.FindReferences("helper", definitionUri);

        Assert.Single(references);
        Assert.Equal(definitionUri, references[0].Uri);
    }

    [Fact]
    public void FindRenameLocations_GroupsByFile()
    {
        var definitionPath = WriteFile("defs.old8", "func sharedAdd(a: int, b: int) -> int {\n    return a + b\n}\n");
        var definitionUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.FromFileSystemPath(definitionPath).ToString();
        WriteFile("usage.old8", "sharedAdd(1, 2)\n");

        var documentManager = new DocumentManager();
        var index = new WorkspaceIndex(documentManager);
        index.SetRoots([_root]);

        var locations = index.FindRenameLocations("sharedAdd", definitionUri);

        Assert.Equal(2, locations.Count);
        Assert.Single(locations[definitionUri]);
    }

    [Fact]
    public void NoRoots_FallsBackToOpenDocumentsOnly()
    {
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument("file:///only-open.old8", "func onlyOpen() -> int {\n    return 1\n}\n");

        var index = new WorkspaceIndex(documentManager);

        var file = Assert.Single(index.GetFiles());
        Assert.True(file.IsOpen);
        Assert.Contains("onlyOpen", file.Symbols.Keys);
    }
}
