using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// Super 表达式测试 - 覆盖 super 调用父类方法、访问父类字段、父类构造调用、多层链式调用与错误处理
/// </summary>
[Collection("Sequential")]
public class SuperExpressionTests
{
    [Fact]
    public void Super_CallOverriddenParentMethod_InvokesParentImplementation()
    {
        var code = @"
class Animal {
    public name:string <- """"
    func init(name:string) {
        this.name <- name
    }
    func speak() -> string {
        return this.name + "" makes a sound""
    }
}

class Dog extends Animal {
    func init(name:string) {
        super.init(name)
    }
    func speak() -> string {
        return super.speak() + "" (bark)""
    }
}

dog <- Dog(""Rex"")
result <- dog.speak()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal("Rex makes a sound (bark)",
            Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("result"))).Value);
    }

    [Fact]
    public void Super_CallParentMethodWithArgumentsAndOverloads_SelectsMatchingSignature()
    {
        var code = @"
class Calculator {
    func compute(x:int) -> int {
        return x * 2
    }
    func compute(x:int, y:int) -> int {
        return x + y
    }
}

class BonusCalculator extends Calculator {
    func compute(x:int) -> int {
        return super.compute(x) + 1
    }
    func computeWithTwo(x:int, y:int) -> int {
        return super.compute(x, y) * 10
    }
}

calc <- BonusCalculator()
r1 <- calc.compute(5)
r2 <- calc.computeWithTwo(3, 4)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(11, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("r1"))).Value);
        Assert.Equal(70, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("r2"))).Value);
    }

    [Fact]
    public void Super_AccessParentField_ReadsParentDefinedField()
    {
        var code = @"
class BaseConfig {
    public baseValue:int <- 42
    protected tag:string <- ""base-tag""
}

class DerivedConfig extends BaseConfig {
    public extra:int <- 8
    func readParentFields() -> string {
        return super.tag + "":"" + super.baseValue.ToStr()
    }
    func total() -> int {
        return super.baseValue + this.extra
    }
}

cfg <- DerivedConfig()
desc <- cfg.readParentFields()
sum <- cfg.total()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal("base-tag:42", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("desc"))).Value);
        Assert.Equal(50, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("sum"))).Value);
    }

    [Fact]
    public void Super_CallParentConstructorByClassNameAndWhenNoParentInit_WorksCorrectly()
    {
        var code = @"
class Person {
    public name:string <- """"
    func init(name:string) {
        this.name <- name
    }
}

class Student extends Person {
    public grade:int <- 0
    func init(name:string, grade:int) {
        super.Person(name)
        this.grade <- grade
    }
}

class EmptyParent {
    public marker:int <- 7
}

class ChildOfEmpty extends EmptyParent {
    func init() {
        super.init()
    }
}

s <- Student(""Lin"", 10)
sname <- s.name
sgrade <- s.grade
c <- ChildOfEmpty()
cmarker <- c.marker
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal("Lin", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("sname"))).Value);
        Assert.Equal(10, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("sgrade"))).Value);
        Assert.Equal(7, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("cmarker"))).Value);
    }

    [Fact]
    public void Super_ChainedCallsOnMethodResultAndInheritedGrandParentMethod_WorkCorrectly()
    {
        var code = @"
class Level1 {
    public label:string <- """"
    func baseLabel() -> string {
        return ""L1:"" + this.label
    }
    func describe() -> string {
        return ""L1:"" + this.label
    }
}

class Level2 extends Level1 {
    func init(label:string) {
        this.label <- label
    }
    func middleTag() -> string {
        return this.baseLabel() + ""->L2""
    }
}

class Level3 extends Level2 {
    func init(label:string) {
        super.init(label)
    }
    func describeUpper() -> string {
        return super.middleTag().ToUpper() + ""->L3""
    }
    func baseUpper() -> string {
        return super.describe().ToUpper()
    }
}

obj <- Level3(""root"")
chained <- obj.describeUpper()
inherited <- obj.baseUpper()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal("L1:ROOT->L2->L3",
            Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("chained"))).Value);
        Assert.Equal("L1:ROOT",
            Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("inherited"))).Value);
    }

    [Fact]
    public void Super_InClassWithoutParent_ThrowsInvalidOperationError()
    {
        var code = @"
class Standalone {
    func test() -> void {
        super.init()
    }
}

s <- Standalone()
s.test()
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<InvalidOperationError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void Super_AccessNonExistentParentMember_ThrowsAttributeError()
    {
        var code = @"
class Parent {
    public x:int <- 1
}

class Child extends Parent {
    func badMethod() -> void {
        super.noSuchMethod()
    }
}

c <- Child()
c.badMethod()
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.Throws<AttributeError>(() => ast.Run(interpreter.Manager));
    }
}
