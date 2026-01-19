using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Debug;

public class NestedArrayDebugTest
{
    [Fact]
    public void TestNestedArrayAccess()
    {
        var code = @"
            deepArray <- [[[1]]]
            PrintLine(""Array type: "" + type(deepArray))

            level1 <- deepArray[0]
            PrintLine(""Level1 type: "" + type(level1))
            PrintLine(""Level1 length: "" + len(level1).ToStr())

            level2 <- level1[0]
            PrintLine(""Level2 type: "" + type(level2))
            PrintLine(""Level2 length: "" + len(level2).ToStr())

            value <- level2[0]
            PrintLine(""Value type: "" + type(value))
            PrintLine(""Value: "" + value.ToStr())
        ";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
        vm.Execute();
    }
}
