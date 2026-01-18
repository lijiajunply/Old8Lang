using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Functions;

/// <summary>
/// 虚拟机异步函数测试
/// 测试 async/await 机制
/// </summary>
[Collection("Sequential")]
public class VMAsyncTests
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
            var vm = new Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void AsyncFunction_SimpleCall_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fetchData() -> string {
                return ""data""
            }

            result <- await fetchData()
            PrintLine(result)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("data", output);
    }

    [Fact]
    public void AsyncFunction_WithDelay_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func delayedFetch() -> int {
                Sleep(100)
                return 42
            }

            result <- await delayedFetch()
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("42", output);
    }

    [Fact]
    public void AsyncFunction_MultipleAwaits_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            async func fetch1() -> int {
                return 10
            }

            async func fetch2() -> int {
                return 20
            }

            result1 <- await fetch1()
            result2 <- await fetch2()
            total <- result1 + result2
            PrintLine(total.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("30", output);
    }
}
