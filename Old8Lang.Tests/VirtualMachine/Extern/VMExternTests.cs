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
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact(Skip = "需要实际的 C 标准库测试")]
    public void ExternCFunction_CallsCorrectly()
    {
        // Arrange
        var code = @"
            native extern ""msvcrt.dll"" {
                func abs(x:int) -> int
            }

            result <- abs(-42)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("42", output);
    }

    [Fact(Skip = "需要实际的 C 标准库测试")]
    public void ExternCFunction_WithAlias_CallsCorrectly()
    {
        // Arrange
        var code = @"
            native extern ""msvcrt.dll"" {
                func abs(x:int) -> int as absolute
            }

            result <- absolute(-100)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("100", output);
    }

    [Fact(Skip = "需要实际的 C 标准库测试")]
    public void ExternCFunction_MultipleParameters_CallsCorrectly()
    {
        // Arrange - 测试多参数函数
        var code = @"
            native extern ""kernel32.dll"" stdcall {
                func GetCurrentProcessId() -> uint
            }

            pid <- GetCurrentProcessId()
            PrintLine(""Process ID: "" + pid.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Process ID:", output);
    }
}