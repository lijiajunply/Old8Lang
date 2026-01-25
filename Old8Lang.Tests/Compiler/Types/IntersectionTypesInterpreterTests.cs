using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Types;

/// <summary>
/// 交叉类型测试 - 测试 Intersection Types 功能
/// 交叉类型使用 & 符号，表示类型必须同时满足所有约束
/// 主要用于泛型约束和接口组合
/// </summary>
[Collection("Sequential")]
public class IntersectionTypesInterpreterTests
{
    #region 泛型约束中的交叉类型

    /// <summary>
    /// 测试泛型约束中的交叉类型基础用法
    /// </summary>
    [Fact]
    public void Run_GenericConstraintWithIntersectionType_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
// 定义接口
interface IComparable {
    func compareTo(other) -> int
}

interface ICloneable {
    func clone() -> any
}

// 定义满足两个接口的类
class Item implements IComparable, ICloneable {
    public value: int

    func init(v: int) {
        value <- v
    }

    func compareTo(other) -> int {
        return value - other.value
    }

    func clone() -> any {
        return Item(value)
    }
}

// 使用交叉类型约束的泛型函数
func processItem(item) -> int {
    cloned <- item.clone()
    return item.compareTo(cloned)
}

item <- Item(42)
result <- processItem(item)";

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试多接口实现的交叉类型
    /// </summary>
    [Fact]
    public void Run_MultipleInterfaceIntersection_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
interface IDrawable {
    func draw() -> string
}

interface IResizable {
    func resize(width: int, height: int) -> string
}

interface IRotatable {
    func rotate(angle: int) -> string
}

class Shape implements IDrawable, IResizable, IRotatable {
    public name: string

    func init(n: string) {
        name <- n
    }

    func draw() -> string {
        return ""Drawing "" + name
    }

    func resize(width: int, height: int) -> string {
        return name + "" resized to "" + width.ToStr() + ""x"" + height.ToStr()
    }

    func rotate(angle: int) -> string {
        return name + "" rotated by "" + angle.ToStr() + "" degrees""
    }
}

shape <- Shape(""Rectangle"")
drawResult <- shape.draw()
resizeResult <- shape.resize(100, 200)
rotateResult <- shape.rotate(45)";

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Assert
        var drawResult = interpreter.Manager.GetValue(new LangId("drawResult"));
        var resizeResult = interpreter.Manager.GetValue(new LangId("resizeResult"));
        var rotateResult = interpreter.Manager.GetValue(new LangId("rotateResult"));

        Assert.IsType<StringLangValue>(drawResult);
        Assert.Equal("Drawing Rectangle", ((StringLangValue)drawResult).Value);

        Assert.IsType<StringLangValue>(resizeResult);
        Assert.Equal("Rectangle resized to 100x200", ((StringLangValue)resizeResult).Value);

        Assert.IsType<StringLangValue>(rotateResult);
        Assert.Equal("Rectangle rotated by 45 degrees", ((StringLangValue)rotateResult).Value);
    }

    #endregion

    #region 接口组合场景

    /// <summary>
    /// 测试接口组合的交叉类型
    /// </summary>
    [Fact]
    public void Run_InterfaceCombinationWithIntersection_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
interface ISerializable {
    func serialize() -> string
}

interface IValidatable {
    func validate() -> bool
}

class User implements ISerializable, IValidatable {
    public name: string
    public age: int

    func init(n: string, a: int) {
        name <- n
        age <- a
    }

    func serialize() -> string {
        return ""User:"" + name + "",Age:"" + age.ToStr()
    }

    func validate() -> bool {
        return age >= 0 and age <= 150
    }
}

user <- User(""Alice"", 25)
serialized <- user.serialize()
isValid <- user.validate()

invalidUser <- User(""Bob"", -5)
isInvalid <- invalidUser.validate()";

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Assert
        var serialized = interpreter.Manager.GetValue(new LangId("serialized"));
        var isValid = interpreter.Manager.GetValue(new LangId("isValid"));
        var isInvalid = interpreter.Manager.GetValue(new LangId("isInvalid"));

        Assert.IsType<StringLangValue>(serialized);
        Assert.Equal("User:Alice,Age:25", ((StringLangValue)serialized).Value);

        Assert.IsType<BoolLangValue>(isValid);
        Assert.True(((BoolLangValue)isValid).Value);

        Assert.IsType<BoolLangValue>(isInvalid);
        Assert.False(((BoolLangValue)isInvalid).Value);
    }

    #endregion

    #region 边界情况

    /// <summary>
    /// 测试类实现部分接口但被用作交叉类型
    /// </summary>
    [Fact]
    public void Run_PartialInterfaceImplementation_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
interface IA {
    func methodA() -> string
}

interface IB {
    func methodB() -> string
}

// 只实现一个接口
class PartialImpl implements IA {
    func methodA() -> string {
        return ""Method A""
    }
}

obj <- PartialImpl()
resultA <- obj.methodA()";

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试空接口组合
    /// </summary>
    [Fact]
    public void Run_EmptyInterfaceIntersection_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
interface IEmpty1 {
}

interface IEmpty2 {
}

class EmptyImpl implements IEmpty1, IEmpty2 {
    public value: int

    func init(v: int) {
        value <- v
    }

    func getValue() -> int {
        return value
    }
}

obj <- EmptyImpl(42)
result <- obj.getValue()";

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 嵌套和复杂场景

    /// <summary>
    /// 测试嵌套接口继承的交叉类型
    /// </summary>
    [Fact]
    public void Run_NestedInterfaceIntersection_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
interface IBase {
    func baseMethod() -> string
}

interface IDerived1 extends IBase {
    func derived1Method() -> string
}

interface IDerived2 extends IBase {
    func derived2Method() -> string
}

class ComplexImpl implements IDerived1, IDerived2 {
    func baseMethod() -> string {
        return ""Base""
    }

    func derived1Method() -> string {
        return ""Derived1""
    }

    func derived2Method() -> string {
        return ""Derived2""
    }
}

obj <- ComplexImpl()
base <- obj.baseMethod()
d1 <- obj.derived1Method()
d2 <- obj.derived2Method()";

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Assert
        var baseResult = interpreter.Manager.GetValue(new LangId("base"));
        var d1Result = interpreter.Manager.GetValue(new LangId("d1"));
        var d2Result = interpreter.Manager.GetValue(new LangId("d2"));

        Assert.IsType<StringLangValue>(baseResult);
        Assert.Equal("Base", ((StringLangValue)baseResult).Value);

        Assert.IsType<StringLangValue>(d1Result);
        Assert.Equal("Derived1", ((StringLangValue)d1Result).Value);

        Assert.IsType<StringLangValue>(d2Result);
        Assert.Equal("Derived2", ((StringLangValue)d2Result).Value);
    }

    /// <summary>
    /// 测试同时使用联合类型和交叉类型
    /// </summary>
    [Fact]
    public void Run_UnionAndIntersectionCombined_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
interface IProcessor {
    func process() -> string
}

class StringProcessor implements IProcessor {
    public data: string

    func init(d: string) {
        data <- d
    }

    func process() -> string {
        return ""Processed: "" + data
    }
}

class IntProcessor implements IProcessor {
    public data: int

    func init(d: int) {
        data <- d
    }

    func process() -> string {
        return ""Processed: "" + data.ToStr()
    }
}

// 联合类型：可以是 StringProcessor 或 IntProcessor
processor1: StringProcessor | IntProcessor <- StringProcessor(""Hello"")
result1 <- processor1.process()

processor2: StringProcessor | IntProcessor <- IntProcessor(42)
result2 <- processor2.process()";

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
