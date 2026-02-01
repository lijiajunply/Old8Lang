using System.Collections;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Linq;

/// <summary>
/// 虚拟机 LINQ Join 和 OrderBy 测试
/// </summary>
public class VMLinqJoinOrderByTests
{
    [Fact]
    public void Linq_OrderBy_Ascending_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {5, 2, 8, 1, 9, 3}
            result <- from x in numbers orderby x select x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable>(result);

        var list = ((IEnumerable)result).Cast<object>().ToList();
        Assert.Equal(6, list.Count);
        // 验证排序结果
        Assert.Equal(1, Convert.ToInt32(list[0]));
        Assert.Equal(2, Convert.ToInt32(list[1]));
        Assert.Equal(3, Convert.ToInt32(list[2]));
    }

    [Fact]
    public void Linq_OrderBy_Descending_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {5, 2, 8, 1, 9, 3}
            result <- from x in numbers orderby x descending select x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable>(result);

        var list = ((IEnumerable)result).Cast<object>().ToList();
        Assert.Equal(6, list.Count);
        // 验证降序排序结果
        Assert.Equal(9, Convert.ToInt32(list[0]));
        Assert.Equal(8, Convert.ToInt32(list[1]));
        Assert.Equal(5, Convert.ToInt32(list[2]));
    }

    [Fact]
    public void Linq_InnerJoin_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 3}
            list2 <- {2, 3, 4}
            result <- from x in list1 join y in list2 on x == y select x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable>(result);

        var list = ((IEnumerable)result).Cast<object>().ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(2, Convert.ToInt32(list[0]));
        Assert.Equal(3, Convert.ToInt32(list[1]));
    }

    [Fact]
    public void Linq_InnerJoin_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 3}
            list2 <- {4, 5, 6}
            result <- from x in list1 join y in list2 on x == y select x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable>(result);

        var list = ((IEnumerable)result).Cast<object>().ToList();
        Assert.Empty(list);
    }

    [Fact]
    public void Linq_GroupJoin_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            categories <- {1, 2, 3}
            items <- {1, 1, 2, 2, 2, 3}
            result <- from c in categories join i in items on c == i into g select g.Count()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable>(result);

        var list = ((IEnumerable)result).Cast<object>().ToList();
        Assert.Equal(3, list.Count);
        Assert.Equal(2, Convert.ToInt32(list[0])); // category 1 has 2 items
        Assert.Equal(3, Convert.ToInt32(list[1])); // category 2 has 3 items
        Assert.Equal(1, Convert.ToInt32(list[2])); // category 3 has 1 item
    }

    [Fact]
    public void Linq_JoinWithWhere_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 3, 4, 5}
            list2 <- {2, 3, 4}
            result <- from x in list1 join y in list2 on x == y where x > 2 select x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable>(result);

        var list = ((IEnumerable)result).Cast<object>().ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(3, Convert.ToInt32(list[0]));
        Assert.Equal(4, Convert.ToInt32(list[1]));
    }

    [Fact]
    public void Linq_Let_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5}
            result <- from x in numbers let doubled <- x * 2 select doubled
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable>(result);

        var list = ((IEnumerable)result).Cast<object>().ToList();
        Assert.Equal(5, list.Count);
        Assert.Equal(2, Convert.ToInt32(list[0]));
        Assert.Equal(4, Convert.ToInt32(list[1]));
        Assert.Equal(6, Convert.ToInt32(list[2]));
    }

    [Fact]
    public void Linq_GroupBy_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6}
            result <- from x in numbers group x by x % 2
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable>(result);

        var list = ((IEnumerable)result).Cast<object>().ToList();
        Assert.Equal(2, list.Count); // 两组：奇数和偶数
    }

    [Fact]
    public void Linq_WhereOrderBy_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- {5, 2, 8, 1, 9, 3, 7, 4, 6}
            result <- from x in numbers where x > 3 orderby x select x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable>(result);

        var list = ((IEnumerable)result).Cast<object>().ToList();
        Assert.Equal(6, list.Count); // 4, 5, 6, 7, 8, 9
        Assert.Equal(4, Convert.ToInt32(list[0]));
        Assert.Equal(5, Convert.ToInt32(list[1]));
        Assert.Equal(9, Convert.ToInt32(list[5]));
    }
}
