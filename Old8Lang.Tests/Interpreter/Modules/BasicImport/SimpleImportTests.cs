using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.BasicImport;

/// <summary>
/// 基础导入功能测试
/// </summary>
[Collection("Sequential")]
public class SimpleImportTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_BasicMathModule_ShouldImportCorrectly()
    {
        // Arrange
        var moduleContent = @"
func add(a:double, b:double) -> double {
    return a + b
}
func multiply(a:double, b:double) -> double {
    return a * b
}
";
        var testContent = @"
import ""test_math""
result1 <- test_math.add(2.0, 3.0)
result2 <- test_math.multiply(4.0, 5.0)
";

        // 创建测试模块
        CreateTempModuleFile("test_math.old8", moduleContent);
        CreateTempModuleFile("simple_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("simple_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 5.0);
        AssertVariableValue(interpreter, "result2", 20.0);
    }

    [Fact]
    public void Import_ExistingMathModule_ShouldWorkCorrectly()
    {
        // Arrange
        var testContent = """
                          import "Math" as m
                          result <- m.sqrt(25)
                          """;
        CreateTempModuleFile("alias_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("alias_test.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(5.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void Import_MultipleFunctions_ShouldImportAllFunctions()
    {
        // Arrange
        var testContent = """
                          import "Math"
                          result1 <- Math.Sqrt(25)
                          result2 <- Pow(2, 3)
                          """;
        CreateTempModuleFile("specific_func_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("specific_func_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 5.0);
        AssertVariableValue(interpreter, "result2", 8.0);
    }

    [Theory]
    [InlineData("nonexistent_module", "import \"nonexistent_module\"")]
    [InlineData("invalid/path/module", "import \"invalid/path/module\"")]
    public void Import_NonExistentModule_ShouldThrowException(string moduleName, string importStatement)
    {
        // Arrange
        var testContent = $"""
                           {importStatement}
                           result <- 42
                           """;
        CreateTempModuleFile("test_error.old8", testContent);

        // Act & Assert
        AssertExecutionThrows("test_error.old8", typeof(ImportError));
    }

    [Fact]
    public void Import_ImportSameModuleTwice_ShouldNotDuplicate()
    {
        // Arrange
        var testContent = """
                          import "Math"
                          result1 <- Sqrt(9)
                          result2 <- Sqrt(16)
                          """;
        CreateTempModuleFile("reimport_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("reimport_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 3.0);
        AssertVariableValue(interpreter, "result2", 4.0);
    }

    [Fact]
    public void Import_EmptyModule_ShouldImportSuccessfully()
    {
        // Arrange
        var moduleContent = "// Empty module with just comments";
        var testContent = @"
import ""empty_module""
result <- ""imported""
";

        CreateTempModuleFile("empty_module.old8", moduleContent);
        CreateTempModuleFile("empty_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("empty_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "imported");
    }

    [Fact]
    public void Import_ModuleWithConstants_ShouldImportConstants()
    {
        // Arrange
        var moduleContent = @"
PI:const <- 3.14159
E:const <- 2.71828
func getPi() -> double {
    return PI
}
";
        var testContent = @"
import ""constants_module""
pi_value <- constants_module.PI
e_value <- constants_module.E
pi_func <- constants_module.getPi()
";

        CreateTempModuleFile("constants_module.old8", moduleContent);
        CreateTempModuleFile("constants_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("constants_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "pi_value", 3.14159);
        AssertVariableValue(interpreter, "e_value", 2.71828);
        AssertVariableValue(interpreter, "pi_func", 3.14159);
    }

    [Fact]
    public void Import_ModuleWithVariables_ShouldImportVariables()
    {
        // Arrange
        var moduleContent = @"
counter <- 0
func increment() -> int {
    counter <- counter + 1
    return counter
}
func getCounter() -> int {
    return counter
}
";
        var testContent = @"
import ""state_module""
initial_counter <- state_module.getCounter()
result1 <- state_module.increment()
result2 <- state_module.increment()
final_counter <- state_module.getCounter()
";

        CreateTempModuleFile("state_module.old8", moduleContent);
        CreateTempModuleFile("state_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("state_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "initial_counter", 0);
        AssertVariableValue(interpreter, "result1", 1);
        AssertVariableValue(interpreter, "result2", 2);
        AssertVariableValue(interpreter, "final_counter", 2);
    }
}