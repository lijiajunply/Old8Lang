namespace Old8Lang.Tests.VirtualMachine.Collections;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

/// <summary>
/// 虚拟机：元组实例方法（此前报「实例必须是 Tuple 类型」）。
/// </summary>
/// <remarks>
/// VM 的元组是右嵌套原生 <c>Tuple</c>；现在分派前先按 VM 编码展平并转成
/// <c>TupleLangValue</c>，方法实现（包括 lambda 参数）走 VM 路径。
/// </remarks>
public class VMTupleMethodTests
{
    [Fact]
    public void TupleMethods_OnNestedVmTuple_Work()
    {
        const string code = @"
            tuple <- (1, 2, 3)
            v1 <- tuple.Get(1)
            v2 <- tuple.Slice(0, 2).ToStr()
            v3 <- tuple.ToList().ToStr()
            v4 <- tuple.Reverse().ToStr()
            v5 <- tuple.Contains(2)
            v6 <- tuple.All((x) -> x > 0)
            v7 <- tuple.Filter((x) -> x > 1).ToStr()
            v8 <- tuple.Map((x) -> x * 2).ToStr()
        ";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(2, vm.GetGlobalVariable("v1"));
        Assert.Equal("(1, 2)", vm.GetGlobalVariable("v2"));
        Assert.Equal("[1, 2, 3]", vm.GetGlobalVariable("v3"));
        Assert.Equal("[3, 2, 1]", vm.GetGlobalVariable("v4"));
        Assert.Equal(true, vm.GetGlobalVariable("v5"));
        Assert.Equal(true, vm.GetGlobalVariable("v6"));
        Assert.Equal("[2, 3]", vm.GetGlobalVariable("v7"));
        Assert.Equal("[2, 4, 6]", vm.GetGlobalVariable("v8"));
    }
}
