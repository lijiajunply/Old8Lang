using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Core;

/// <summary>
/// 模块导入测试的基础类，提供通用的测试功能
/// </summary>
public abstract class ModuleImportTestBase(ITestOutputHelper output) : IDisposable
{
    protected readonly ITestOutputHelper Output = output;

    private readonly string TestFilesDirectory = GetTestFilesDirectory();

    /// <summary>
    /// 获取测试文件目录的绝对路径
    /// </summary>
    private static string GetTestFilesDirectory()
    {
        // 获取当前程序集的位置
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

        // 从程序集目录向上查找，直到找到项目根目录的 OldLib（跳过 bin 目录）
        var currentDir = assemblyDirectory;
        while (currentDir != null)
        {
            // 跳过 bin 目录下的 OldLib
            if (!currentDir.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            {
                var oldLibPath = Path.Combine(currentDir, "OldLib");
                if (Directory.Exists(oldLibPath))
                {
                    return oldLibPath;
                }
            }
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        // 如果找不到，抛出异常
        throw new DirectoryNotFoundException($"无法找到 OldLib 目录。程序集位置: {assemblyLocation}");
    }

    /// <summary>
    /// 创建解释器实例
    /// </summary>
    private LangInterpreter CreateInterpreter()
    {
        var interpreter = new LangInterpreter();

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
            string? content = null;

            // 如果生成了内容，写入文件
            if (content != null)
            {
                fullPath = Path.Combine(TestFilesDirectory, relativeFilePath);
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(fullPath, content);
            }
            else
            {
                throw new FileNotFoundException($"测试文件不存在: {fullPath}\n实际的 TestFilesDirectory: {TestFilesDirectory}");
            }
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

        // 确保父目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

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
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}