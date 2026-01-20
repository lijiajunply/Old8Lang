using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Reflection;

/// <summary>
/// 编译器模式反射系统测试
/// 测试反射功能在编译器模式下的 IL 生成和执行
/// 注意：编译模式要求函数参数和返回类型有类型注解
/// </summary>
[Collection("Sequential")]
public class ReflectionTests
{
    #region GetClassName 测试

    [Fact]
    public void GetClassName_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""test""
            }
            p <- Person()
            result <- GetClassName(p)
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
    public void GetClassName_WithDifferentClasses_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            class Animal {
                public species <- ""Unknown""
            }
            class Vehicle {
                public brand <- ""Unknown""
            }
            a <- Animal()
            v <- Vehicle()
            animalClass <- GetClassName(a)
            vehicleClass <- GetClassName(v)
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

    #endregion

    #region GetClassMethods 测试

    [Fact]
    public void GetClassMethods_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }
                public func subtract(a:int, b:int) -> int {
                    return a - b
                }
            }
            calc <- Calculator()
            methods <- GetClassMethods(calc)
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
    public void GetClassMethods_WithPrivateMethods_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            class Secret {
                public func publicMethod() -> void { }
                private func privateMethod() -> void { }
            }
            s <- Secret()
            methods <- GetClassMethods(s)
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

    #endregion

    #region GetClassFields 测试

    [Fact]
    public void GetClassFields_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Unknown""
                public age <- 0
            }
            p <- Person()
            fields <- GetClassFields(p)
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
    public void GetClassFields_WithPrivateFields_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue <- 42
                public publicValue <- 100
            }
            s <- Secret()
            fields <- GetClassFields(s)
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

    #endregion

    #region GetMethodInfo 测试

    [Fact]
    public void GetMethodInfo_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }
            }
            calc <- Calculator()
            info <- GetMethodInfo(calc, ""add"")
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
    public void GetMethodInfo_ForPrivateMethod_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            class Secret {
                private func getSecret() -> int {
                    return 42
                }
            }
            s <- Secret()
            info <- GetMethodInfo(s, ""getSecret"")
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

    #endregion

    #region GetFieldInfo 测试

    [Fact]
    public void GetFieldInfo_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Test""
            }
            p <- Person()
            info <- GetFieldInfo(p, ""name"")
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
    public void GetFieldInfo_ForPrivateField_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue <- 42
            }
            s <- Secret()
            info <- GetFieldInfo(s, ""secretValue"")
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

    #endregion

    #region InvokeMethod 测试

    [Fact]
    public void InvokeMethod_PublicMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }
            }
            calc <- Calculator()
            result <- InvokeMethod(calc, ""add"", {10, 20})
            PrintLine(result.ToStr())
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
    public void InvokeMethod_PrivateMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Secret {
                private func getSecret() -> int {
                    return 42
                }
            }
            s <- Secret()
            result <- InvokeMethod(s, ""getSecret"", {})
            PrintLine(result.ToStr())
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
    public void InvokeMethod_WithNoArguments_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            class Greeter {
                public func greet() -> string {
                    return ""Hello, World!""
                }
            }
            g <- Greeter()
            result <- InvokeMethod(g, ""greet"", {})
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
    public void InvokeMethod_WithMultipleArguments_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            class Math {
                public func calculate(a:int, b:int, c:int) -> int {
                    return a + b * c
                }
            }
            m <- Math()
            result <- InvokeMethod(m, ""calculate"", {10, 5, 3})
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

    #endregion

    #region GetField 测试

    [Fact]
    public void GetField_PublicField_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Alice""
            }
            p <- Person()
            result <- GetField(p, ""name"")
            PrintLine(result.ToStr())
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
    public void GetField_PrivateField_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretCode <- 12345
            }
            s <- Secret()
            result <- GetField(s, ""secretCode"")
            PrintLine(result.ToStr())
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

    #endregion

    #region SetField 测试

    [Fact]
    public void SetField_PublicField_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Unknown""
            }
            p <- Person()
            SetField(p, ""name"", ""Bob"")
            PrintLine(p.name)
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
    public void SetField_PrivateField_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue <- 0
                public func getSecret() -> int {
                    return secretValue
                }
            }
            s <- Secret()
            SetField(s, ""secretValue"", 999)
            result <- s.getSecret()
            PrintLine(result.ToStr())
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
    public void SetField_ThenGetField_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            class Data {
                public value <- 0
            }
            d <- Data()
            SetField(d, ""value"", 42)
            result <- GetField(d, ""value"")
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

    #endregion

    #region CreateInstance 测试

    [Fact]
    public void CreateInstance_WithNoArgs_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Simple {
                public value <- 10
            }
            obj <- CreateInstance(""Simple"", {})
            PrintLine(obj.value.ToStr())
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
    public void CreateInstance_WithArgs_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- """"
                public age <- 0
                func init(n, a) {
                    name <- n
                    age <- a
                }
            }
            obj <- CreateInstance(""Person"", {""Alice"", 25})
            PrintLine(obj.name)
            PrintLine(obj.age.ToStr())
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
    public void CreateInstance_CanCallMethods_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }
            }
            calc <- CreateInstance(""Calculator"", {})
            result <- calc.add(5, 3)
            PrintLine(result.ToStr())
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

    #endregion

    #region IsInstanceOf 测试

    [Fact]
    public void IsInstanceOf_CorrectClass_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Test""
            }
            p <- Person()
            result <- IsInstanceOf(p, ""Person"")
            PrintLine(result.ToStr())
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
    public void IsInstanceOf_WrongClass_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Test""
            }
            class Animal {
                public species <- ""Unknown""
            }
            p <- Person()
            result <- IsInstanceOf(p, ""Animal"")
            PrintLine(result.ToStr())
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

    #endregion

    #region HasMethod 测试

    [Fact]
    public void HasMethod_ExistingMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }
            }
            calc <- Calculator()
            result <- HasMethod(calc, ""add"")
            PrintLine(result.ToStr())
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
    public void HasMethod_NonExistingMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a:int, b:int) -> int {
                    return a + b
                }
            }
            calc <- Calculator()
            result <- HasMethod(calc, ""subtract"")
            PrintLine(result.ToStr())
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
    public void HasMethod_PrivateMethod_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Secret {
                private func getSecret() -> int {
                    return 42
                }
            }
            s <- Secret()
            result <- HasMethod(s, ""getSecret"")
            PrintLine(result.ToStr())
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

    #endregion

    #region HasField 测试

    [Fact]
    public void HasField_ExistingField_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Test""
            }
            p <- Person()
            result <- HasField(p, ""name"")
            PrintLine(result.ToStr())
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
    public void HasField_NonExistingField_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Test""
            }
            p <- Person()
            result <- HasField(p, ""age"")
            PrintLine(result.ToStr())
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
    public void HasField_PrivateField_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue <- 42
            }
            s <- Secret()
            result <- HasField(s, ""secretValue"")
            PrintLine(result.ToStr())
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

    #endregion

    #region 综合测试

    [Fact]
    public void Reflection_CompleteWorkflow_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试完整的反射工作流
        var code = @"
            class Person {
                private name <- ""Unknown""
                private age <- 0

                func init(n, a) {
                    name <- n
                    age <- a
                }

                public func greet() -> string {
                    return ""Hello, I am "" + name
                }

                private func getAge() -> int {
                    return age
                }
            }

            // 创建实例
            person <- Person(""Alice"", 25)

            // 获取类名
            className <- GetClassName(person)
            PrintLine(""Class: "" + className)

            // 检查方法和字段
            hasGreet <- HasMethod(person, ""greet"")
            hasName <- HasField(person, ""name"")
            PrintLine(""Has greet: "" + hasGreet.ToStr())
            PrintLine(""Has name: "" + hasName.ToStr())

            // 动态调用方法
            greeting <- InvokeMethod(person, ""greet"", {})
            PrintLine(""Greeting: "" + greeting.ToStr())

            // 动态访问字段
            nameValue <- GetField(person, ""name"")
            PrintLine(""Name: "" + nameValue.ToStr())

            // 动态修改字段
            SetField(person, ""name"", ""Bob"")
            newName <- GetField(person, ""name"")
            PrintLine(""New name: "" + newName.ToStr())

            // 类型检查
            isPerson <- IsInstanceOf(person, ""Person"")
            PrintLine(""Is Person: "" + isPerson.ToStr())
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
    public void Reflection_DynamicMethodDispatch_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试动态方法分发
        var code = @"
            class Rectangle {
                private width <- 0
                private height <- 0

                func init(w, h) {
                    width <- w
                    height <- h
                }

                public func getArea() -> int {
                    return width * height
                }
            }

            class Circle {
                private radius <- 0

                func init(r) {
                    radius <- r
                }

                public func getArea() -> int {
                    return radius * radius * 3
                }
            }

            rect <- Rectangle(4, 5)
            circle <- Circle(3)

            // 动态调用相同名称的方法
            rectArea <- InvokeMethod(rect, ""getArea"", {})
            circleArea <- InvokeMethod(circle, ""getArea"", {})

            PrintLine(""Rectangle area: "" + rectArea.ToStr())
            PrintLine(""Circle area: "" + circleArea.ToStr())
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
    public void Reflection_WithStaticMembers_CompilesCorrectly()
    {
        // Arrange - 测试静态成员的反射
        var code = @"
            class Counter {
                public static count <- 0

                public static func increment() -> void {
                    count <- count + 1
                }

                public static func getCount() -> int {
                    return count
                }
            }

            c <- Counter()
            methods <- GetClassMethods(c)
            fields <- GetClassFields(c)
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

    #endregion
}
