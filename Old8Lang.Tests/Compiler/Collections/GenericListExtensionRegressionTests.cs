using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

public class GenericListExtensionRegressionTests
{
    [Fact]
    public void GenericListExtension_SelfReferentialArrayDoesNotOverflow()
    {
        const string code = @"
extension list<T> {
    func phaseOneCyclicArray() -> int { return 1 }
}
items <- [null]
items[0] <- items
receiver <- {items}
Assert.Equal(1, receiver.phaseOneCyclicArray())
Assert.Equal(1, items.Length())
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
        var items = Assert.IsType<Old8Lang.AST.Expression.Value.ArrayLangValue>(
            interpreter.Manager.GetValue(new Old8Lang.AST.Expression.LangId("items")));
        var receiver = Assert.IsType<Old8Lang.AST.Expression.Value.ListLangValue>(
            interpreter.Manager.GetValue(new Old8Lang.AST.Expression.LangId("receiver")));
        Assert.Same(items, items.RunResult[0]);
        Assert.Same(items, receiver.Values[0]);
    }

    [Fact]
    public void GenericListExtension_RejectsMixedElementsThatViolateConstraint()
    {
        const string code = @"
extension list<T> where T: struct {
    func phaseOneMixedConstraint() -> int { return 1 }
}
values <- {1, ""text""}
values.phaseOneMixedConstraint()
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        Assert.Throws<Old8Lang.Error.TypeError>(action);
    }

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

    [Fact]
    public void GenericListExtension_PreservesNestedAliasesAndReturnedReceiver()
    {
        const string code = @"
extension list<T> {
    func phaseOneGrowFirst(value: int) -> list<T> {
        this[0].Add(value)
        return this
    }
}
nested <- {{1, 2}}
innerAlias <- nested[0]
returned <- nested.phaseOneGrowFirst(3)
returned.phaseOneGrowFirst(4)
Assert.Equal({{1, 2, 3, 4}}, nested)
Assert.Equal({1, 2, 3, 4}, innerAlias)
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

    [Fact]
    public void GenericListExtension_PreservesMutableArgumentAliases()
    {
        const string code = @"
extension list<T> {
    func phaseOneAppendTo(other: list<int>) -> void {
        other.Add(9)
    }
}
receiver <- {1}
argument <- {2}
alias <- argument
receiver.phaseOneAppendTo(argument)
Assert.Equal(2, alias.Count())
Assert.Equal(9, argument[1])
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

    [Fact]
    public void GenericListExtension_EnforcesTargetElementConstraints()
    {
        const string validCode = @"
extension list<T> where T: struct {
    func phaseOneValueOnly() -> int { return 1 }
}
values <- {1}
Assert.Equal(1, values.phaseOneValueOnly())
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(validCode), "test", interpreter);
        action();

        const string invalidCode = @"
extension list<T> where T: struct {
    func phaseOneStructOnly() -> int { return 1 }
}
values <- {""text""}
values.phaseOneStructOnly()
";
        var invalidInterpreter = new LangInterpreter();
        var invalidAction = Old8Lang.Compiler.Compiler.Compile(invalidInterpreter.Build(invalidCode), "test", invalidInterpreter);
        Assert.Throws<Old8Lang.Error.TypeError>(invalidAction);

        const string targetBindingCode = @"
extension list<T> {
    func phaseOneTypedReturn() -> T { return ""wrong"" }
}
values <- {1}
values.phaseOneTypedReturn()
";
        var bindingInterpreter = new LangInterpreter();
        var bindingAction = Old8Lang.Compiler.Compiler.Compile(
            bindingInterpreter.Build(targetBindingCode), "test", bindingInterpreter);
        Assert.Throws<Old8Lang.Error.TypeError>(bindingAction);
    }
}
