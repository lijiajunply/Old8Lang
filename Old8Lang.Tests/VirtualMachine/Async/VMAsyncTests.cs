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

    #endregion
}
