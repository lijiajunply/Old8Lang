using Old8Lang.AST.Expression;
using Old8Lang.Bytecode;
using Old8Lang.Error;
using Old8Lang.Hosting;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.Modules.Abstractions;
using Old8LangLib;

namespace Old8Lang.Tests.Architecture;

/// <summary>
/// 验证宿主模块目录的显式注册与隔离：宿主创建的执行实例只看见自己的目录，
/// 进程默认目录只服务于兼容构造器。
/// </summary>
public class HostModuleIsolationTests
{
    private const string HostModuleName = "HostOnlyMath";
    private const string TestModuleCode = "import HostOnlyMath\nAssert.Equal(5.0, HostOnlyMath.Abs(-5))";

    private static Old8HostBuilder CreateHostWithTestModule()
    {
        var builder = new Old8HostBuilder();
        builder.Register(new StandardLibraryModuleDescriptor(
            HostModuleName,
            "Old8LangLib",
            [StandardLibraryTypeExport.StaticClass(typeof(MathLib))]));
        return builder;
    }

    private static Old8HostBuilder CreateEmptyHost() => new();

    [Fact]
    public void InterpreterImport_UsesOnlyItsHostCatalog()
    {
        var configured = CreateHostWithTestModule().Build().CreateInterpreter();
        configured.Build(TestModuleCode).Run(configured.Manager);

        var empty = CreateEmptyHost().Build().CreateInterpreter();
        var ast = empty.Build(TestModuleCode);
        Assert.Throws<ImportError>(() => ast.Run(empty.Manager));
    }

    [Fact]
    public void CompilePathImport_UsesOnlyItsHostCatalog()
    {
        var configured = CreateHostWithTestModule().Build().CreateInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(
            configured.Build(TestModuleCode), "host-isolation-il", configured);
        action();

        var empty = CreateEmptyHost().Build().CreateInterpreter();
        var ast = empty.Build(TestModuleCode);
        Assert.Throws<ImportError>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "host-isolation-il", empty));
    }

    [Fact]
    public void VirtualMachineImport_UsesOnlyItsHostCatalog()
    {
        var configured = CreateHostWithTestModule().Build();
        var configuredInterpreter = configured.CreateInterpreter();
        var bytecode = configured.CreateBytecodeCompiler().Compile(configuredInterpreter.Build(TestModuleCode));
        configured.CreateVirtualMachine(bytecode).Execute();

        var empty = CreateEmptyHost().Build();
        var emptyInterpreter = empty.CreateInterpreter();
        var emptyBytecode = empty.CreateBytecodeCompiler().Compile(emptyInterpreter.Build(TestModuleCode));
        var exception = Assert.Throws<ImportError>(() => empty.CreateVirtualMachine(emptyBytecode).Execute());
        Assert.Contains("加载模块", exception.Message);
        Assert.Contains(HostModuleName, exception.Message);
    }

    [Fact]
    public void CustomModuleRegistration_DoesNotLeakToOtherHostsOrProcessDefault()
    {
        var configured = CreateHostWithTestModule().Build();
        var other = CreateEmptyHost().Build();

        Assert.True(configured.StandardLibraries.IsStandardLibrary(HostModuleName));
        Assert.False(other.StandardLibraries.IsStandardLibrary(HostModuleName));
        Assert.False(StandardLibraryRegistry.IsStandardLibrary(HostModuleName));
    }

    [Fact]
    public void HostCreatedInstances_AreNotBoundToProcessDefaultCatalog()
    {
        var host = CreateEmptyHost().Build();

        Assert.NotSame(StandardLibraryRegistry.DefaultCatalog, host.CreateInterpreter().Manager.StandardLibraries);
        Assert.NotSame(StandardLibraryRegistry.DefaultCatalog, host.CreateBytecodeCompiler().StandardLibraries);
        Assert.NotSame(
            StandardLibraryRegistry.DefaultCatalog,
            host.CreateVirtualMachine(new BytecodeFile()).StandardLibraries);
    }

    [Fact]
    public void LegacyConstructors_UseProcessDefaultCatalog()
    {
        // 先快照，避免其他测试同时替换进程默认目录造成偶发差异。
        var expected = StandardLibraryRegistry.DefaultCatalog;

        Assert.Same(expected, new LangInterpreter().Manager.StandardLibraries);
        Assert.Same(expected, new BytecodeCompiler().StandardLibraries);
        Assert.Same(expected, new Old8Lang.Bytecode.VM.VirtualMachine(new BytecodeFile()).StandardLibraries);
    }

    [Fact]
    public void AdHocManagers_UseSharedEmptyCatalog()
    {
        Assert.Same(StandardLibraryCatalog.Empty, new VariateManager().StandardLibraries);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            StandardLibraryCatalog.Empty.Register(new StandardLibraryModuleDescriptor(
                "ShouldNotRegister",
                "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(MathLib))])));
        Assert.Contains("空标准库目录", exception.Message);
    }

    [Fact]
    public void ScopesAndCaptures_PreserveHostCatalog()
    {
        var catalog = new StandardLibraryCatalog();
        catalog.Register(new StandardLibraryModuleDescriptor(
            HostModuleName,
            "Old8LangLib",
            [StandardLibraryTypeExport.StaticClass(typeof(MathLib))]));
        var manager = new VariateManager { StandardLibraries = catalog };

        Assert.Same(catalog, manager.Clone().StandardLibraries);
        Assert.Same(catalog, manager.NewManger().StandardLibraries);
        Assert.Same(catalog, manager.CloneForGenerator().StandardLibraries);
        Assert.Same(catalog, CapturedScope.FromManager(manager).ToManager(null).StandardLibraries);
    }

    [Fact]
    public void TwoNonEmptyHosts_DoNotSeeEachOthersModules()
    {
        const string otherModuleName = "HostOnlyMathOther";

        var first = CreateHostWithTestModule().Build().CreateInterpreter();
        first.Build(TestModuleCode).Run(first.Manager);

        var secondBuilder = new Old8HostBuilder();
        secondBuilder.Register(new StandardLibraryModuleDescriptor(
            otherModuleName,
            "Old8LangLib",
            [StandardLibraryTypeExport.StaticClass(typeof(MathLib))]));
        var second = secondBuilder.Build().CreateInterpreter();
        second.Build($"import {otherModuleName}\nAssert.Equal(5.0, {otherModuleName}.Abs(-5))").Run(second.Manager);

        var firstUsingOther = first.Build($"import {otherModuleName}\nPrintLine(1)");
        Assert.Throws<ImportError>(() => firstUsingOther.Run(first.Manager));

        var secondUsingFirst = second.Build(TestModuleCode);
        Assert.Throws<ImportError>(() => secondUsingFirst.Run(second.Manager));
    }

    [Fact]
    public void ClosureAndGeneratorBodies_SeeHostCatalog()
    {
        const string code = """
            func closureImport() -> double {
                import HostOnlyMath
                return HostOnlyMath.Abs(-5)
            }
            func generatorImport() {
                import HostOnlyMath
                yield HostOnlyMath.Abs(-3)
                yield HostOnlyMath.Abs(-4)
            }
            Assert.Equal(5.0, closureImport())
            sum <- 0.0
            for value in generatorImport() {
                sum <- sum + value
            }
            Assert.Equal(7.0, sum)
            """;

        var interpreter = CreateHostWithTestModule().Build().CreateInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);
    }

    [Fact]
    public void CatalogDependentDefaults_DoNotReadProcessDefault()
    {
        Assert.False(StandardLibraryLoader.ValidateStandardLibrary("Math", out var error));
        Assert.Contains("不是已注册的标准库", error);

        var resolution = new ModuleResolver().ResolveModule("Math", null);
        Assert.False(resolution.IsSuccess);
    }
}
