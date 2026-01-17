using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Linq;

/// <summary>
/// 虚拟机 LINQ 高级测试 - 验证实际输出结果
/// </summary>
public class VMLinqAdvancedTests
{
    [Fact]
    public void Linq_Select_ReturnsCorrectValues()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3}
            result <- from x in numbers select x * 2
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);

        // 验证结果是列表类型
        var list = result as System.Collections.IList;
        Assert.NotNull(list);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Linq_Where_FiltersCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6}
            result <- from x in numbers where x > 3 select x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);

        var list = result as System.Collections.IList;
        Assert.NotNull(list);
        // 应该过滤出 4, 5, 6
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Linq_MultipleWhere_FiltersCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            result <- from x in numbers where x > 3 where x < 8 select x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);

        var list = result as System.Collections.IList;
        Assert.NotNull(list);
        // 应该过滤出 4, 5, 6, 7
        Assert.Equal(4, list.Count);
    }
}
