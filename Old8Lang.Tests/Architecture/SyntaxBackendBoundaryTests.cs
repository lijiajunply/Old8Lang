using System.Reflection;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.Syntax;

namespace Old8Lang.Tests.Architecture;

/// <summary>
/// 语法节点与执行后端的源码/类型边界测试。
/// 阶段 1 的目标：语法节点（语句与表达式结构节点）不引用解释器、IL 或 VM 类型。
/// 运行时值（LangValueType 子类）的运行时行为仍暂存于 AST，留待后续阶段拆分。
/// </summary>
public class SyntaxBackendBoundaryTests
{
    private static readonly string[] BackendTypeNames =
    [
        nameof(VariateManager),
        "ILGenerator",
        nameof(LocalManager)
    ];

    /// <summary>
    /// 语句节点与表达式结构节点的源码不应出现解释器/IL/VM 类型。
    /// </summary>
    [Fact]
    public void SyntaxNodeSources_DoNotReferenceExecutionBackends()
    {
        var repoRoot = FindRepositoryRoot();
        if (repoRoot is null)
        {
            // 源码不可用时（例如仅部署二进制）跳过源码边界检查。
            return;
        }

        var astRoot = Path.Combine(repoRoot, "Old8Lang.Runtime", "AST");
        Assert.True(Directory.Exists(astRoot), $"找不到 AST 目录: {astRoot}");

        var syntaxDirectories = new[]
        {
            Path.Combine(astRoot, "Statement"),
            Path.Combine(astRoot, "Expression", "Core"),
            Path.Combine(astRoot, "Expression", "Advanced"),
            Path.Combine(astRoot, "Expression", "Generics"),
            Path.Combine(astRoot, "Expression", "Async"),
            Path.Combine(astRoot, "Expression", "Linq"),
            Path.Combine(astRoot, "Expression", "Generators")
        };

        var violations = new List<string>();
        foreach (var dir in syntaxDirectories.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                foreach (var typeName in BackendTypeNames)
                {
                    if (text.Contains(typeName, StringComparison.Ordinal))
                    {
                        violations.Add($"{Path.GetRelativePath(repoRoot, file)} 引用了 {typeName}");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    /// <summary>
    /// AST 节点类型不再暴露依赖执行后端的旧执行方法。
    /// </summary>
    [Fact]
    public void AstNodes_DoNotExposeLegacyExecutionMethods()
    {
        var assemblies = new[] { typeof(OldStatement).Assembly, typeof(LangValueType).Assembly }.Distinct();
        var legacyNames = new[] { "Run", "GenerateIl", "LoadIlValue", "OutputType", "SetValueToIl" };

        var violations = new List<string>();
        foreach (var type in assemblies.SelectMany(assembly => assembly.GetTypes()))
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            var isStatementNode = typeof(OldStatement).IsAssignableFrom(type);
            var isExpressionStructureNode = typeof(LangExpression).IsAssignableFrom(type)
                                           && !typeof(LangValueType).IsAssignableFrom(type);
            if (!isStatementNode && !isExpressionStructureNode)
            {
                continue;
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                   BindingFlags.Instance | BindingFlags.Static |
                                                   BindingFlags.DeclaredOnly))
            {
                if (!legacyNames.Contains(method.Name, StringComparer.Ordinal))
                {
                    continue;
                }

                var usesBackend = method.GetParameters().Any(p =>
                    BackendTypeNames.Contains(p.ParameterType.Name, StringComparer.Ordinal));
                if (usesBackend)
                {
                    violations.Add($"{type.Name}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})");
                }
            }
        }

        Assert.Empty(violations);
    }


    /// <summary>
    /// 标量字面量节点应实现 ILiteralSyntax，明确其「语法字面量」身份（阶段 1 的区分）。
    /// </summary>
    [Fact]
    public void ScalarLiteralNodes_AreMarkedAsLiteralSyntax()
    {
        Type[] literalTypes =
        [
            typeof(IntLangValue), typeof(DoubleLangValue), typeof(StringLangValue),
            typeof(BoolLangValue), typeof(CharLangValue), typeof(NullLangValue),
            typeof(VoidLangValue)
        ];

        Assert.All(literalTypes, t =>
            Assert.True(typeof(ILiteralSyntax).IsAssignableFrom(t), $"{t.Name} 应实现 ILiteralSyntax"));

        Assert.Equal(42, new IntLangValue(42).LiteralValue);
        Assert.Equal(LiteralKind.Int, new IntLangValue(42).Kind);
        Assert.Equal("hi", new StringLangValue("hi").LiteralValue);
        Assert.Null(new NullLangValue().LiteralValue);
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
