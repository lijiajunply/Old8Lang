using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

public class TupleDestructuringRegressionTests
{
    [Theory]
    [InlineData("(a, (b, c)) <- [1, {2, 3}]")]
    [InlineData("(a, (b, c)) <- (1, (2, 3))")]
    [InlineData("((a, b), c) <- ((1, 2), 3)")]
    public void TuplePattern_NestedPatternsUnpackMixedSequences(string assignment)
    {
        var code = assignment + "\nAssert.Equal(1, a)\nAssert.Equal(2, b)\nAssert.Equal(3, c)";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
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
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

    [Fact]
    public void TuplePattern_UpdatesExistingVariablesAndAcceptsDynamicValues()
    {
        const string code = @"
            a <- 0
            b <- 0
            func values() -> object { return [12, 13] }
            (a, b) <- values()
            Assert.Equal(12, a)
            Assert.Equal(13, b)
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void TuplePattern_UpdatesGlobalReadByFunction()
    {
        const string code = @"
            a <- 0
            func read() -> int { return a }
            (a, b) <- [12, 13]
            Assert.Equal(12, read())
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void TuplePattern_UpdatesFunctionParametersAndEvaluatesRightSideOnce()
    {
        const string code = @"
            calls <- 0
            func values() -> object { calls <- calls + 1 return [9, 8] }
            func test(a:int, b:int) -> int {
                (a, b) <- values()
                return a * 10 + b
            }
            Assert.Equal(98, test(1, 2))
            Assert.Equal(1, calls)
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void TuplePattern_InAsyncFunction_PreservesValuesAcrossAwait()
    {
        const string code = @"
            async func compute() -> int {
                (a, b) <- [4, 5]
                await Task.Delay(10)
                return a * 10 + b
            }
            result <- await compute()
            Assert.Equal(45, result)
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void TuplePattern_InAsyncFunction_UpdatesParametersAndGlobal()
    {
        const string code = @"
            global <- 0
            async func compute(a:int) -> int {
                (a, global) <- (7, 8)
                await Task.Delay(10)
                return a + global
            }
            result <- await compute(1)
            Assert.Equal(15, result)
            Assert.Equal(8, global)
        ";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Theory]
    [InlineData("(a, b) <- [1]")]
    [InlineData("(a, b) <- 1")]
    [InlineData("(a, (b, c)) <- (1, [2])")]
    [InlineData("(a, (b, c)) <- (1, 2)")]
    public void TuplePattern_InvalidCollectionFailsAtRuntime(string code)
    {
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        Assert.ThrowsAny<Old8Lang.Error.Old8Exception>(() => action());
    }

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
