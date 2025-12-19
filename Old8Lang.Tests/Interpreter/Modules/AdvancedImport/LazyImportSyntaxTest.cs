using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.AdvancedImport;

/// <summary>
/// 懒导入语法测试
/// </summary>
public class LazyImportSyntaxTest(ITestOutputHelper output)
{
    [Fact]
    public void LazyImportSyntax_ShouldBeParsed()
    {
        // 测试基本的懒导入语法解析
        var testFilesDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "OldLib"
        );

        var tempDir = Path.Combine(testFilesDirectory, "temp");
        Directory.CreateDirectory(tempDir);

        // 创建测试模块
        var testModule = @"
test_value <- ""Hello from lazy module""
test_function() -> string {
    return ""Lazy function called""
}
";
        var modulePath = Path.Combine(tempDir, "test_lazy_module.old8");
        File.WriteAllText(modulePath, testModule);

        // 测试懒导入语法
        var testContent = @"
lazy import ""test_lazy_module""
";
        var testFilePath = Path.Combine(tempDir, "lazy_syntax_test.old8");
        File.WriteAllText(testFilePath, testContent);

        try
        {
            var interpreter = new LangInterpreter();
            interpreter.Manager.LangInfo!.ImportPath = tempDir;

            // 尝试解析懒导入语法
            var code = File.ReadAllText(testFilePath);

            output.WriteLine($"Testing code: {code}");

            // 如果解析没有抛出异常，说明语法被正确识别
            var ast = interpreter.Build(code, testFilePath);

            Assert.NotNull(ast);
            output.WriteLine("Lazy import syntax parsed successfully!");
        }
        catch (Exception ex)
        {
            output.WriteLine($"Parse exception: {ex.Message}");
            throw;
        }
        finally
        {
            try
            {
                File.Delete(modulePath);
                File.Delete(testFilePath);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }

    [Fact]
    public void LazyImportWithAlias_ShouldBeParsed()
    {
        // 测试带别名的懒导入语法
        var testFilesDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "OldLib"
        );

        var tempDir = Path.Combine(testFilesDirectory, "temp");
        Directory.CreateDirectory(tempDir);

        // 创建测试模块
        var testModule = @"
value <- 42
";
        var modulePath = Path.Combine(tempDir, "test_alias_module.old8");
        File.WriteAllText(modulePath, testModule);

        // 测试带别名的懒导入语法
        var testContent = @"
lazy import ""test_alias_module"" as tm
result <- tm.value
";
        var testFilePath = Path.Combine(tempDir, "lazy_alias_test.old8");
        File.WriteAllText(testFilePath, testContent);

        try
        {
            var interpreter = new LangInterpreter();
            interpreter.Manager.LangInfo!.ImportPath = tempDir;

            var code = File.ReadAllText(testFilePath);
            output.WriteLine($"Testing alias code: {code}");

            var ast = interpreter.Build(code, testFilePath);
            ast.Run(interpreter.Manager);

            var result = interpreter.Manager.GetValue(new LangId("result"));
            Assert.NotNull(result);
            Assert.IsType<IntLangValue>(result);
            Assert.Equal(42, ((IntLangValue)result).Value);

            output.WriteLine("Lazy import with alias executed successfully!");
        }
        catch (Exception ex)
        {
            output.WriteLine($"Execution exception: {ex.Message}");
            output.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            try
            {
                File.Delete(modulePath);
                File.Delete(testFilePath);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}