using System.Xml.Linq;

namespace Old8Lang.Tests.Architecture;

/// <summary>
/// 固定阶段 2 开始前的项目引用图，以及阶段 1 partial 执行代码的归档位置。
/// </summary>
public class ArchitectureProjectDependencyTests
{
    [Fact]
    public void Runtime_ReferencesOnlySharedLayers_AndNotOfficialModules()
    {
        var root = RequireRepositoryRoot();
        var references = GetProjectReferences(root, "Old8Lang.Runtime/Old8Lang.Runtime.csproj");

        Assert.Equal(
            [
                "Old8Lang.Concurrency/Old8Lang.Concurrency.csproj",
                "Old8Lang.Core/Old8Lang.Core.csproj",
                "Old8Lang.Modules.Abstractions/Old8Lang.Modules.Abstractions.csproj",
                "Old8Lang.Observability/Old8Lang.Observability.csproj",
                "Old8Lang.ProjectSystem/Old8Lang.ProjectSystem.csproj"
            ],
            references);

        foreach (var sharedProject in references)
        {
            Assert.DoesNotContain(
                "Old8Lang.Runtime/Old8Lang.Runtime.csproj",
                GetProjectReferences(root, sharedProject));
        }
    }

    [Fact]
    public void OfficialModules_DependOnRuntime_NotTheReverse()
    {
        var root = RequireRepositoryRoot();
        var runtimeReferences = GetProjectReferences(root, "Old8Lang.Runtime/Old8Lang.Runtime.csproj");
        var officialReferences = GetProjectReferences(root, "Old8Lang.Modules.Official/Old8Lang.Modules.Official.csproj");

        Assert.DoesNotContain("Old8Lang.Modules.Official/Old8Lang.Modules.Official.csproj", runtimeReferences);
        Assert.Contains("Old8Lang.Runtime/Old8Lang.Runtime.csproj", officialReferences);
    }

    [Fact]
    public void LegacyPartialExecutionExtensions_AreArchivedOutsideNodes()
    {
        var root = RequireRepositoryRoot();
        var executionRoot = Path.Combine(root, "Old8Lang.Runtime", "Execution");
        var legacyRoot = Path.Combine(executionRoot, "LegacyNodeExtensions");
        var oldNodesRoot = Path.Combine(executionRoot, "Nodes");

        var archivedFiles = Directory.EnumerateFiles(legacyRoot, "*.Split.cs", SearchOption.AllDirectories).ToArray();
        Assert.Equal(31, archivedFiles.Length);
        Assert.Empty(Directory.EnumerateFiles(oldNodesRoot, "*.Split.cs", SearchOption.AllDirectories));

        Assert.All(archivedFiles, file =>
            Assert.Contains($"{Path.DirectorySeparatorChar}LegacyNodeExtensions{Path.DirectorySeparatorChar}", file,
                StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> GetProjectReferences(string root, string projectRelativePath)
    {
        var projectPath = Path.Combine(root, projectRelativePath);
        var document = XDocument.Load(projectPath);
        return document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            // MSBuild 项目文件统一使用 Windows 风格的相对路径；在 Unix 上先规范化分隔符。
            .Select(include => include!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))
            .Select(include => Path.GetRelativePath(root, Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, include))))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static string RequireRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Old8Lang.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("找不到 Old8Lang.sln。");
    }
}
