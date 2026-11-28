using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Types;

/// <summary>
/// TypeTemplate 与泛型系统测试 - 覆盖泛型类型参数、new()/class/struct/接口/基类/类型参数约束、类型参数推断与嵌套泛型类型
/// </summary>
[Collection("Sequential")]
public class TypeTemplateTests
{
    #region TypeTemplate 元数据与泛型类型参数

    [Fact]
    public void TypeTemplate_GenericMetadataAndMultipleTypeParameters_InstantiatesCorrectly()
    {
        var code = @"
class Pair<TKey, TValue> {
    public key:TKey
    public val:TValue

    func init(k:TKey, v:TValue) {
        this.key <- k
        this.val <- v
    }

    func getKey() -> TKey {
        return this.key
    }

    func getVal() -> TValue {
        return this.val
    }
}

p <- Pair<string, int>(""age"", 28)
k <- p.getKey()
v <- p.getVal()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        var template = Assert.IsType<TypeTemplate>(interpreter.Manager.GetAny(new LangId("Pair")));
        Assert.True(template.IsGeneric);
        Assert.NotNull(template.GenericParameters);
        Assert.Equal(2, template.GenericParameters.Count);
        Assert.Equal("TKey", template.GenericParameters[0].Name);
        Assert.Equal("TValue", template.GenericParameters[1].Name);

        Assert.Equal("age", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("k"))).Value);
        Assert.Equal(28, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v"))).Value);
    }

    [Fact]
    public void TypeTemplate_GenericInheritance_PreservesParentTypeParameter()
    {
        var code = @"
class BaseBox<T> {
    public value:T
    func init(v:T) {
        this.value <- v
    }
    func get() -> T {
        return this.value
    }
}

class LabeledBox<T> extends BaseBox<T> {
    public label:string <- """"
    func init(v:T, label:string) {
        super.init(v)
        this.label <- label
    }
}

lb <- LabeledBox<int>(99, ""score"")
val <- lb.get()
lbl <- lb.label
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(99, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("val"))).Value);
        Assert.Equal("score", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("lbl"))).Value);
    }

    #endregion

    #region 泛型类型约束

    [Fact]
    public void TypeTemplate_NewConstraint_AcceptsDefaultConstructibleAndRejectsMissingNoArgInit()
    {
        var validCode = @"
class DefaultItem {
    public code:int <- 7
    func init() {
        this.code <- 7
    }
}

class Factory<T> where T: new() {
    public tag:string <- ""ok""
}

f <- Factory<DefaultItem>()
res <- f.tag
";
        var interp1 = new LangInterpreter();
        interp1.Build(validCode).Run(interp1.Manager);
        Assert.Equal("ok", Assert.IsType<StringLangValue>(interp1.Manager.GetValue(new LangId("res"))).Value);

        var invalidCode = @"
class NeedsArg {
    public x:int <- 0
    func init(x:int) {
        this.x <- x
    }
}

class Factory<T> where T: new() {
    public tag:string <- ""ok""
}

f <- Factory<NeedsArg>()
";
        var interp2 = new LangInterpreter();
        var ast = interp2.Build(invalidCode);
        Assert.Throws<ArgumentException>(() => ast.Run(interp2.Manager));
    }

    [Fact]
    public void TypeTemplate_ClassAndStructConstraints_EnforceReferenceAndValueTypes()
    {
        var validCode = @"
class User {
    public name:string <- """"
    func init(n:string) {
        this.name <- n
    }
}

class RefHolder<T> where T: class {
    public item:T
    func init(i:T) {
        this.item <- i
    }
}

class ValHolder<T> where T: struct {
    public item:T
    func init(i:T) {
        this.item <- i
    }
}

rh <- RefHolder<User>(User(""Alice""))
vh <- ValHolder<int>(123)
userName <- rh.item.name
numVal <- vh.item
";
        var interp = new LangInterpreter();
        interp.Build(validCode).Run(interp.Manager);
        Assert.Equal("Alice", Assert.IsType<StringLangValue>(interp.Manager.GetValue(new LangId("userName"))).Value);
        Assert.Equal(123, Assert.IsType<IntLangValue>(interp.Manager.GetValue(new LangId("numVal"))).Value);

        // 值类型违反 class 约束
        var classViolation = @"
class RefOnly<T> where T: class {
    public item:T
}
x <- RefOnly<int>()
";
        var interpClassErr = new LangInterpreter();
        Assert.Throws<ArgumentException>(() => interpClassErr.Build(classViolation).Run(interpClassErr.Manager));

        // 引用类型违反 struct 约束
        var structViolation = @"
class User {
    public name:string <- """"
}
class ValOnly<T> where T: struct {
    public item:T
}
x <- ValOnly<User>()
";
        var interpStructErr = new LangInterpreter();
        Assert.Throws<ArgumentException>(() => interpStructErr.Build(structViolation).Run(interpStructErr.Manager));
    }

    [Fact]
    public void TypeTemplate_InterfaceAndBaseClassConstraints_ValidateHierarchy()
    {
        var validCode = @"
interface INamed {
    func getName() -> string
}

class Animal {
    public kind:string <- ""animal""
}

class Cat extends Animal implements INamed {
    public name:string <- """"
    func init(n:string) {
        this.name <- n
    }
    func getName() -> string {
        return this.name
    }
}

class Shelter<T> where T: Animal & INamed {
    public pet:T
    func init(p:T) {
        this.pet <- p
    }
    func petName() -> string {
        return this.pet.getName()
    }
}

s <- Shelter<Cat>(Cat(""Mimi""))
name <- s.petName()
";
        var interp = new LangInterpreter();
        interp.Build(validCode).Run(interp.Manager);
        Assert.Equal("Mimi", Assert.IsType<StringLangValue>(interp.Manager.GetValue(new LangId("name"))).Value);

        // 未实现接口的类违反约束
        var invalidCode = @"
interface INamed {
    func getName() -> string
}
class Plain {
    public x:int <- 1
}
class NamedBox<T> where T: INamed {
    public item:T
}
b <- NamedBox<Plain>()
";
        var interpErr = new LangInterpreter();
        Assert.Throws<ArgumentException>(() => interpErr.Build(invalidCode).Run(interpErr.Manager));
    }

    [Fact]
    public void TypeTemplate_TypeParameterConstraint_ValidatesCompatibleAndRejectsIncompatibleTypes()
    {
        var validCode = @"
class Item {
    public id:int <- 7
}

class Mapper<T, U> where T: U {
    public source:T
    func init(s:T) {
        this.source <- s
    }
}

m <- Mapper<Item, Item>(Item())
res <- m.source.id
";
        var interp = new LangInterpreter();
        interp.Build(validCode).Run(interp.Manager);
        Assert.Equal(7, Assert.IsType<IntLangValue>(interp.Manager.GetValue(new LangId("res"))).Value);

        var invalidCode = @"
class A {}
class B {}
class Mapper<T, U> where T: U {
    public source:T
}
m <- Mapper<A, B>()
";
        var interpErr = new LangInterpreter();
        Assert.Throws<ArgumentException>(() => interpErr.Build(invalidCode).Run(interpErr.Manager));
    }

    #endregion

    #region 类型参数推断与嵌套泛型类型

    [Fact]
    public void TypeTemplate_TypeArgumentInference_InfersForGenericClassAndFunction()
    {
        var code = @"
class Wrapper<T> {
    public value:T
    func init(v:T) {
        this.value <- v
    }
    func unwrap() -> T {
        return this.value
    }
}

func identity<T>(x:T) -> T {
    return x
}

wInt <- Wrapper(42)
wStr <- Wrapper(""hello"")
v1 <- wInt.unwrap()
v2 <- wStr.unwrap()
v3 <- identity(3.5)
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(42, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("v1"))).Value);
        Assert.Equal("hello", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("v2"))).Value);
        Assert.Equal(3.5, Assert.IsType<DoubleLangValue>(interpreter.Manager.GetValue(new LangId("v3"))).Value, 0.0001);
    }

    [Fact]
    public void TypeTemplate_NestedGenericTypes_InstantiatesAndAccessesDeepValues()
    {
        var code = @"
class Box<T> {
    public value:T
    func init(v:T) {
        this.value <- v
    }
    func get() -> T {
        return this.value
    }
}

class Pair<A, B> {
    public first:A
    public second:B
    func init(a:A, b:B) {
        this.first <- a
        this.second <- b
    }
}

inner <- Box<int>(77)
outer <- Box<Box<int>>(inner)
deepVal <- outer.get().get()

pairOfBoxes <- Pair<Box<int>, Box<string>>(Box<int>(10), Box<string>(""ten""))
pFirst <- pairOfBoxes.first.get()
pSecond <- pairOfBoxes.second.get()

listBox <- Box<list<int>>({1, 2, 3})
listCount <- listBox.get().Count()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(77, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("deepVal"))).Value);
        Assert.Equal(10, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("pFirst"))).Value);
        Assert.Equal("ten", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("pSecond"))).Value);
        Assert.Equal(3, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("listCount"))).Value);
    }

    #endregion
}
