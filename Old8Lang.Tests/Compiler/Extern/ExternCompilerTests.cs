using System.Runtime.InteropServices;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Extern;

/// <summary>
/// Extern 语句编译模式测试
/// 测试 Native DLL extern 的 IL 生成和执行
/// 注意：Python 和 JavaScript extern 不支持编译模式
/// </summary>
[Collection("Sequential")]
public class ExternCompilerTests
{
    #region Native DLL 编译测试 - Windows

    /// <summary>
    /// 测试 Windows msvcrt.dll 的 abs 函数编译
    /// </summary>
    [Fact]
    public void CompileNativeDll_MsvcrtAbs_CompilesAndExecutesCorrectly()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var code = @"
extern ""msvcrt.dll"" func abs(x:int) -> int

func test() -> int {
    return abs(-42)
}

result <- test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 Windows kernel32.dll 编译
    /// </summary>
    [Fact]
    public void CompileNativeDll_Kernel32_CompilesAndExecutesCorrectly()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var code = @"
extern ""kernel32.dll"" stdcall {
    func GetCurrentThreadId() -> int,
    func GetCurrentProcessId() -> int
}

func getIds() -> int {
    tid <- GetCurrentThreadId()
    pid <- GetCurrentProcessId()
    return tid + pid
}

result <- getIds()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region Native DLL 编译测试 - macOS/Linux

    /// <summary>
    /// 测试 C 标准库 abs 函数编译（macOS/Linux）
    /// </summary>
    [Fact]
    public void CompileNativeDll_LibcAbs_CompilesAndExecutesCorrectly()
    {
        // 仅在 macOS 或 Linux 平台上运行
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var libName = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libSystem.dylib" : "libc.so.6";
        var code = $@"
extern ""{libName}"" func abs(x:int) -> int

func test() -> int {{
    return abs(-100)
}}

result <- test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 调用约定编译测试

    /// <summary>
    /// 测试不同调用约定的编译
    /// </summary>
    [Fact]
    public void CompileNativeDll_CallingConventions_CompilesCorrectly()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var code = @"
extern ""msvcrt.dll"" cdecl func abs(x:int) -> int
extern ""kernel32.dll"" stdcall func GetCurrentProcessId() -> int

func test() -> int {
    a <- abs(-50)
    p <- GetCurrentProcessId()
    return a
}

result <- test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 批量导入编译测试

    /// <summary>
    /// 测试批量函数导入的编译
    /// </summary>
    [Fact]
    public void CompileNativeDll_BatchImport_CompilesCorrectly()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var code = @"
extern ""kernel32.dll"" stdcall {
    func GetCurrentThreadId() -> int,
    func GetCurrentProcessId() -> int
}

func test() -> int {
    tid <- GetCurrentThreadId()
    pid <- GetCurrentProcessId()
    return tid
}

result <- test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region Python/JavaScript 编译模式测试（应该抛出异常）

    /// <summary>
    /// 测试 Python extern 在编译模式下抛出 NotSupportedException
    /// </summary>
    [Fact]
    public void CompilePython_Script_ThrowsNotSupportedException()
    {
        // Arrange
        var code = @"
extern ""test.py"" func test_func() -> int

func main() -> int {
    return test_func()
}

result <- main()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
        {
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        });

        Assert.Contains("不支持编译模式", exception.Message);
    }

    /// <summary>
    /// 测试 Python module extern 在编译模式下抛出 NotSupportedException
    /// </summary>
    [Fact]
    public void CompilePython_Module_ThrowsNotSupportedException()
    {
        // Arrange
        var code = @"
extern ""pymodule:math"" func sqrt(x:double) -> double

func main() -> double {
    return sqrt(16.0)
}

result <- main()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
        {
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        });

        Assert.Contains("不支持编译模式", exception.Message);
    }

    /// <summary>
    /// 测试 JavaScript extern 在编译模式下抛出 NotSupportedException
    /// </summary>
    [Fact]
    public void CompileJavaScript_Script_ThrowsNotSupportedException()
    {
        // Arrange
        var code = @"
extern ""test.js"" func add(a:int, b:int) -> int

func main() -> int {
    return add(1, 2)
}

result <- main()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
        {
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        });

        Assert.Contains("不支持编译模式", exception.Message);
    }

    /// <summary>
    /// 测试 js: 前缀在编译模式下抛出 NotSupportedException
    /// </summary>
    [Fact]
    public void CompileJavaScript_WithJsPrefix_ThrowsNotSupportedException()
    {
        // Arrange
        var code = @"
extern ""js:test.js"" func multiply(a:int, b:int) -> int

func main() -> int {
    return multiply(3, 4)
}

result <- main()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
        {
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        });

        Assert.Contains("不支持编译模式", exception.Message);
    }

    #endregion

    #region 混合场景编译测试

    /// <summary>
    /// 测试混合多个 DLL 的编译
    /// </summary>
    [Fact]
    public void CompileNativeDll_MultipleDlls_CompilesCorrectly()
    {
        // 仅在 Windows 平台上运行
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var code = @"
extern ""msvcrt.dll"" func abs(x:int) -> int
extern ""kernel32.dll"" stdcall func GetCurrentProcessId() -> int

func calculate() -> int {
    a <- abs(-200)
    p <- GetCurrentProcessId()
    return a
}

result <- calculate()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
