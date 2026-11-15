namespace Old8Lang.Tests.VirtualMachine.Expressions;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

/// <summary>
/// 虚拟机：数组 / 列表 / 字符串 / 字典的 <c>.Length</c> 属性。
/// </summary>
public class VMLengthPropertyTests
{
    [Theory]
    [InlineData("array <- [1, 2, 3]", "array", 3)]
    [InlineData("list <- {1, 2}", "list", 2)]
    [InlineData("text <- \"abc\"", "text", 3)]
    [InlineData("dict <- {\"a\": 1, \"b\": 2}", "dict", 2)]
    public void LengthProperty_EqualsLen(string declaration, string name, int expected)
    {
        var code = declaration + $@"
lengthValue <- {name}.Length
lenValue <- len({name})
";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(expected, vm.GetGlobalVariable("lengthValue"));
        Assert.Equal(expected, vm.GetGlobalVariable("lenValue"));
    }
}
