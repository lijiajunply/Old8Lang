using System.Runtime.InteropServices;
using Old8Lang.Error;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Extern;

/// <summary>
/// Native DLL Extern 功能测试（解释模式）
/// 测试 C/C++ P/Invoke 功能
/// </summary>
[Collection("Sequential")]
public class NativeDllExternTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    #region Windows 平台测试

    /// <summary>
    /// 测试 Windows msvcrt.dll 的 abs 函数
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_MsvcrtAbs_ReturnsCorrectResults()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Output.WriteLine("跳过测试：仅在 Windows 平台上运行");
            return;
        }

        // Arrange
        var old8Content = @"
extern ""msvcrt.dll"" {
    func abs(x:int) -> int
}

result1 <- abs(-42)
result2 <- abs(123)
result3 <- abs(0)
";

        CreateTempModuleFile("test_native_abs.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_native_abs.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 42);
        AssertVariableValue(interpreter, "result2", 123);
        AssertVariableValue(interpreter, "result3", 0);
    }

    /// <summary>
    /// 测试 Windows kernel32.dll 的线程和进程 ID 函数
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_Kernel32ThreadProcess_ReturnsIds()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Output.WriteLine("跳过测试：仅在 Windows 平台上运行");
            return;
        }

        // Arrange
        var old8Content = @"
extern ""kernel32.dll"" stdcall {
    func GetCurrentThreadId() -> int,
    func GetCurrentProcessId() -> int
}

threadId <- GetCurrentThreadId()
processId <- GetCurrentProcessId()
";

        CreateTempModuleFile("test_native_kernel32.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_native_kernel32.old8");

        // Assert
        Assert.Null(exception);
        // 验证返回的是有效的 ID（大于 0）
        var threadId = interpreter.Manager.GetValue(new AST.Expression.LangId("threadId"));
        var processId = interpreter.Manager.GetValue(new AST.Expression.LangId("processId"));
        Assert.NotNull(threadId);
        Assert.NotNull(processId);
        Assert.IsType<AST.Expression.Value.IntLangValue>(threadId);
        Assert.IsType<AST.Expression.Value.IntLangValue>(processId);
        Assert.True(((AST.Expression.Value.IntLangValue)threadId).Value > 0);
        Assert.True(((AST.Expression.Value.IntLangValue)processId).Value > 0);
    }

    #endregion

    #region macOS/Linux 平台测试

    /// <summary>
    /// 测试 C 标准库的 abs 函数（macOS/Linux）
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_LibcAbs_ReturnsCorrectResults()
    {
        // 仅在 macOS 或 Linux 平台上运行
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Output.WriteLine("跳过测试：仅在 macOS/Linux 平台上运行");
            return;
        }

        // Arrange
        var libName = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libSystem.dylib" : "libc.so.6";
        var old8Content = $@"
extern ""{libName}"" {{
    func abs(x:int) -> int
}}

result1 <- abs(-42)
result2 <- abs(123)
result3 <- abs(0)
";

        CreateTempModuleFile("test_native_libc_abs.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_native_libc_abs.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 42);
        AssertVariableValue(interpreter, "result2", 123);
        AssertVariableValue(interpreter, "result3", 0);
    }

    #endregion

    #region 调用约定测试

    /// <summary>
    /// 测试不同调用约定（Cdecl, StdCall, WinApi）
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_CallingConventions_WorksCorrectly()
    {
        // 仅在 Windows 平台上运行（调用约定在 Windows 上更明显）
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Output.WriteLine("跳过测试：仅在 Windows 平台上运行");
            return;
        }

        // Arrange
        var old8Content = @"
// 使用 cdecl 调用约定
extern ""msvcrt.dll"" cdecl func abs(x:int) -> int

// 使用 stdcall 调用约定
extern ""kernel32.dll"" stdcall func GetCurrentProcessId() -> int

result1 <- abs(-100)
result2 <- GetCurrentProcessId()
";

        CreateTempModuleFile("test_native_conventions.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_native_conventions.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 100);
        var processId = interpreter.Manager.GetValue(new AST.Expression.LangId("result2"));
        Assert.NotNull(processId);
        Assert.IsType<AST.Expression.Value.IntLangValue>(processId);
        Assert.True(((AST.Expression.Value.IntLangValue)processId).Value > 0);
    }

    #endregion

    #region 批量导入测试

    /// <summary>
    /// 测试批量函数导入（使用花括号）
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_BatchImport_WorksCorrectly()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Output.WriteLine("跳过测试：仅在 Windows 平台上运行");
            return;
        }

        // Arrange
        var old8Content = @"
extern ""kernel32.dll"" stdcall {
    func GetCurrentThreadId() -> int,
    func GetCurrentProcessId() -> int
}

threadId <- GetCurrentThreadId()
processId <- GetCurrentProcessId()
";

        CreateTempModuleFile("test_native_batch.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_native_batch.old8");

        // Assert
        Assert.Null(exception);
        var threadId = interpreter.Manager.GetValue(new AST.Expression.LangId("threadId"));
        var processId = interpreter.Manager.GetValue(new AST.Expression.LangId("processId"));
        Assert.NotNull(threadId);
        Assert.NotNull(processId);
    }

    #endregion

    #region 单函数导入测试

    /// <summary>
    /// 测试单函数导入（不使用花括号）
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_SingleFunctionImport_WorksCorrectly()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Output.WriteLine("跳过测试：仅在 Windows 平台上运行");
            return;
        }

        // Arrange
        var old8Content = @"
extern ""msvcrt.dll"" func abs(x:int) -> int

result <- abs(-999)
";

        CreateTempModuleFile("test_native_single.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_native_single.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 999);
    }

    #endregion

    #region 错误处理

    /// <summary>
    /// 测试 DLL 文件不存在时的错误处理
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_NonExistentDll_ThrowsException()
    {
        // Arrange
        var old8Content = @"
extern ""nonexistent_library.dll"" func test() -> void
";

        CreateTempModuleFile("test_native_error_dll.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_native_error_dll.old8", typeof(InvalidOperationError));
    }

    /// <summary>
    /// 测试函数在 DLL 中不存在时的错误处理
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_NonExistentFunction_ThrowsException()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Output.WriteLine("跳过测试：仅在 Windows 平台上运行");
            return;
        }

        // Arrange
        var old8Content = @"
extern ""msvcrt.dll"" func nonExistentFunction() -> int

result <- nonExistentFunction()
";

        CreateTempModuleFile("test_native_error_function.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_native_error_function.old8", typeof(InvalidOperationError));
    }

    #endregion

    #region 复杂场景

    /// <summary>
    /// 测试混合调用约定的批量导入
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_MixedConventions_WorksCorrectly()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Output.WriteLine("跳过测试：仅在 Windows 平台上运行");
            return;
        }

        // Arrange
        var old8Content = @"
extern ""msvcrt.dll"" cdecl {
    func abs(x:int) -> int
}

extern ""kernel32.dll"" stdcall {
    func GetCurrentThreadId() -> int
}

result1 <- abs(-50)
result2 <- GetCurrentThreadId()
";

        CreateTempModuleFile("test_native_mixed.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_native_mixed.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 50);
        var threadId = interpreter.Manager.GetValue(new AST.Expression.LangId("result2"));
        Assert.NotNull(threadId);
        Assert.IsType<AST.Expression.Value.IntLangValue>(threadId);
    }

    /// <summary>
    /// 测试多个 DLL 文件同时导入
    /// </summary>
    [Fact]
    public void ExecuteNativeDll_MultipleDlls_WorksCorrectly()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Output.WriteLine("跳过测试：仅在 Windows 平台上运行");
            return;
        }

        // Arrange
        var old8Content = @"
extern ""msvcrt.dll"" func abs(x:int) -> int
extern ""kernel32.dll"" stdcall func GetCurrentProcessId() -> int

result1 <- abs(-200)
result2 <- GetCurrentProcessId()
";

        CreateTempModuleFile("test_native_multiple_dlls.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_native_multiple_dlls.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 200);
        var processId = interpreter.Manager.GetValue(new AST.Expression.LangId("result2"));
        Assert.NotNull(processId);
        Assert.IsType<AST.Expression.Value.IntLangValue>(processId);
        Assert.True(((AST.Expression.Value.IntLangValue)processId).Value > 0);
    }

    #endregion
}
