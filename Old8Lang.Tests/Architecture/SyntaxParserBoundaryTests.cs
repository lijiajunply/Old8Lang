using Old8Lang.Syntax;

namespace Old8Lang.Tests.Architecture;

/// <summary>
/// 阶段 2 门禁：语法模型与解析器都位于只依赖 Core 的 Old8Lang.Syntax 程序集。
/// </summary>
public class SyntaxParserBoundaryTests
{
    [Fact]
    public void ParserAndSyntaxNodes_AreDefinedInSyntaxAssembly()
    {
        var assembly = typeof(LiteralSyntax).Assembly;

        Assert.Same(assembly, typeof(Old8Lang.LangParser.LangParser).Assembly);
        Assert.Same(assembly, typeof(LangParser.Parsers.PrimaryParser).Assembly);
        Assert.Same(assembly, typeof(LangParser.Parsers.ExpressionParser).Assembly);
        Assert.Same(assembly, typeof(LangParser.Parsers.StatementParser).Assembly);
        Assert.Same(assembly, typeof(LangParser.Parsers.FunctionParser).Assembly);
        Assert.Same(assembly, typeof(LangParser.Parsers.ClassParser).Assembly);
        Assert.Same(assembly, typeof(LangParser.Parsers.LinqParser).Assembly);
        Assert.Same(assembly, typeof(LangParser.Parsers.ExtensionParser).Assembly);
        Assert.Same(assembly, typeof(LangParser.Core.ParserBase).Assembly);

        Assert.DoesNotContain(
            assembly.GetReferencedAssemblies(),
            reference => reference.Name is "Old8Lang.Runtime" or "Old8Lang.Hosting" or "Old8Lang.Modules.Official");
        Assert.Equal(["Old8Lang.Core"], assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("Old8Lang.", StringComparison.Ordinal) == true));
    }

    [Fact]
    public void SyntaxSources_DoNotReferenceExecutionBackends()
    {
        var root = FindRepositoryRoot();
        if (root is null) return;

        var syntaxRoot = Path.Combine(root, "Old8Lang.Syntax");
        Assert.True(Directory.Exists(syntaxRoot), $"找不到 Syntax 目录: {syntaxRoot}");

        var forbiddenTokens = new[]
        {
            "using Old8Lang.Interpreter",
            "using Old8Lang.Compiler",
            "using Old8Lang.Bytecode",
            "using Old8Lang.ModuleSystem",
            "using Old8Lang.TypeSystem",
            "using Old8Lang.ExternProviders",
            "using Old8Lang.Observability",
            "using Old8Lang.ProjectSystem",
            "using Old8Lang.Concurrency",
            "VariateManager",
            "ILGenerator",
            "LocalManager",
            "UnifiedModule"
        };

        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(syntaxRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var token in forbiddenTokens)
            {
                if (text.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(root, file)} 引用了 {token}");
                }
            }
        }

        Assert.Empty(violations);
    }

    private static string? FindRepositoryRoot()
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

        return null;
    }
}
