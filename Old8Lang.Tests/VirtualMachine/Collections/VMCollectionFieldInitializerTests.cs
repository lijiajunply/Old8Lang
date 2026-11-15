namespace Old8Lang.Tests.VirtualMachine.Collections;

using VM = Old8Lang.Bytecode.VM.VirtualMachine;

/// <summary>
/// 虚拟机：集合字面量作为字段初值。
/// </summary>
/// <remarks>
/// <c>BytecodeCompiler.EvaluateConstantExpression</c> 原先只接受标量，
/// <c>class C { public items <- {1, 2, 3} }</c> 在字节码编译期抛
/// NotSupportedException；现在递归求值列表/数组/字典字面量，
/// 按 VM 运行期表示（List&lt;object?&gt; / object[] / Dictionary）注入。
/// </remarks>
public class VMCollectionFieldInitializerTests
{
    [Fact]
    public void ListDictionaryAndArrayFieldInitializers_Work()
    {
        const string code = @"
class Holder {
    public items <- {1, 2, 3}
    public lookup <- {""a"": 1, ""b"": 2}
    public numbers <- [10, 20]
    public func total() {
        return this.items.Count() + this.lookup.Count() + this.numbers.Count()
    }
}
h <- Holder()
result <- h.total()
itemsText <- h.items.ToStr()
lookupText <- h.lookup.ToStr()
";

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        Assert.Equal(7, vm.GetGlobalVariable("result"));
        Assert.Equal("[1, 2, 3]", vm.GetGlobalVariable("itemsText"));
        Assert.Equal("{\"a\": 1, \"b\": 2}", vm.GetGlobalVariable("lookupText"));
    }
}
