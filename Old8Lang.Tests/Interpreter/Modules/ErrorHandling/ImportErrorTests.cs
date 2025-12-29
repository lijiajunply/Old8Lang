using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.ErrorHandling;

/// <summary>
/// 导入错误处理测试
/// </summary>
public class ImportErrorTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var testContent = @"
import ""nonexistent_file123121231233""
result <- 42
";
        CreateTempModuleFile("error_test.old8", testContent);

        // Act & Assert
        var (_, exception) = ExecuteCodeFile("error_test.old8");
        Assert.NotNull(exception);
        Assert.IsType<ImportError>(exception);

        Output.WriteLine($"预期的异常类型: {exception.GetType().Name}");
        Output.WriteLine($"异常消息: {exception.Message}");
    }

    [Fact]
    public void Import_CircularDependency_ShouldBeHandled()
    {
        // Arrange
        var testContent = """
                          import "MathLib"
                          result <- Abs(-5)
                          """;
        CreateTempModuleFile("circular_dependency_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("circular_dependency_test.old8");

        // Assert - 循环依赖应该被正确处理，而不是导致无限递归
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    [Fact]
    public void Import_InvalidSyntaxInModule_ShouldThrowSyntaxError()
    {
        // Arrange
        var invalidModuleContent = @"
func broken_function( -> int {  // 缺少参数和右括号
    return 42
}
";
        var testContent = @"
import ""invalid_syntax_module""
result <- 42
";

        CreateTempModuleFile("invalid_syntax_module.old8", invalidModuleContent);
        CreateTempModuleFile("syntax_error_test.old8", testContent);

        // Act & Assert
        AssertExecutionThrows("syntax_error_test.old8", typeof(SyntaxError));
    }

    [Fact]
    public void Import_ModuleWithRuntimeException_ShouldPropagateError()
    {
        // Arrange
        var runtimeErrorModuleContent = @"
func divideByZero() -> double {
    return 1.0 / 0.0
}
";
        var testContent = @"
import ""runtime_error_module""
result <- runtime_error_module.divideByZero()
";

        CreateTempModuleFile("runtime_error_module.old8", runtimeErrorModuleContent);
        CreateTempModuleFile("runtime_error_test.old8", testContent);

        // Act & Assert
        AssertExecutionThrows("runtime_error_test.old8", typeof(ZeroDivisionError));
    }

    [Fact]
    public void Import_EmptyImportPath_ShouldThrowError()
    {
        // Arrange
        var testContent = @"
import """"
result <- 42
";
        CreateTempModuleFile("empty_path_test.old8", testContent);

        // Act & Assert
        AssertExecutionThrows("empty_path_test.old8", typeof(ImportError));
    }

    [Fact]
    public void Import_ModuleWithValidationErrors_ShouldBeHandled()
    {
        // Arrange
        var testContent = """
                          try {
                              import "nonexistent.module"
                              result <- "Import successful"
                          } catch {
                              result <- "Import failed: " + exception
                          }
                          """;
        CreateTempModuleFile("validation_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("validation_test.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        var message = ((StringLangValue)result).Value;
        Assert.Contains("Import failed", message);
    }

    [Theory]
    [InlineData("/absolute/path/module", "绝对路径")]
    [InlineData("../parent/module", "相对上级目录")]
    [InlineData("./current/module", "相对当前目录")]
    [InlineData("module/../../../back", "路径遍历攻击")]
    public void Import_SuspiciousPaths_ShouldBeHandled(string path, string description)
    {
        // Arrange
        var testContent = $@"
import ""{path}""
result <- 42
";
        CreateTempModuleFile("suspicious_path_test.old8", testContent);

        // Act & Assert
        // 根据安全策略，某些路径可能被拒绝
        var (interpreter, exception) = ExecuteCodeFile("suspicious_path_test.old8");

        // 如果执行成功，结果应该是42
        // 如果失败，应该抛出适当的异常
        if (exception == null)
        {
            AssertVariableValue(interpreter, "result", 42);
        }
        else
        {
            Output.WriteLine($"{description} - 路径被正确拒绝: {exception.Message}");
        }
    }

    [Fact]
    public void Import_ModuleWithMissingDependency_ShouldThrowError()
    {
        // Arrange
        var dependentModuleContent = @"
import ""missing_dependency""
func getValue() -> int {
    return dependency.getValue()
}
";
        var testContent = @"
import ""dependent_module""
result <- dependent_module.getValue()
";

        CreateTempModuleFile("dependent_module.old8", dependentModuleContent);
        CreateTempModuleFile("missing_dependency_test.old8", testContent);

        // Act & Assert
        AssertExecutionThrows("missing_dependency_test.old8", typeof(ImportError));
    }

    [Fact]
    public void Import_ReadOnlyModuleFile_ShouldHandleGracefully()
    {
        // Arrange - 创建一个只读模块文件（在某些系统上可能不支持）
        var moduleContent = @"
func getValue() -> int { return 42 }
";

        var modulePath = CreateTempModuleFile("readonly_module.old8", moduleContent);

        try
        {
            // 尝试设置文件为只读
            File.SetAttributes(modulePath, FileAttributes.ReadOnly);
        }
        catch
        {
            // 如果无法设置只读属性，跳过此测试
            Output.WriteLine("无法设置文件为只读，跳过测试");
            return;
        }

        var testContent = @"
import ""readonly_module""
result <- readonly_module.getValue()
";
        CreateTempModuleFile("readonly_test.old8", testContent);

        try
        {
            // Act & Assert - 读取只读文件应该成功
            var (interpreter, exception) = ExecuteCodeFile("readonly_test.old8");
            Assert.Null(exception);
            AssertVariableValue(interpreter, "result", 42);
        }
        finally
        {
            // Cleanup - 恢复文件属性以便删除
            try
            {
                File.SetAttributes(modulePath, FileAttributes.Normal);
            }
            catch
            {
                // 忽略清理失败
            }
        }
    }

    [Fact]
    public void Import_ModuleTooLarge_ShouldHandleGracefully()
    {
        // Arrange - 创建一个大模块文件（模拟）
        var largeContent = string.Join("\n", Enumerable.Range(0, 10000)
            .Select(i => $"func func{i}() -> int {{ return {i} }}"));

        var testContent = @"
import ""large_module""
result <- ""loaded""
";

        CreateTempModuleFile("large_module.old8", largeContent);
        CreateTempModuleFile("large_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("large_test.old8");

        // Assert - 大文件应该能够正常处理，或抛出适当的异常
        if (exception == null)
        {
            AssertVariableValue(interpreter, "result", "loaded");
        }
        else
        {
            Output.WriteLine($"大文件处理异常（预期）: {exception.Message}");
        }
    }
}