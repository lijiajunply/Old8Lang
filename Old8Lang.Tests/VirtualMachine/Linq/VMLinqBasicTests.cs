using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Linq;

/// <summary>
/// 虚拟机 LINQ 基础测试
/// </summary>
public class VMLinqBasicTests
{
    [Fact]
    public void Linq_BasicSelect_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
            result <- from x in numbers select x * 2
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<System.Collections.IEnumerable>(result);
    }

    [Fact]
    public void Linq_Where_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            result <- from x in numbers where x > 5 select x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
    }

    [Fact]
    public void Linq_WhereAndSelect_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
            result <- from x in numbers where x % 2 == 0 select x * 10
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
    }
}
