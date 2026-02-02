using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 类声明编译模式测试
/// 测试编译器模式下的类声明、实例化和方法调用的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class ClassDeclarationTests
{
    [Fact]
    public void Class_BasicDeclaration_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- """"
                public age:int <- 0

                func init(name:string, age:int) -> void {
                    this.name <- name
                    this.age <- age
                }

                func getName() -> string {
                    return this.name
                }
            }

            func test() -> string {
                p <- Person(""Alice"", 30)
                return p.getName()
            }

            Assert.True(test() == ""Alice"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Class_WithMethods_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }

                public func multiply(a:int, b:int) -> int {
                    return a * b
                }
            }

            func test() -> int {
                calc <- Calculator()
                return calc.add(10, 20) + calc.multiply(3, 4)
            }

            Assert.True(test() == 42)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Class_WithPrivateMembers_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            class Counter {
                private count:int <- 0

                func init() -> void {
                    this.count <- 0
                }

                func increment() -> void {
                    this.count <- this.count + 1
                }

                func getCount() -> int {
                    return this.count
                }
            }

            func test() -> int {
                c <- Counter()
                c.increment()
                c.increment()
                return c.getCount()
            }

            Assert.True(test() == 2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Class_StaticMembers_CompilesAndExecutes()
    {
        // Arrange
        var code = @"
            class MathUtil {
                public static pi <- 3.14

                public static func double(x:int) -> int {
                    return x * 2
                }
            }

            func test() -> int {
                return MathUtil.double(21)
            }

            Assert.True(test() == 42)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
