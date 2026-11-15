namespace Old8Lang.Tests.VirtualMachine.Collections;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

/// <summary>
/// 虚拟机：数组 / 列表的 <c>+</c> 按顺序拼接。
/// </summary>
/// <remarks>
/// 数组拼接此前已有（原生 <c>object[]</c>），本轮补上列表
/// （VM 的列表字面量是原生 <c>List&lt;object?&gt;</c>）。
/// </remarks>
public class VMCollectionConcatTests
{
    [Fact]
    public void ArrayPlusArray_ConcatenatesInOrder()
    {
        const string code = @"
left <- [1, 2]
right <- [3, 4]
joined <- left + right
";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var joined = Assert.IsType<object[]>(vm.GetGlobalVariable("joined"));
        Assert.Equal(4, joined.Length);
        Assert.Equal(1, joined[0]);
        Assert.Equal(4, joined[3]);
        Assert.Equal(2, Assert.IsType<object[]>(vm.GetGlobalVariable("left")).Length);
    }

    [Fact]
    public void ListPlusList_ConcatenatesInOrder()
    {
        const string code = @"
left <- {1, 2}
right <- {3, 4}
joined <- left + right
";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        var joined = Assert.IsType<List<object?>>(vm.GetGlobalVariable("joined"));
        Assert.Equal(4, joined.Count);
        Assert.Equal(1, joined[0]);
        Assert.Equal(4, joined[3]);
        Assert.Equal(2, Assert.IsType<List<object?>>(vm.GetGlobalVariable("left")).Count);
    }
}
