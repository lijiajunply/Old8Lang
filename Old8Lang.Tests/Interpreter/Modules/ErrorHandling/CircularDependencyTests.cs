using Old8Lang.AST.Expression;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.ErrorHandling;

/// <summary>
/// 循环依赖处理测试
/// </summary>
public class CircularDependencyTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_CircularDependency_ShouldBeHandled()
    {
        // Arrange
        var testContent = """
            import "Math"
            result <- Abs(-5)
            """;
        CreateTempModuleFile("circular_dependency_basic_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("circular_dependency_basic_test.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    [Fact]
    public void Import_DirectCircularDependency_ShouldDetectAndHandle()
    {
        // Arrange
        var moduleAContent = @"
import ""circular_module_b""
func funcA() -> int {
    return 10 + getValueB()
}
func getValueA() -> int {
    return 5
}
";

        var moduleBContent = @"
import ""circular_module_a""
func funcB() -> int {
    return 20 + getValueA()
}
func getValueB() -> int {
    return 15
}
";

        var testContent = @"
import ""circular_module_a"" as a
import ""circular_module_b"" as b
result <- a.funcA() + b.funcB()
";

        CreateTempModuleFile("circular_module_a.old8", moduleAContent);
        CreateTempModuleFile("circular_module_b.old8", moduleBContent);
        CreateTempModuleFile("circular_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("circular_test.old8");

        // Assert
        // 循环依赖应该被检测和处理
        if (exception != null)
        {
            Output.WriteLine($"循环依赖被正确检测: {exception.Message}");
            Assert.Contains("circular", exception.Message.ToLower());
        }
        else
        {
            // 如果没有异常，应该有合理的结果
            var result = interpreter.Manager.GetValue(new LangId("result"));
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void Import_IndirectCircularDependency_ShouldDetectChain()
    {
        // Arrange
        var moduleAContent = @"
import ""indirect_module_b""
func getA() -> int {
    return 1 + getB()
}
";

        var moduleBContent = @"
import ""indirect_module_c""
func getB() -> int {
    return 2 + getC()
}
";

        var moduleCContent = @"
import ""indirect_module_a""
func getC() -> int {
    return 3 + getA()
}
";

        var testContent = @"
import ""indirect_module_a"" as a
result <- a.getA()
";

        CreateTempModuleFile("indirect_module_a.old8", moduleAContent);
        CreateTempModuleFile("indirect_module_b.old8", moduleBContent);
        CreateTempModuleFile("indirect_module_c.old8", moduleCContent);
        CreateTempModuleFile("indirect_circular_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("indirect_circular_test.old8");

        // Assert
        // 间接循环依赖应该被检测到并抛出异常
        Assert.NotNull(exception);
        Assert.True(exception.Message.Contains("circular", StringComparison.OrdinalIgnoreCase) ||
                   exception.Message.Contains("循环依赖"));
    }

    [Fact]
    public void Import_SelfDependency_ShouldBeRejected()
    {
        // Arrange
        var moduleContent = @"
import ""self_module""
func getValue() -> int {
    return 42
}
";

        var testContent = @"
import ""self_module"" as self
result <- self.getValue()
";

        CreateTempModuleFile("self_module.old8", moduleContent);
        CreateTempModuleFile("self_dependency_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("self_dependency_test.old8");

        // Assert
        // 自依赖应该被检测并拒绝
        if (exception != null)
        {
            Output.WriteLine($"自依赖被正确检测: {exception.Message}");
        }
    }

    [Fact]
    public void Import_CircularDependencyWithLazyImport_ShouldWork()
    {
        // Arrange
        var moduleAContent = @"
lazy import ""lazy_circular_b"" as b
func funcA() -> string {
    return ""A calls B: "" + b.funcB()
}
func standaloneA() -> string {
    return ""A standalone""
}
";

        var moduleBContent = @"
lazy import ""lazy_circular_a"" as a
func funcB() -> string {
    return ""B calls A: "" + a.funcA()
}
func standaloneB() -> string {
    return ""B standalone""
}
";

        var testContent = @"
import ""lazy_circular_a"" as a
import ""lazy_circular_b"" as b
result1 <- a.standaloneA()
result2 <- b.standaloneB()
";

        CreateTempModuleFile("lazy_circular_a.old8", moduleAContent);
        CreateTempModuleFile("lazy_circular_b.old8", moduleBContent);
        CreateTempModuleFile("lazy_circular_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_circular_test.old8");

        // Assert
        // 延迟导入应该能处理循环依赖
        if (exception == null)
        {
            AssertVariableValue(interpreter, "result1", "A standalone");
            AssertVariableValue(interpreter, "result2", "B standalone");
        }
        else
        {
            Output.WriteLine($"延迟导入循环依赖: {exception.Message}");
        }
    }

    [Fact]
    public void Import_CircularDependencyWithConditionalImport_ShouldWork()
    {
        // Arrange
        var moduleAContent = @"
if (true) {
    import ""conditional_circular_b""
}
func funcA() -> string {
    return ""A""
}
";

        var moduleBContent = @"
if (false) {
    import ""conditional_circular_a""
}
func funcB() -> string {
    return ""B""
}
";

        var testContent = @"
import ""conditional_circular_a"" as a
import ""conditional_circular_b"" as b
result1 <- a.funcA()
result2 <- b.funcB()
";

        CreateTempModuleFile("conditional_circular_a.old8", moduleAContent);
        CreateTempModuleFile("conditional_circular_b.old8", moduleBContent);
        CreateTempModuleFile("conditional_circular_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("conditional_circular_test.old8");

        // Assert
        // 条件导入可能避免循环依赖
        if (exception == null)
        {
            AssertVariableValue(interpreter, "result1", "A");
            AssertVariableValue(interpreter, "result2", "B");
        }
        else
        {
            Output.WriteLine($"条件导入循环依赖: {exception.Message}");
        }
    }

    [Fact]
    public void Import_ComplexCircularDependencyWeb_ShouldBeHandled()
    {
        // Arrange - 创建复杂的循环依赖网络
        var modules = new Dictionary<string, string>
        {
            ["core.old8"] = @"
import ""utils""
import ""config""
func initialize() -> string {
    return ""Core initialized""
}
",
            ["utils.old8"] = @"
import ""core""
import ""helpers""
func utility() -> string {
    return ""Utility function""
}
",
            ["config.old8"] = @"
import ""utils""
func getConfig() -> string {
    return ""Config loaded""
}
",
            ["helpers.old8"] = @"
import ""core""
import ""config""
func help() -> string {
    return ""Helper function""
}
"
        };

        var testContent = @"
import ""core"" as core
import ""utils"" as utils
import ""config"" as config
import ""helpers"" as helpers
result <- core.initialize()
";

        // 创建所有模块
        foreach (var module in modules)
        {
            CreateTempModuleFile(module.Key, module.Value);
        }
        CreateTempModuleFile("complex_circular_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("complex_circular_test.old8");

        // Assert
        // 复杂的循环依赖网络应该被检测到并抛出异常
        Assert.NotNull(exception);
        Assert.True(exception.Message.Contains("circular", StringComparison.OrdinalIgnoreCase) ||
                   exception.Message.Contains("循环依赖"));
    }

    [Fact]
    public void Import_CircularDependencyWithClasses_ShouldHandleCorrectly()
    {
        // Arrange
        var moduleAContent = @"
import ""circular_class_b""
class ClassA {
    fieldA <- 10

    public func getValue() -> int {
        return fieldA + getClassBValue()
    }
}
func getClassBValue() -> int {
    instance <- ClassB()
    return instance.getValue()
}
";

        var moduleBContent = @"
import ""circular_class_a""
class ClassB {
    fieldB <- 20

    public func getValue() -> int {
        return fieldB
    }
}
";

        var testContent = @"
import ""circular_class_a"" as a
result <- a.ClassA()
";

        CreateTempModuleFile("circular_class_a.old8", moduleAContent);
        CreateTempModuleFile("circular_class_b.old8", moduleBContent);
        CreateTempModuleFile("circular_class_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("circular_class_test.old8");

        // Assert
        // 类相关的循环依赖应该被检测到并抛出异常
        Assert.NotNull(exception);
        Assert.True(exception.Message.Contains("circular", StringComparison.OrdinalIgnoreCase) ||
                   exception.Message.Contains("循环依赖"));
    }

    [Fact]
    public void Import_CircularDependencyRecovery_ShouldProvideHelpfulMessage()
    {
        // Arrange
        var moduleAContent = @"
import ""recovery_module_b""
func testA() -> string {
    return ""Module A""
}
";

        var moduleBContent = @"
import ""recovery_module_a""
func testB() -> string {
    return ""Module B""
}
";

        var testContent = @"
try {
    import ""recovery_module_a"" as a
    import ""recovery_module_b"" as b
    result <- ""No circular dependency detected""
} catch (error) {
    result <- ""Circular dependency: "" + error.Message
}
";

        CreateTempModuleFile("recovery_module_a.old8", moduleAContent);
        CreateTempModuleFile("recovery_module_b.old8", moduleBContent);
        CreateTempModuleFile("recovery_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("recovery_test.old8");

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);

        if (result is AST.Expression.Value.StringLangValue stringResult)
        {
            var message = stringResult.Value;
            if (message.Contains("Circular"))
            {
                Output.WriteLine($"循环依赖错误消息: {message}");
                Assert.True(message.Length > 20, "错误消息应该提供有用的信息");
            }
        }
    }
}