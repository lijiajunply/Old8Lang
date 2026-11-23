using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Collections;

public class TupleDestructuringRegressionTests
{
    [Theory]
    [InlineData("(a, (b, c)) <- (1, [2])")]
    [InlineData("(a, (b, c)) <- (1, 2)")]
    public void TuplePattern_InvalidNestedValueThrowsLanguageError(string code)
    {
        var interpreter = new LangInterpreter();
        Assert.ThrowsAny<Old8Lang.Error.Old8Exception>(() => interpreter.Build(code).Run(interpreter.Manager));
    }

    [Theory]
    [InlineData("(a, (b, c)) <- [1, {2, 3}]")]
    [InlineData("(a, (b, c)) <- (1, (2, 3))")]
    [InlineData("((a, b), c) <- ((1, 2), 3)")]
    public void TuplePattern_NestedPatternsUnpackMixedSequences(string code)
    {
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("a"))).Value);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("b"))).Value);
        Assert.Equal(3, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("c"))).Value);
    }

    [Fact]
    public void TuplePattern_UnpacksArrayAndListValues()
    {
        const string code = "(a, b) <- [4, 5, 6]\n(c, d) <- {7, 8}";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);
        Assert.Equal(4, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("a"))).Value);
        Assert.Equal(5, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("b"))).Value);
        Assert.Equal(7, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("c"))).Value);
        Assert.Equal(8, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("d"))).Value);
    }

    [Fact]
    public void ConsecutiveStatements_RecognizeTupleAssignment()
    {
        const string code = @"
a <- 1
b <- 2
(a, b) <- (b, a)
(c, d) <- (a, b)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("c"))).Value);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("d"))).Value);
    }

    [Fact]
    public void TuplePattern_AssignsValuesAndCanSwapExistingVariables()
    {
        const string code = "(a, b) <- (b, a)";
        var interpreter = new LangInterpreter();
        interpreter.Manager.Set(new LangId("a"), new IntLangValue(1));
        interpreter.Manager.Set(new LangId("b"), new IntLangValue(2));
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("a"))).Value);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("b"))).Value);
    }
}
