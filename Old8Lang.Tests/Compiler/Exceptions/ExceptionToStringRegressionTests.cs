using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Exceptions;

public class ExceptionToStringRegressionTests
{
    [Fact]
    public void CaughtIndexError_ToStrContainsOriginalIndex()
    {
        const string code = @"
            caught <- false
            values <- [1, 2, 3]
            try {
                value <- values[-5]
            } catch (e) {
                Assert.Contains(e.ToStr(), ""-5"")
                caught <- true
            }
            Assert.True(caught)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }

    [Fact]
    public void CaughtThrownMessage_ToStrContainsMessage()
    {
        const string code = @"
            caught <- false
            try {
                throw ""unique sentinel""
            } catch (e) {
                message <- e.ToStr()
                Assert.Contains(message, ""unique sentinel"")
                caught <- true
            }
            Assert.True(caught)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }
}
