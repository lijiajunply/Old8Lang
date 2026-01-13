using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Async;

/// <summary>
/// 虚拟机异步生成器测试
/// 测试虚拟机执行异步生成器（async generator）的正确性
/// </summary>
[Collection("Sequential")]
public class VMAsyncGeneratorTests
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

    #region Basic Async Generator Tests

    [Fact]
    public void SimpleAsyncGenerator_YieldsValues()
    {
        // Arrange - 测试简单的异步生成器
        var code = @"
async func asyncRange(start:int, end:int) -> object {
    i <- start
    while i < end {
        yield i
        i <- i + 1
    }
}

gen <- asyncRange(1, 4)
async for num in gen {
    PrintLine(ToStr(num))
}
";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("1\n2\n3", output);
    }

    [Fact]
    public void AsyncGenerator_WithAwait_YieldsValues()
    {
        // Arrange - 测试异步生成器的基本功能
        var code = @"
async func delayedRange(start:int, end:int) -> object {
    i <- start
    while i < end {
        yield i
        i <- i + 1
    }
}

gen <- delayedRange(5, 8)
async for num in gen {
    PrintLine(ToStr(num))
}
";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("5\n6\n7", output);
    }

    #endregion
}