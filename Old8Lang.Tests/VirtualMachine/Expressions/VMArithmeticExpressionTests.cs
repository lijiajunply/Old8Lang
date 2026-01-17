using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机算术表达式测试
/// </summary>
public class VMArithmeticExpressionTests
{
    [Fact]
    public void ArithmeticExpression_Addition_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 10 + 20
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(30, result);
    }

    [Fact]
    public void ArithmeticExpression_ComplexExpression_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- (10 + 5) * 2 - 3
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(27, result);
    }
}
