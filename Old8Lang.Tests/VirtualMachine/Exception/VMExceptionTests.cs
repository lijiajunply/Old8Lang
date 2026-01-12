using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Exception;

/// <summary>
/// 虚拟机异常处理测试
/// 测试虚拟机执行Try-Catch-Finally等异常处理的正确性
/// </summary>
[Collection("Sequential")]
public class VMExceptionTests
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

    #region Basic Exception Tests

    [Fact]
    public void SimpleThrow_ThrowsException()
    {
        // Arrange
        var code = @"
            func throwError() -> void {
                throw ""Test error""
            }

            throwError()
        ";

        // Act & Assert
        var exception = Assert.Throws<System.Exception>(() => ExecuteVMCode(code));
        Assert.Equal("Test error", exception.Message);
    }

    #endregion
}
