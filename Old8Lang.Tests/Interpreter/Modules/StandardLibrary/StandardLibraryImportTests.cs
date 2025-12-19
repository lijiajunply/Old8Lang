using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.StandardLibrary;

/// <summary>
/// 标准库导入功能测试
/// 测试基于 LangInfo.json 中定义的标准库的导入功能
/// </summary>
public class StandardLibraryImportTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_OS_Library_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/os_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证 OS 库的基本功能
        var osInfo = interpreter.Manager.GetValue(new LangId("OS"));
        Assert.NotNull(osInfo);
        Assert.IsAssignableFrom<IModuleValueType>(osInfo);
    }

    [Fact]
    public void Import_MathLib_Library_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/mathlib_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证 MathLib 库的基本功能
        var mathLib = interpreter.Manager.GetValue(new LangId("MathLib"));
        Assert.NotNull(mathLib);
        Assert.IsAssignableFrom<IModuleValueType>(mathLib);
    }

    [Fact]
    public void Import_Time_Library_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/time_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证 Time 库的基本功能
        var timeLib = interpreter.Manager.GetValue(new LangId("Time"));
        Assert.NotNull(timeLib);
        Assert.IsAssignableFrom<IModuleValueType>(timeLib);
    }

    [Fact]
    public void Import_File_Library_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/file_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证 File 库的基本功能
        var fileLib = interpreter.Manager.GetValue(new LangId("File"));
        Assert.NotNull(fileLib);
        Assert.IsAssignableFrom<IModuleValueType>(fileLib);
    }

    [Fact]
    public void Import_Terminal_Library_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/terminal_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证 Terminal 库的基本功能
        var terminalLib = interpreter.Manager.GetValue(new LangId("Terminal"));
        Assert.NotNull(terminalLib);
        Assert.IsAssignableFrom<IModuleValueType>(terminalLib);
    }

    [Fact]
    public void Import_Net_Library_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/net_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证 Net 库的基本功能
        var netLib = interpreter.Manager.GetValue(new LangId("Net"));
        Assert.NotNull(netLib);
        Assert.IsAssignableFrom<IModuleValueType>(netLib);
    }

    [Fact]
    public void Import_Json_Library_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/json_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证 JSON 库的基本功能
        var jsonLib = interpreter.Manager.GetValue(new LangId("Json"));
        Assert.NotNull(jsonLib);
        Assert.IsAssignableFrom<IModuleValueType>(jsonLib);
    }

    [Fact]
    public void Import_CollectionLib_Library_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/collectionlib_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证 CollectionLib 库的基本功能
        var collectionLib = interpreter.Manager.GetValue(new LangId("CollectionLib"));
        Assert.NotNull(collectionLib);
        Assert.IsAssignableFrom<IModuleValueType>(collectionLib);
    }

    [Fact]
    public void Import_MultipleStandardLibraries_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/multiple_stdlib_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证多个标准库都被正确导入
        var osLib = interpreter.Manager.GetValue(new LangId("OS"));
        var mathLib = interpreter.Manager.GetValue(new LangId("MathLib"));
        var timeLib = interpreter.Manager.GetValue(new LangId("Time"));

        Assert.NotNull(osLib);
        Assert.NotNull(mathLib);
        Assert.NotNull(timeLib);

        Assert.IsAssignableFrom<IModuleValueType>(osLib);
        Assert.IsAssignableFrom<IModuleValueType>(mathLib);
        Assert.IsAssignableFrom<IModuleValueType>(timeLib);
    }

    [Fact]
    public void Import_StandardLibraryWithAlias_ShouldWorkCorrectly()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("StandardLibrary/stdlib_alias_import_test.old8");

        // Assert
        Assert.Null(exception);

        // 验证带别名的标准库导入
        var mathAlias = interpreter.Manager.GetValue(new LangId("math"));
        Assert.NotNull(mathAlias);
        Assert.IsAssignableFrom<IModuleValueType>(mathAlias);

        // 原始库名应该不存在
        var mathLib = interpreter.Manager.GetValue(new LangId("MathLib"));
        Assert.Null(mathLib);
    }

    [Fact]
    public void Import_NonExistentStandardLibrary_ShouldThrowException()
    {
        // Act & Assert
        var (_, exception) = ExecuteCodeFile("StandardLibrary/nonexistent_stdlib_test.old8");

        // 应该抛出导入异常
        Assert.NotNull(exception);
        Assert.IsType<Old8Lang.Error.ImportError>(exception);
    }
}