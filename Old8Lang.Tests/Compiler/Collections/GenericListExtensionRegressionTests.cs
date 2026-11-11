using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

public class GenericListExtensionRegressionTests
{
    [Fact]
    public void IntegerExtension_StillAcceptsNativeValueAndArguments()
    {
        const string code = @"
extension int {
    func phaseOneAdd(x: int) -> int { return this + x }
}
value <- 3
Assert.Equal(8, value.phaseOneAdd(5))
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

    [Fact]
    public void GenericListExtension_MutationsUpdateOriginalAndAliases()
    {
        const string code = @"
extension list<T> {
    func phaseOneAppend() -> void {
        this.Add(4)
    }
}
items <- {1, 2, 3}
alias <- items
items.phaseOneAppend()
Assert.Equal(4, items.Count())
Assert.Equal(4, alias[3])
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

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
