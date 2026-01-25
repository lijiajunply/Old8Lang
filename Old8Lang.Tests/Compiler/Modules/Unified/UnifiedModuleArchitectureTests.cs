using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.Unified;

/// <summary>
/// 统一模块架构测试
/// 验证新的统一模块接口架构在各种场景下的正确性
/// </summary>
[Collection("Sequential")]
public class UnifiedModuleArchitectureTests(ITestOutputHelper output)
{
    private readonly string TestFilesDirectory = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..",
        "..",
        "..",
        "OldLib",
        "temp",
        "Unified"
    );

    private LangInterpreter CreateInterpreter()
    {
        return new LangInterpreter();
    }

    private (LangInterpreter interpreter, Exception? exception) ExecuteCodeFile(string fileName, string content)
    {
        var filePath = CreateTempModuleFile(fileName, content);
        var interpreter = CreateInterpreter();
        Exception? exception = null;

        try
        {
            var ast = interpreter.Build(File.ReadAllText(filePath), filePath);
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

            // Assert - 验证编译成功
            Assert.NotNull(compiledAction);

            // 执行编译后的代码 - 验证执行不抛出异常
            var executionException = Record.Exception(() => compiledAction());
            Assert.Null(executionException);
        }
        catch (Exception ex)
        {
            exception = ex;
            output.WriteLine($"执行异常: {ex.Message}");
            if (ex.InnerException != null)
            {
                output.WriteLine($"内部异常: {ex.InnerException.Message}");
            }
        }

        return (interpreter, exception);
    }

    private string CreateTempModuleFile(string fileName, string content)
    {
        var tempDir = TestFilesDirectory;
        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    [Fact]
    public void UnifiedFactory_Enabled_ShouldCreateModuleObjectsWithNewInterface()
    {
        // Arrange
        var moduleContent = @"
func testFunction() -> string {
    return ""Module function called""
}

MODULE_CONSTANT <- ""Test constant""
";

        var testContent = @"
import ""test_module"" as tm
result <- tm.testFunction()
constant_value <- tm.MODULE_CONSTANT
";

        // Act
        CreateTempModuleFile("test_module.old8", moduleContent);
        var (interpreter, exception) = ExecuteCodeFile("main_test.old8", testContent);

        // Assert
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Module function called", ((StringLangValue)result).Value);

        var constantValue = interpreter.Manager.GetValue(new LangId("constant_value"));
        Assert.NotNull(constantValue);
        Assert.IsType<StringLangValue>(constantValue);
        Assert.Equal("Test constant", ((StringLangValue)constantValue).Value);

        // 验证模块对象实现了新接口
        var module = interpreter.Manager.GetValue(new LangId("tm"));
        Assert.NotNull(module);
        Assert.IsAssignableFrom<IModuleValueType>(module);

        var moduleValue = (IModuleValueType)module;
        Assert.Equal("test_module", moduleValue.ModuleName);
        Assert.True(moduleValue.IsLoaded);
        Assert.Equal(ModuleLoadingState.Loaded, moduleValue.LoadingState);
    }
}