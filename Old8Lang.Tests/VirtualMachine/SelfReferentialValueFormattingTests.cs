using Old8Lang.Bytecode;
using Old8Lang.Bytecode.Generators;

namespace Old8Lang.Tests.VirtualMachine;

public class SelfReferentialValueFormattingTests
{
    [Fact]
    public void GeneratorValues_CanBeFormattedWithoutRecursing()
    {
        var vm = new Old8Lang.Bytecode.VM.VirtualMachine(new BytecodeFile());
        var generator = new BytecodeGeneratorLangValue(1, vm);
        var asyncGenerator = new BytecodeAsyncGeneratorLangValue(2, vm);
        var stream = new BytecodeAsyncStreamLangValue(3, vm);

        Assert.Equal("Generator(1)", generator.ToString());
        Assert.Equal("AsyncGenerator(2)", asyncGenerator.ToString());
        Assert.Equal("AsyncStream(3)", stream.ToString());
    }
}
