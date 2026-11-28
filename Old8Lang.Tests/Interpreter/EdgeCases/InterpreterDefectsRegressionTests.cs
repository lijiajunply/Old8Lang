using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.EdgeCases;

/// <summary>
/// 2026-11-28 解释器缺陷与边角缺口回归测试
/// </summary>
[Collection("Sequential")]
public class InterpreterDefectsRegressionTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void WhileLoop_IfReturnWithIndexedFieldAccess_DoesNotInfiniteLoop()
    {
        var code = @"
class RM {
    private resources <- {}
    func init() {
        this.resources.Add(""a"")
        this.resources.Add(""b"")
    }
    func has(name:string) -> bool {
        i <- 0
        while i < this.resources.Count() {
            if this.resources[i] == name { return true }
            i <- i + 1
        }
        return false
    }
}
rm <- RM()
foundA <- rm.has(""a"")
foundB <- rm.has(""b"")
foundC <- rm.has(""c"")
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.True(((BoolLangValue)interpreter.Manager.GetValue(new LangId("foundA"))!).Value);
        Assert.True(((BoolLangValue)interpreter.Manager.GetValue(new LangId("foundB"))!).Value);
        Assert.False(((BoolLangValue)interpreter.Manager.GetValue(new LangId("foundC"))!).Value);
    }

    [Fact]
    public void MultiLevelInheritance_ChainedSuperCalls_DoNotStackOverflow()
    {
        var code = @"
class Level1 {
    public trace:string <- """"
    func init(v:string) {
        this.trace <- ""L1:"" + v
    }
    func describe() -> string {
        return ""Level1""
    }
}

class Level2 extends Level1 {
    func init(v:string) {
        super.init(v)
        this.trace <- this.trace + ""->L2""
    }
    func describe() -> string {
        return super.describe() + ""->Level2""
    }
}

class Level3 extends Level2 {
    func init(v:string) {
        super.init(v)
        this.trace <- this.trace + ""->L3""
    }
    func describe() -> string {
        return super.describe() + ""->Level3""
    }
}

obj <- Level3(""ok"")
traceResult <- obj.trace
descResult <- obj.describe()
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal("L1:ok->L2->L3", ((StringLangValue)interpreter.Manager.GetValue(new LangId("traceResult"))!).Value);
        Assert.Equal("Level1->Level2->Level3", ((StringLangValue)interpreter.Manager.GetValue(new LangId("descResult"))!).Value);
    }

    [Fact]
    public void CollectionAndTupleLiterals_InLoopAndLinq_ReturnDistinctInstances()
    {
        var code = @"
items <- {
    {""cat"": ""A"", ""sub"": 1, ""val"": 10},
    {""cat"": ""A"", ""sub"": 2, ""val"": 20},
    {""cat"": ""B"", ""sub"": 1, ""val"": 30},
    {""cat"": ""A"", ""sub"": 1, ""val"": 40}
}

grouped <- from x in items group x by (x[""cat""], x[""sub""])
groupCount <- len(grouped)

projected <- from x in items select {""k"": x[""val""]}
firstVal <- projected[0][""k""]
secondVal <- projected[1][""k""]
lastVal <- projected[3][""k""]

rows <- {}
for i <- 0, i < 3, i++ {
    rows.Add({""idx"": i})
}
row0 <- rows[0][""idx""]
row1 <- rows[1][""idx""]
row2 <- rows[2][""idx""]
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(3, ((IntLangValue)interpreter.Manager.GetValue(new LangId("groupCount"))!).Value);
        Assert.Equal(10, ((IntLangValue)interpreter.Manager.GetValue(new LangId("firstVal"))!).Value);
        Assert.Equal(20, ((IntLangValue)interpreter.Manager.GetValue(new LangId("secondVal"))!).Value);
        Assert.Equal(40, ((IntLangValue)interpreter.Manager.GetValue(new LangId("lastVal"))!).Value);
        Assert.Equal(0, ((IntLangValue)interpreter.Manager.GetValue(new LangId("row0"))!).Value);
        Assert.Equal(1, ((IntLangValue)interpreter.Manager.GetValue(new LangId("row1"))!).Value);
        Assert.Equal(2, ((IntLangValue)interpreter.Manager.GetValue(new LangId("row2"))!).Value);
    }

    [Fact]
    public void AliasedModuleImports_SameTopLevelVariableName_StayIsolated()
    {
        var modA = @"
offset <- 10
func shift(x:int) -> int {
    return x + offset
}
";
        var modB = @"
offset <- 100
func shift(x:int) -> int {
    return x + offset
}
";
        var main = @"
offset <- 1
import ""iso_alias_a"" as a
import ""iso_alias_b"" as b

resA <- a.shift(5)
resB <- b.shift(5)
localOffset <- offset
";
        CreateTempModuleFile("iso_alias_a.old8", modA);
        CreateTempModuleFile("iso_alias_b.old8", modB);
        CreateTempModuleFile("iso_alias_main.old8", main);

        var (interpreter, exception) = ExecuteCodeFile("iso_alias_main.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "resA", 15);
        AssertVariableValue(interpreter, "resB", 105);
        AssertVariableValue(interpreter, "localOffset", 1);
    }

    [Fact]
    public void InfiniteRecursion_ThrowsCatchableRecursionErrorBeforeStackOverflow()
    {
        var code = @"
func boom(n:int) -> int {
    return boom(n + 1)
}
boom(0)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<RecursionError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void CallingNonFunctionValue_ThrowsInvalidOperationError()
    {
        var code = @"
num <- 42
res <- num(1)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<InvalidOperationError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void EnumArithmetic_SupportsEnumWithEnumAndNumbers()
    {
        var code = @"
enum Flags {
    Read <- 1,
    Write <- 2,
    Exec <- 4
}

combined <- Flags.Read + Flags.Write + Flags.Exec
diff <- Flags.Exec - Flags.Read
scaled <- Flags.Write * Flags.Exec
intPlusEnum <- 10 + Flags.Write
doublePlusEnum <- 1.5 + Flags.Read
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(7, ((IntLangValue)interpreter.Manager.GetValue(new LangId("combined"))!).Value);
        Assert.Equal(3, ((IntLangValue)interpreter.Manager.GetValue(new LangId("diff"))!).Value);
        Assert.Equal(8, ((IntLangValue)interpreter.Manager.GetValue(new LangId("scaled"))!).Value);
        Assert.Equal(12, ((IntLangValue)interpreter.Manager.GetValue(new LangId("intPlusEnum"))!).Value);
        Assert.Equal(2.5, ((DoubleLangValue)interpreter.Manager.GetValue(new LangId("doublePlusEnum"))!).Value, 4);
    }

    [Fact]
    public void GenericTypeParameterConstraint_UserClassInheritance_WorksAcrossValidAndInvalidTypes()
    {
        var validCode = @"
class Animal {
    public name:string <- ""a""
}
class Dog extends Animal {
    public breed:string <- ""husky""
}
class Cage<T, U> where T: U {
    public item:T
    func init(v:T) {
        this.item <- v
    }
}
d <- Dog()
c <- Cage<Dog, Animal>(d)
res <- c.item.breed
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(validCode);
        ast.Run(interpreter.Manager);
        Assert.Equal("husky", ((StringLangValue)interpreter.Manager.GetValue(new LangId("res"))!).Value);

        var invalidCode = @"
class Animal {}
class Car {}
class Cage<T, U> where T: U {}
c <- Cage<Car, Animal>()
";
        var badInterpreter = new LangInterpreter();
        var badAst = badInterpreter.Build(invalidCode);
        var ex = Assert.Throws<ArgumentException>(() => badAst.Run(badInterpreter.Manager));
        Assert.Contains("不满足泛型参数", ex.Message);
    }

    [Fact]
    public void LinqChainedIntoContinuation_WorksAcrossAllThreeModes()
    {
        var code = @"
nums <- {1, 2, 3, 4, 5, 6}
res <- from x in nums
       where x > 1
       select x * 2 into doubled
       where doubled >= 6
       select doubled + 1 into shifted
       where shifted < 13
       select shifted
Assert.Equal(3, res.Count())
Assert.Equal(7, res[0])
Assert.Equal(9, res[1])
Assert.Equal(11, res[2])
";
        // 1. 解释模式
        var interp = new LangInterpreter();
        interp.Build(code).Run(interp.Manager);

        // 2. IL 模式
        var ilInterp = new LangInterpreter();
        var compiled = Old8Lang.Compiler.Compiler.Compile(ilInterp.Build(code), "test_linq_chain", ilInterp);
        compiled();

        // 3. VM 模式
        var vmInterp = new LangInterpreter();
        var vmAst = vmInterp.Build(code);
        var bytecode = new BytecodeCompiler().Compile(vmAst);
        var vm = new Old8Lang.Bytecode.VM.VirtualMachine(bytecode);
        vm.Execute();
    }
}
