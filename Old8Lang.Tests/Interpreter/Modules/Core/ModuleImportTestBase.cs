using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Core;

/// <summary>
/// 模块导入测试的基础类，提供通用的测试功能
/// </summary>
public abstract class ModuleImportTestBase(ITestOutputHelper output)
{
    protected readonly ITestOutputHelper Output = output;

    private readonly string TestFilesDirectory = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..",
        "..",
        "..",
        "OldLib"
    );

    /// <summary>
    /// 创建解释器实例
    /// </summary>
    private LangInterpreter CreateInterpreter()
    {
        var interpreter = new LangInterpreter();

        // 配置导入路径为测试文件目录
        var tempDir = Path.Combine(TestFilesDirectory, "temp");
        if (!interpreter.Manager.LangInfo!.ImportPath.Equals(tempDir, StringComparison.OrdinalIgnoreCase))
        {
            // 确保temp目录存在
            Directory.CreateDirectory(tempDir);

            // 设置导入路径
            interpreter.Manager.LangInfo.ImportPath = tempDir;
        }

        return interpreter;
    }

    /// <summary>
    /// 执行 Old8Lang 代码文件
    /// </summary>
    protected (LangInterpreter interpreter, Exception? exception) ExecuteCodeFile(string relativeFilePath)
    {
        // 首先尝试在主目录中查找文件
        var fullPath = Path.Combine(TestFilesDirectory, relativeFilePath);

        // 如果主目录中不存在，尝试在 temp 子目录中查找
        if (!File.Exists(fullPath))
        {
            var tempPath = Path.Combine(TestFilesDirectory, "temp", relativeFilePath);
            if (File.Exists(tempPath))
            {
                fullPath = tempPath;
            }
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"测试文件不存在: {fullPath}");
        }

        var code = File.ReadAllText(fullPath);
        var interpreter = CreateInterpreter();
        Exception? exception = null;

        try
        {
            var ast = interpreter.Build(code, fullPath);
            ast.Run(interpreter.Manager);
        }
        catch (Exception ex)
        {
            exception = ex;
            Output.WriteLine($"执行异常: {ex.Message}");
            Output.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }

        return (interpreter, exception);
    }

    /// <summary>
    /// 验证变量值
    /// </summary>
    protected void AssertVariableValue<T>(LangInterpreter interpreter, string variableName, T expectedValue)
        where T : notnull
    {
        var value = interpreter.Manager.GetValue(new LangId(variableName));
        Assert.NotNull(value);

        if (typeof(T) == typeof(int))
        {
            Assert.IsType<IntLangValue>(value);
            Assert.Equal((int)(object)expectedValue, ((IntLangValue)value).Value);
        }
        else if (typeof(T) == typeof(double))
        {
            Assert.IsType<DoubleLangValue>(value);
            Assert.Equal((double)(object)expectedValue, ((DoubleLangValue)value).Value, 0.0001);
        }
        else if (typeof(T) == typeof(string))
        {
            Assert.IsType<StringLangValue>(value);
            Assert.Equal((string)(object)expectedValue, ((StringLangValue)value).Value);
        }
        else if (typeof(T) == typeof(bool))
        {
            Assert.IsType<BoolLangValue>(value);
            Assert.Equal((bool)(object)expectedValue, ((BoolLangValue)value).Value);
        }
        else
        {
            Assert.Fail($"不支持的类型: {typeof(T)}");
        }
    }

    /// <summary>
    /// 验证执行应该抛出异常
    /// </summary>
    protected void AssertExecutionThrows(string relativeFilePath, Type expectedExceptionType)
    {
        var (_, exception) = ExecuteCodeFile(relativeFilePath);
        Assert.NotNull(exception);
        Assert.IsType(expectedExceptionType, exception);
    }

    /// <summary>
    /// 创建临时的测试模块文件
    /// </summary>
    protected string CreateTempModuleFile(string fileName, string content)
    {
        var tempDir = Path.Combine(TestFilesDirectory, "temp");
        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>
    /// 清理临时测试文件
    /// </summary>
    protected void CleanupTempFiles()
    {
        var tempDir = Path.Combine(TestFilesDirectory, "temp");
        if (Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Output.WriteLine($"清理临时文件失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 测试完成后清理
    /// </summary>
    public virtual void Dispose()
    {
        CleanupTempFiles();
    }
}