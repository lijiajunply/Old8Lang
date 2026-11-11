using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

public class StringLengthRegressionTests
{
    [Fact]
    public void NativeString_LengthAndCount_ReturnNativeIntegers()
    {
        const string code = @"
            text <- ""abc""
            Assert.Equal(3, text.Length())
            Assert.Equal(3, text.Count())
            Assert.Equal(0, """".length())
            Assert.Equal(""3"", text.Length().ToStr())
            Assert.True(text.Length() > 0)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }
}
