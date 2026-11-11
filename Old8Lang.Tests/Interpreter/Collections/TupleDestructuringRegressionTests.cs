using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Collections;

public class TupleDestructuringRegressionTests
{
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
