using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

public class GenericListExtensionRegressionTests
{
    [Fact]
    public void GenericListExtension_ConditionalObjectReturn_PreservesRuntimeType()
    {
        const string code = @"
extension list<T> {
    func phaseOneMaybeReceiver(flag: bool) -> object {
        if flag { return 7 }
        return this
    }
}
items <- {1, 2}
result <- items.phaseOneMaybeReceiver(true)
Assert.Equal(7, result)
Assert.Equal(8, result + 1)
Assert.Equal(1, result.CompareTo(6))
";
        var interpreter = new LangInterpreter();
        var action = Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter);
        action();
    }

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

    [Fact]
    public void GenericDictExtension_PreservesAliasesMutationsAndChaining()
    {
        const string code = @"
extension dict<K, V> {
    func putAndReturn(key: K, val: V) -> dict<K, V> {
        this[key] <- val
        return this
    }
    func mutateNested(other: dict<string, int>) -> void {
        other[""added""] <- 99
    }
}
d <- {""a"": 1}
alias <- d
returned <- d.putAndReturn(""b"", 2)
returned.putAndReturn(""c"", 3)
Assert.Equal(3, d.Count())
Assert.Equal(2, alias[""b""])
Assert.Equal(3, alias[""c""])

other <- {""x"": 10}
otherAlias <- other
d.mutateNested(other)
Assert.Equal(99, otherAlias[""added""])
";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void GenericArrayExtension_PreservesAliasesMutationsAndChaining()
    {
        const string code = @"
extension array<T> {
    func setFirstAndReturn(val: T) -> array<T> {
        this[0] <- val
        return this
    }
}
arr <- [1, 2, 3]
alias <- arr
returned <- arr.setFirstAndReturn(42)
returned.setFirstAndReturn(100)
Assert.Equal(100, arr[0])
Assert.Equal(100, alias[0])
Assert.Equal(3, arr.Length())
";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void GenericExtension_EmptyCollections_HandleDynamicInferenceAndTypedReturn()
    {
        const string code = @"
extension list<T> where T: struct {
    func countOrZero() -> int { return this.Count() }
    func fallbackValue(v: int) -> T { return v }
}
extension dict<K, V> where K: class, V: struct {
    func entryCount() -> int { return this.Count() }
}
extension array<T> where T: struct {
    func arrayLen() -> int { return this.Count() }
}
emptyList <- {}
emptyArr <- []
emptyDict: dict <- {}
Assert.Equal(0, emptyList.countOrZero())
Assert.Equal(5, emptyList.fallbackValue(5))
Assert.Equal(0, emptyArr.arrayLen())
Assert.Equal(0, emptyDict.entryCount())
";
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void GenericExtension_MultiTypeParamCrossConstraints_EnforcedAcrossModes()
    {
        const string validCode = @"
extension dict<K, V> where K: V {
    func crossOk() -> int { return this.Count() }
}
d <- {1: 2, 3: 4}
res <- d.crossOk()
Assert.Equal(2, res)
";
        // Interpreter
        var interp = new LangInterpreter();
        interp.Build(validCode).Run(interp.Manager);
        // IL
        var ilInterp = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(ilInterp.Build(validCode), "test", ilInterp)();
        // VM (including bytecode serialization roundtrip)
        var bytecode = VirtualMachine.CompileHelper.CompileToBytecode(validCode);
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            bytecode.WriteTo(writer);
        ms.Position = 0;
        Old8Lang.Bytecode.BytecodeFile reloaded;
        using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            reloaded = Old8Lang.Bytecode.BytecodeFile.ReadFrom(reader);
        var vm = new Old8Lang.Bytecode.VM.VirtualMachine(reloaded);
        vm.Execute();
        Assert.Equal(2, vm.GetGlobalVariable("res"));

        const string invalidCode = @"
extension dict<K, V> where K: V {
    func crossFail() -> int { return this.Count() }
}
d <- {1: 2, 3: ""mismatch""}
d.crossFail()
";
        // Interpreter rejects mismatched later entry
        var badInterp = new LangInterpreter();
        Assert.Throws<Old8Lang.Error.TypeError>(() => badInterp.Build(invalidCode).Run(badInterp.Manager));
        // IL rejects mismatched later entry
        var badIlInterp = new LangInterpreter();
        var badAction = Old8Lang.Compiler.Compiler.Compile(badIlInterp.Build(invalidCode), "test", badIlInterp);
        Assert.Throws<Old8Lang.Error.TypeError>(badAction);
        // VM rejects mismatched later entry
        var badVm = new Old8Lang.Bytecode.VM.VirtualMachine(VirtualMachine.CompileHelper.CompileToBytecode(invalidCode));
        Assert.Throws<Old8Lang.Error.TypeError>(() => badVm.Execute());
    }

    [Fact]
    public void GenericExtension_NewClassStructAndParamBinding_EnforcedAcrossModes()
    {
        // new() + class + struct 约束：string 没有无参构造，不满足 new()；int 满足 struct & new()
        const string validNew = @"
extension list<T> where T: struct & new() {
    func extNewValid(item: T) -> T { return item }
}
nums <- {10, 20}
Assert.Equal(30, nums.extNewValid(30))
";
        var interp1 = new LangInterpreter();
        interp1.Build(validNew).Run(interp1.Manager);
        var il1 = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(il1.Build(validNew), "test", il1)();

        // 参数绑定违背 T = int
        const string badParam = @"
extension list<T> {
    func extParamCheck(item: T) -> void { this.Add(item) }
}
nums <- {1, 2}
nums.extParamCheck(""not an int"")
";
        var badParamInterp = new LangInterpreter();
        Assert.Throws<Old8Lang.Error.TypeError>(() => badParamInterp.Build(badParam).Run(badParamInterp.Manager));
        var badParamIl = new LangInterpreter();
        Assert.Throws<Old8Lang.Error.TypeError>(() =>
            Old8Lang.Compiler.Compiler.Compile(badParamIl.Build(badParam), "test", badParamIl)());

        // class & new() 约束对 Old8Lang 自定义类生效（有/无无参 init）
        const string userClassNewValid = @"
class WithDefault {
    public v: int
    func init() -> void { this.v <- 1 }
}
extension list<T> where T: class & new() {
    func extClassNewOk() -> int { return this.Count() }
}
items <- {WithDefault()}
Assert.Equal(1, items.extClassNewOk())
";
        var uInterp = new LangInterpreter();
        uInterp.Build(userClassNewValid).Run(uInterp.Manager);

        const string userClassNewInvalid = @"
class NoDefault {
    public v: int
    func init(x: int) -> void { this.v <- x }
}
extension list<T> where T: class & new() {
    func extClassNewFail() -> int { return this.Count() }
}
items <- {NoDefault(5)}
items.extClassNewFail()
";
        var uBadInterp = new LangInterpreter();
        Assert.Throws<Old8Lang.Error.TypeError>(() => uBadInterp.Build(userClassNewInvalid).Run(uBadInterp.Manager));

        // VM class 约束拒绝值类型
        const string vmClassFail = @"
extension list<T> where T: class {
    func vmClassOnly() -> int { return 1 }
}
nums <- {1, 2}
nums.vmClassOnly()
";
        var vmFail = new Old8Lang.Bytecode.VM.VirtualMachine(VirtualMachine.CompileHelper.CompileToBytecode(vmClassFail));
        Assert.Throws<Old8Lang.Error.TypeError>(() => vmFail.Execute());
    }
}
