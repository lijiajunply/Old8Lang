using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Collections;

public class VMTupleDestructuringRegressionTests
{
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
