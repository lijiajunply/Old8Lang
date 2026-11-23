using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Collections;

public class VMTupleDestructuringRegressionTests
{
    [Theory]
    [InlineData("(a, (b, c)) <- (1, [2])")]
    [InlineData("(a, (b, c)) <- (1, 2)")]
    public void TuplePattern_InvalidNestedValueThrowsLanguageError(string code)
    {
        var vm = new Old8Lang.Bytecode.VM.VirtualMachine(new BytecodeCompiler().Compile(new LangInterpreter().Build(code)));
        Assert.ThrowsAny<Old8Lang.Error.Old8Exception>(() => vm.Execute());
    }

    [Theory]
    [InlineData("(a, (b, c)) <- [1, {2, 3}]")]
    [InlineData("(a, (b, c)) <- (1, (2, 3))")]
    [InlineData("(a, b, c) <- (1, 2, 3)")]
    [InlineData("((a, b), c) <- ((1, 2), 3)")]
    public void TuplePattern_NestedPatternsUnpackMixedSequences(string assignment)
    {
        var code = assignment + "\nAssert.Equal(1, a)\nAssert.Equal(2, b)\nAssert.Equal(3, c)";
        var ast = new LangInterpreter().Build(code);
        new Old8Lang.Bytecode.VM.VirtualMachine(new BytecodeCompiler().Compile(ast)).Execute();
    }

    [Fact]
    public void TuplePattern_NestedTupleAndNullSurviveBytecodeRoundTrip()
    {
        const string code = "(a, (b, c)) <- (null, (2, 3))\nAssert.Equal(null, a)\nAssert.Equal(2, b)\nAssert.Equal(3, c)";
        var bytecode = new BytecodeCompiler().Compile(new LangInterpreter().Build(code));
        var path = Path.Combine(Path.GetTempPath(), $"o8tuple_{Guid.NewGuid():N}.o8bc");
        try
        {
            bytecode.SaveToFile(path);
            new Old8Lang.Bytecode.VM.VirtualMachine(BytecodeFile.LoadFromFile(path)).Execute();
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TuplePattern_UnpacksArrayAndListValues()
    {
        const string code = @"
            (a, b) <- [4, 5, 6]
            (c, d) <- {7, 8}
            Assert.Equal(4, a)
            Assert.Equal(5, b)
            Assert.Equal(7, c)
            Assert.Equal(8, d)
        ";
        var ast = new LangInterpreter().Build(code);
        new Old8Lang.Bytecode.VM.VirtualMachine(new BytecodeCompiler().Compile(ast)).Execute();
    }

    [Fact]
    public void TuplePattern_AssignsAndSwapsValues()
    {
        const string code = @"
(a, b) <- (1, 2)
(a, b) <- (b, a)
Assert.Equal(2, a)
Assert.Equal(1, b)
";
        var ast = new LangInterpreter().Build(code);
        new Old8Lang.Bytecode.VM.VirtualMachine(new BytecodeCompiler().Compile(ast)).Execute();
    }
}
