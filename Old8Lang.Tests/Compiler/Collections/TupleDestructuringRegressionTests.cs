using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

public class TupleDestructuringRegressionTests
{
    [Fact]
    public void TuplePattern_AssignsAndSwapsValuesIncludingLongTuples()
    {
        const string code = @"
(a, b) <- (1, 2)
(a, b) <- (b, a)
Assert.Equal(2, a)
Assert.Equal(1, b)
(p, q, r, s, t, u, v, w) <- (1, 2, 3, 4, 5, 6, 7, 8)
Assert.Equal(8, w)
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

    [Fact]
    public void TuplePattern_TooFewElementsIsRejected()
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build("(a, b, c) <- (1, 2)");
        Assert.ThrowsAny<Exception>(() => Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
    }
}
