using Old8Lang.Bytecode;
using Xunit;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机比较表达式测试
/// </summary>
public class VMComparisonExpressionTests
{
    [Fact]
    public void ComparisonExpression_Equal_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result1 <- 10 == 10
            result2 <- 10 == 20
            result3 <- ""hello"" == ""hello""
            result4 <- ""hello"" == ""world""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(true, vm.GetGlobalVariable("result1"));
        Assert.Equal(false, vm.GetGlobalVariable("result2"));
        Assert.Equal(true, vm.GetGlobalVariable("result3"));
        Assert.Equal(false, vm.GetGlobalVariable("result4"));
    }

    [Fact]
    public void ComparisonExpression_NotEqual_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result1 <- 10 != 10
            result2 <- 10 != 20
            result3 <- ""hello"" != ""hello""
            result4 <- ""hello"" != ""world""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(false, vm.GetGlobalVariable("result1"));
        Assert.Equal(true, vm.GetGlobalVariable("result2"));
        Assert.Equal(false, vm.GetGlobalVariable("result3"));
        Assert.Equal(true, vm.GetGlobalVariable("result4"));
    }

    [Fact]
    public void ComparisonExpression_GreaterThan_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result1 <- 20 > 10
            result2 <- 10 > 20
            result3 <- 10 > 10
            result4 <- 5.5 > 3.2
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(true, vm.GetGlobalVariable("result1"));
        Assert.Equal(false, vm.GetGlobalVariable("result2"));
        Assert.Equal(false, vm.GetGlobalVariable("result3"));
        Assert.Equal(true, vm.GetGlobalVariable("result4"));
    }

    [Fact]
    public void ComparisonExpression_LessThan_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result1 <- 10 < 20
            result2 <- 20 < 10
            result3 <- 10 < 10
            result4 <- 3.2 < 5.5
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(true, vm.GetGlobalVariable("result1"));
        Assert.Equal(false, vm.GetGlobalVariable("result2"));
        Assert.Equal(false, vm.GetGlobalVariable("result3"));
        Assert.Equal(true, vm.GetGlobalVariable("result4"));
    }
}
