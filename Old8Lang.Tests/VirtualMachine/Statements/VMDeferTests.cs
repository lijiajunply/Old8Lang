using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机 defer 语句测试
/// 测试 defer 语句的资源清理机制
/// </summary>
[Collection("Sequential")]
public class VMDeferTests
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
    public void SimpleDeferStatement_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                PrintLine(""start"")
                defer PrintLine(""cleanup"")
                PrintLine(""end"")
            }
            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("start", lines[0]);
        Assert.Equal("end", lines[1]);
        Assert.Equal("cleanup", lines[2]); // defer 在函数退出时执行
    }

    [Fact]
    public void MultipleDeferStatements_ExecuteInReverseOrder()
    {
        // Arrange
        var code = @"
            func test() -> void {
                defer PrintLine(""first"")
                defer PrintLine(""second"")
                defer PrintLine(""third"")
                PrintLine(""main"")
            }
            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("main", lines[0]);
        Assert.Equal("third", lines[1]);  // LIFO 顺序
        Assert.Equal("second", lines[2]);
        Assert.Equal("first", lines[3]);
    }

    [Fact]
    public void DeferWithException_StillExecutes()
    {
        // Arrange
        var code = @"
            func test() -> void {
                defer PrintLine(""cleanup"")
                PrintLine(""before error"")
                throw ""error""
                PrintLine(""after error"")
            }

            try {
                test()
            } catch (e) {
                PrintLine(""caught"")
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("before error", lines);
        Assert.Contains("cleanup", lines); // defer 即使在异常情况下也会执行
        Assert.Contains("caught", lines);
        Assert.DoesNotContain("after error", lines);
    }
}
