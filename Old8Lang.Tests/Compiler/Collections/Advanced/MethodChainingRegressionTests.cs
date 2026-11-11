using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections.Advanced;

public class MethodChainingRegressionTests
{
    [Fact]
    public void CountResult_CanCallToStr()
    {
        const string code = @"
            values <- {1, 2, 3}
            Assert.Equal(""3"", values.Count().ToStr())
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }

    [Fact]
    public void FindAllResult_CanCallCount()
    {
        const string code = @"
            values <- {1, 2, 3, 4}
            Assert.Equal(2, values.FindAll((x:int) -> x % 2 == 0).Count())
            Assert.Equal(""2"", values.FindAll((x:int) -> x % 2 == 0).Count().ToStr())
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }

    [Fact]
    public void ChainedReceiver_IsEvaluatedOnlyOnce()
    {
        const string code = @"
            class Factory {
                public calls:int <- 0
                func get() -> list {
                    this.calls <- this.calls + 1
                    return {1, 2, 3, 4}
                }
            }
            factory <- Factory()
            count <- factory.get().FindAll((x:int) -> x % 2 == 0).Count()
            Assert.Equal(2, count)
            Assert.Equal(1, factory.calls)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }
}
