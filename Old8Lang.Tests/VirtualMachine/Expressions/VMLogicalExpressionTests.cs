using Old8Lang.Bytecode;
using Xunit;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机逻辑表达式测试
/// </summary>
public class VMLogicalExpressionTests
{
    [Fact]
    public void LogicalExpression_And_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result1 <- true && true
            result2 <- true && false
            result3 <- false && true
            result4 <- false && false
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(true, vm.GetGlobalVariable("result1"));
        Assert.Equal(false, vm.GetGlobalVariable("result2"));
        Assert.Equal(false, vm.GetGlobalVariable("result3"));
        Assert.Equal(false, vm.GetGlobalVariable("result4"));
    }

    [Fact]
    public void LogicalExpression_Or_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result1 <- true || true
            result2 <- true || false
            result3 <- false || true
            result4 <- false || false
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(true, vm.GetGlobalVariable("result1"));
        Assert.Equal(true, vm.GetGlobalVariable("result2"));
        Assert.Equal(true, vm.GetGlobalVariable("result3"));
        Assert.Equal(false, vm.GetGlobalVariable("result4"));
    }

    [Fact]
    public void LogicalExpression_Not_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result1 <- !true
            result2 <- !false
            result3 <- !(true && false)
            result4 <- !(false || false)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(false, vm.GetGlobalVariable("result1"));
        Assert.Equal(true, vm.GetGlobalVariable("result2"));
        Assert.Equal(true, vm.GetGlobalVariable("result3"));
        Assert.Equal(true, vm.GetGlobalVariable("result4"));
    }
}
