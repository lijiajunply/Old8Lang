using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Extern;

/// <summary>
/// 虚拟机 Extern 语句测试
/// 测试虚拟机执行 extern 函数调用的正确性
/// </summary>
[Collection("Sequential")]
public class VMExternTests
{
    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 捕获控制台输出
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // 执行字节码
            var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ExternCFunction_CallsCorrectly()
    {
        // Arrange - 使用跨平台的 C 标准库名称
        var libName = OperatingSystem.IsWindows() ? "msvcrt.dll" :
                      OperatingSystem.IsMacOS() ? "libSystem.dylib" :
                      "libc.so.6";

        var code = $@"
            extern ""{libName}"" {{
                func abs(x:int) -> int
            }}

            result <- abs(-42)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("42", output);
    }

    [Fact]
    public void ExternCFunction_WithAlias_CallsCorrectly()
    {
        // Arrange - 使用跨平台的 C 标准库名称
        var libName = OperatingSystem.IsWindows() ? "msvcrt.dll" :
                      OperatingSystem.IsMacOS() ? "libSystem.dylib" :
                      "libc.so.6";

        var code = $@"
            extern ""{libName}"" {{
                func abs(x:int) -> int as absolute
            }}

            result <- absolute(-100)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("100", output);
    }

    [Fact]
    public void ExternCFunction_GetPid_CallsCorrectly()
    {
        // Arrange - 测试获取进程ID（跨平台）
        var libName = OperatingSystem.IsWindows() ? "kernel32.dll" :
                      OperatingSystem.IsMacOS() ? "libSystem.dylib" :
                      "libc.so.6";

        var funcName = OperatingSystem.IsWindows() ? "GetCurrentProcessId" : "getpid";
        var convention = OperatingSystem.IsWindows() ? "stdcall" : "";

        var code = $@"
            extern ""{libName}"" {convention} {{
                func {funcName}() -> int
            }}

            pid <- {funcName}()
            PrintLine(""Process ID: "" + pid.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Process ID:", output);
        Assert.Matches(@"Process ID: \d+", output);
    }
}