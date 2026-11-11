using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Modules;

public class VMStandardLibraryImportTests
{
    [Fact]
    public void StandardLibraryModule_StillWorksAfterBytecodeRoundTrip()
    {
        const string code = "import \"Math\" as maths\nAssert.Equal(3.0, maths.Sqrt(9))";
        var ast = new LangInterpreter().Build(code);
        var bytecode = new BytecodeCompiler().Compile(ast);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            bytecode.WriteTo(writer);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var restored = BytecodeFile.ReadFrom(reader);
        new Old8Lang.Bytecode.VM.VirtualMachine(restored).Execute();
    }

    [Theory]
    [InlineData("import \"Math\"", "Math")]
    [InlineData("import \"Math\" as maths", "maths")]
    public void StandardLibraryModule_ExposesFunctions(string import, string name)
    {
        var code = $"{import}\nAssert.Equal(3.0, {name}.Sqrt(9))\nAssert.Equal(8.0, {name}.Pow(2, 3))";
        var ast = new LangInterpreter().Build(code);
        var vm = new Old8Lang.Bytecode.VM.VirtualMachine(new BytecodeCompiler().Compile(ast));
        vm.Execute();
        Assert.NotNull(vm.GetGlobalVariable(name));
        if (name != "Math")
            Assert.Null(vm.GetGlobalVariable("Math"));
    }

    [Theory]
    [InlineData("import { Sqrt } from \"Math\"", "Sqrt")]
    [InlineData("import { Sqrt as root } from \"Math\"", "root")]
    public void StandardLibrarySelectiveImport_ExposesFunctions(string import, string name)
    {
        var code = $"{import}\nAssert.Equal(3.0, {name}(9))";
        var ast = new LangInterpreter().Build(code);
        new Old8Lang.Bytecode.VM.VirtualMachine(new BytecodeCompiler().Compile(ast)).Execute();
    }
}
