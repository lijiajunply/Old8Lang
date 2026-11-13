using Old8Lang.Interpreter;
using Old8Lang.Hosting;
using Old8Lang.Bytecode;
using Old8Lang.Concurrency;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.Modules.Official;

namespace Old8Lang.Tests.Architecture;

/// <summary>
/// 验证核心与官方扩展模块之间的依赖边界。
/// </summary>
public class ModuleCompositionTests
{
    [Fact]
    public void SourceContracts_AreProvidedByCoreAssembly()
    {
        Assert.Equal("Old8Lang.Core", typeof(SourcePosition).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(LangInfo).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(Old8Lang.Error.CompilerException).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(Old8Lang.Error.SourceContextWindow).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(Old8Lang.Error.Old8Exception).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(Old8Lang.Error.SyntaxError).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(global::Old8Lang.LangParser.LangTokenType).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(global::Old8Lang.LangParser.PreprocessorSymbols).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(global::Old8Lang.LangParser.PreprocessorTokenizer).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(global::Old8Lang.LangParser.LangTokenizer).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(global::Old8Lang.LangParser.Core.TokenIndexCache).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(global::Old8Lang.LangParser.ParserHelpers.ExpectHelper).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(global::Old8Lang.LangParser.DocCommentParser).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(global::Old8Lang.AST.DocCommentInfo).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(global::Old8Lang.LangParser.Core.ParserContext).Assembly.GetName().Name);
        Assert.Equal("Old8Lang.Core", typeof(FileHeaderDirectives).Assembly.GetName().Name);
    }

    [Fact]
    public void OfficialModules_AreRegisteredByTheHostCompositionRoot()
    {
        var expectedNames = new[]
        {
            "OS", "File", "Terminal", "Time", "Math", "Crypto", "Csv", "Vector", "Regex",
            "ColorfulTerminal", "TemplateEngine", "Net", "MachineLearning", "Serialization", "Database"
        };

        foreach (var name in expectedNames)
        {
            Assert.True(StandardLibraryRegistry.IsStandardLibrary(name), $"模块 '{name}' 未由官方模块注册。");
        }
    }

    [Fact]
    public void CoreAssembly_DoesNotReferenceOfficialExtensionAssemblies()
    {
        Assert.Equal("Old8Lang.Runtime", typeof(LangInterpreter).Assembly.GetName().Name);

        var references = typeof(LangInterpreter).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        var extensions = new[]
        {
            "Old8LangLib", "Old8Lang.NetLib", "Old8Lang.DatabaseLib",
            "Old8Lang.MachineLearningLib", "Old8Lang.SerializationLib"
        };

        foreach (var extension in extensions)
        {
            Assert.DoesNotContain(extension, references);
        }
    }

    [Fact]
    public void ConcurrencyResources_AreProvidedByDedicatedAssembly()
    {
        Assert.Equal("Old8Lang.Concurrency", typeof(ResourceManager).Assembly.GetName().Name);
        Assert.NotEqual(typeof(LangInterpreter).Assembly, typeof(ResourceManager).Assembly);
    }

    [Fact]
    public void Hosts_KeepModuleCatalogsIsolatedUntilExplicitlyInstalled()
    {
        var firstBuilder = new Old8HostBuilder();
        OfficialModules.RegisterAll(firstBuilder);
        var first = firstBuilder.Build();
        var second = new Old8HostBuilder().Build();

        Assert.True(first.StandardLibraries.IsStandardLibrary("Database"));
        Assert.False(second.StandardLibraries.IsStandardLibrary("Database"));
    }

    [Fact]
    public void HostCreatedInterpreter_UsesItsOwnModuleCatalog()
    {
        var configuredBuilder = new Old8HostBuilder();
        OfficialModules.RegisterAll(configuredBuilder);
        var configuredInterpreter = configuredBuilder.Build().CreateInterpreter();
        var emptyInterpreter = new Old8HostBuilder().Build().CreateInterpreter();

        Assert.True(configuredInterpreter.Manager.StandardLibraries.IsStandardLibrary("Math"));
        Assert.False(emptyInterpreter.Manager.StandardLibraries.IsStandardLibrary("Math"));
    }

    [Fact]
    public void HostCreatedVmTools_UseItsOwnModuleCatalog()
    {
        var configuredBuilder = new Old8HostBuilder();
        OfficialModules.RegisterAll(configuredBuilder);
        var configuredHost = configuredBuilder.Build();
        var emptyHost = new Old8HostBuilder().Build();

        Assert.True(configuredHost.CreateBytecodeCompiler().StandardLibraries.IsStandardLibrary("Math"));
        Assert.False(emptyHost.CreateBytecodeCompiler().StandardLibraries.IsStandardLibrary("Math"));
        Assert.True(configuredHost.CreateVirtualMachine(new BytecodeFile()).StandardLibraries.IsStandardLibrary("Math"));
        Assert.False(emptyHost.CreateVirtualMachine(new BytecodeFile()).StandardLibraries.IsStandardLibrary("Math"));
    }
}
