using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

public class GenericListExtensionRegressionTests
{
    [Fact]
    public void GenericListExtension_ReceivesNativeListAndReturnsValue()
    {
        const string code = @"
extension list<T> {
    func phaseOneListSum() -> int {
        total <- 0
        for item in this {
            total <- total + item
        }
        return total
    }
}
items <- {1, 2, 3}
Assert.Equal(6, items.phaseOneListSum())
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }
}
