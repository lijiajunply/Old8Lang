using System.Reflection.Emit;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Architecture;

/// <summary>
/// 阶段 3 的后端程序集边界门禁：每个执行后端独立成程序集，只依赖共享 Runtime；
/// Runtime 不得反向引用任何后端程序集。
/// </summary>
public class BackendAssemblyBoundaryTests
{
    [Fact]
    public void RuntimeAssembly_DoesNotReferenceInterpreterBackend()
    {
        var referenced = typeof(LangInterpreter).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name)
            .ToArray();

        Assert.DoesNotContain("Old8Lang.Interpreter", referenced);
        Assert.DoesNotContain("Old8Lang.Compiler", referenced);
        Assert.DoesNotContain("Old8Lang.VirtualMachine", referenced);
    }

    [Fact]
    public void InterpreterAssembly_ReferencesRuntimeAndNoOtherBackend()
    {
        var referenced = typeof(InterpreterBackend).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name)
            .ToArray();

        Assert.Contains("Old8Lang.Runtime", referenced);
        Assert.DoesNotContain("Old8Lang.Compiler", referenced);
        Assert.DoesNotContain("Old8Lang.VirtualMachine", referenced);
        Assert.DoesNotContain("Old8Lang.Modules.Official", referenced);
        Assert.DoesNotContain("Old8Lang.Hosting", referenced);
    }

    [Fact]
    public void InterpreterBackend_IsRegisteredThroughRuntimeContract()
    {
        InterpreterBackend.Register();

        Assert.True(RuntimeVisitors.HasInterpreter);
        var manager = new VariateManager();
        Assert.IsType<InterpreterVisitor>(RuntimeVisitors.CreateInterpreter(manager));
    }

    [Fact]
    public void RuntimeSource_DoesNotConstructInterpreterVisitorDirectly()
    {
        var root = RequireRepositoryRoot();
        var runtimeRoot = Path.Combine(root, "Old8Lang.Runtime");
        var offenders = Directory
            .EnumerateFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(file => File.ReadAllText(file).Contains("InterpreterVisitor", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void CompilerAssembly_ReferencesRuntimeAndNoOtherBackend()
    {
        var referenced = typeof(CompilerBackend).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name)
            .ToArray();

        Assert.Contains("Old8Lang.Runtime", referenced);
        Assert.DoesNotContain("Old8Lang.Interpreter", referenced);
        Assert.DoesNotContain("Old8Lang.VirtualMachine", referenced);
        Assert.DoesNotContain("Old8Lang.Modules.Official", referenced);
        Assert.DoesNotContain("Old8Lang.Hosting", referenced);
    }

    [Fact]
    public void CompilerBackend_IsRegisteredThroughRuntimeContract()
    {
        CompilerBackend.Register();

        Assert.True(RuntimeVisitors.HasCompiler);
        var dynamicMethod = new DynamicMethod("compiler_boundary_test", typeof(void), Type.EmptyTypes);
        var local = new LocalManager();
        Assert.IsType<CompilerVisitor>(RuntimeVisitors.CreateCompiler(dynamicMethod.GetILGenerator(), local));
    }

    [Fact]
    public void RuntimeSource_DoesNotConstructCompilerVisitorDirectly()
    {
        var root = RequireRepositoryRoot();
        var runtimeRoot = Path.Combine(root, "Old8Lang.Runtime");
        var offenders = Directory
            .EnumerateFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(file => File.ReadAllText(file).Contains("new CompilerVisitor(", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(offenders);
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
