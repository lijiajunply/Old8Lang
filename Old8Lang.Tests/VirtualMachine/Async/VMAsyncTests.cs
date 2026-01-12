using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Async;

/// <summary>
/// 虚拟机异步支持测试
/// 测试虚拟机执行Await、Yield、NewTask等异步操作的正确性
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
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region Basic Async Tests

    [Fact]
    public void SimpleFunction_ExecutesCorrectly()
    {
        // Arrange - 测试基本函数调用
        var code = @"
            func add(a:int, b:int) -> int {
                return a + b
            }

            result <- add(10, 20)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("30", output);
    }

    [Fact]
    public void SimpleAsyncFunction_ExecutesCorrectly()
    {
        // Arrange - 测试简单异步函数调用和 await
        var code = @"
            async func hello() -> string {
                return ""Hello from async""
            }

            task <- hello()
            result <- await task
            PrintLine(result)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Hello from async", output);
    }

    [Fact]
    public void AsyncFunctionWithParameters_ExecutesCorrectly()
    {
        // Arrange - 测试带参数的异步函数
        var code = @"
            async func greet(name:string) -> string {
                return ""Hello, "" + name
            }

            task <- greet(""World"")
            result <- await task
            PrintLine(result)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Hello, World", output);
    }

    [Fact]
    public void AsyncFunctionWithMultipleParameters_ExecutesCorrectly()
    {
        // Arrange - 测试多参数异步函数
        var code = @"
            async func add(a:int, b:int) -> int {
                return a + b
            }

            task <- add(15, 25)
            result <- await task
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("40", output);
    }

    #endregion

    #region Concurrent Async Tests

    [Fact]
    public void MultipleAsyncFunctions_ExecuteConcurrently()
    {
        // Arrange - 测试多个异步函数并发执行
        var code = @"
            async func fetchData(id:int) -> string {
                return ""Data-"" + id.ToStr()
            }

            task1 <- fetchData(1)
            task2 <- fetchData(2)
            task3 <- fetchData(3)

            result1 <- await task1
            result2 <- await task2
            result3 <- await task3

            PrintLine(result1)
            PrintLine(result2)
            PrintLine(result3)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Data-1", lines[0]);
        Assert.Equal("Data-2", lines[1]);
        Assert.Equal("Data-3", lines[2]);
    }

    #endregion
}
