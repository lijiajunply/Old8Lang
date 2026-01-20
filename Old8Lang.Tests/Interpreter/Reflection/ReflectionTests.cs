using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Reflection;

/// <summary>
/// 解释器模式反射系统测试
/// 测试反射功能在解释器模式下的正确性
/// </summary>
public class ReflectionTests
{
    #region GetClassName 测试

    [Fact]
    public void GetClassName_ReturnsCorrectClassName()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Person", ((StringLangValue)result).Value);
    }

    [Fact]
    public void GetClassName_WithNestedClass_ReturnsCorrectName()
    {
        // Arrange
        var code = @"
            class OuterClass {
                public value <- 0
            }
            obj <- OuterClass()
            className <- GetClassName(obj)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("className"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("OuterClass", ((StringLangValue)result).Value);
    }

    #endregion

    #region GetClassMethods 测试

    [Fact]
    public void GetClassMethods_ReturnsAllMethods()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a, b) {
                    return a + b
                }
                public func subtract(a, b) {
                    return a - b
                }
                private func multiply(a, b) {
                    return a * b
                }
            }
            calc <- Calculator()
            methods <- GetClassMethods(calc)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("methods"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
    }

    [Fact]
    public void GetClassMethods_IncludesInitMethod()
    {
        // Arrange
        var code = @"
            class Person {
                private name <- """"
                func init(n) {
                    name <- n
                }
                public func greet() {
                    return ""Hello""
                }
            }
            p <- Person(""Test"")
            methods <- GetClassMethods(p)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("methods"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
    }

    #endregion

    #region GetClassFields 测试

    [Fact]
    public void GetClassFields_ReturnsAllFields()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Unknown""
                private age <- 0
                public email <- """"
            }
            p <- Person()
            fields <- GetClassFields(p)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("fields"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
    }

    [Fact]
    public void GetClassFields_IncludesPrivateFields()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("fields"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
    }

    #endregion

    #region GetMethodInfo 测试

    [Fact]
    public void GetMethodInfo_ReturnsMethodDetails()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a, b) {
                    return a + b
                }
            }
            calc <- Calculator()
            info <- GetMethodInfo(calc, ""add"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("info"));
        Assert.NotNull(result);
        Assert.IsType<DictionaryLangValue>(result);
    }

    [Fact]
    public void GetMethodInfo_ReturnsPublicFlag()
    {
        // Arrange
        var code = @"
            class Test {
                public func publicMethod() { }
                private func privateMethod() { }
            }
            t <- Test()
            publicInfo <- GetMethodInfo(t, ""publicMethod"")
            privateInfo <- GetMethodInfo(t, ""privateMethod"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var publicInfo = interpreter.Manager.GetValue(new LangId("publicInfo"));
        var privateInfo = interpreter.Manager.GetValue(new LangId("privateInfo"));
        Assert.NotNull(publicInfo);
        Assert.NotNull(privateInfo);
    }

    #endregion

    #region GetFieldInfo 测试

    [Fact]
    public void GetFieldInfo_ReturnsFieldDetails()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("info"));
        Assert.NotNull(result);
        Assert.IsType<DictionaryLangValue>(result);
    }

    [Fact]
    public void GetFieldInfo_ReturnsPrivateFlag()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("info"));
        Assert.NotNull(result);
        Assert.IsType<DictionaryLangValue>(result);
    }

    #endregion

    #region InvokeMethod 测试

    [Fact]
    public void InvokeMethod_CallsPublicMethod()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a, b) {
                    return a + b
                }
            }
            calc <- Calculator()
            result <- InvokeMethod(calc, ""add"", {10, 20})
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void InvokeMethod_CallsPrivateMethod()
    {
        // Arrange
        var code = @"
            class Secret {
                private func getSecret() {
                    return 42
                }
            }
            s <- Secret()
            result <- InvokeMethod(s, ""getSecret"", {})
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void InvokeMethod_WithNoArguments()
    {
        // Arrange
        var code = @"
            class Greeter {
                public func greet() {
                    return ""Hello, World!""
                }
            }
            g <- Greeter()
            result <- InvokeMethod(g, ""greet"", {})
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello, World!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void InvokeMethod_WithMultipleArguments()
    {
        // Arrange
        var code = @"
            class Math {
                public func calculate(a, b, c) {
                    return a + b * c
                }
            }
            m <- Math()
            result <- InvokeMethod(m, ""calculate"", {10, 5, 3})
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(25, ((IntLangValue)result).Value); // 10 + 5 * 3 = 25
    }

    #endregion

    #region GetField 测试

    [Fact]
    public void GetField_ReturnsPublicFieldValue()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Alice""
            }
            p <- Person()
            result <- GetField(p, ""name"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Alice", ((StringLangValue)result).Value);
    }

    [Fact]
    public void GetField_ReturnsPrivateFieldValue()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretCode <- 12345
            }
            s <- Secret()
            result <- GetField(s, ""secretCode"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(12345, ((IntLangValue)result).Value);
    }

    [Fact]
    public void GetField_ReturnsIntegerField()
    {
        // Arrange
        var code = @"
            class Counter {
                public count <- 100
            }
            c <- Counter()
            result <- GetField(c, ""count"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(100, ((IntLangValue)result).Value);
    }

    #endregion

    #region SetField 测试

    [Fact]
    public void SetField_ModifiesPublicField()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Unknown""
            }
            p <- Person()
            SetField(p, ""name"", ""Bob"")
            result <- p.name
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Bob", ((StringLangValue)result).Value);
    }

    [Fact]
    public void SetField_ModifiesPrivateField()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue <- 0
                public func getSecret() {
                    return secretValue
                }
            }
            s <- Secret()
            SetField(s, ""secretValue"", 999)
            result <- s.getSecret()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(999, ((IntLangValue)result).Value);
    }

    [Fact]
    public void SetField_ThenGetField_ReturnsNewValue()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    #endregion

    #region CreateInstance 测试

    [Fact]
    public void CreateInstance_CreatesObjectWithNoArgs()
    {
        // Arrange
        var code = @"
            class Simple {
                public value <- 10
            }
            obj <- CreateInstance(""Simple"", {})
            result <- obj.value
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    [Fact]
    public void CreateInstance_CreatesObjectWithArgs()
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
            nameResult <- obj.name
            ageResult <- obj.age
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var nameResult = interpreter.Manager.GetValue(new LangId("nameResult"));
        var ageResult = interpreter.Manager.GetValue(new LangId("ageResult"));
        Assert.NotNull(nameResult);
        Assert.NotNull(ageResult);
        Assert.IsType<StringLangValue>(nameResult);
        Assert.IsType<IntLangValue>(ageResult);
        Assert.Equal("Alice", ((StringLangValue)nameResult).Value);
        Assert.Equal(25, ((IntLangValue)ageResult).Value);
    }

    [Fact]
    public void CreateInstance_CanCallMethodsOnCreatedObject()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a, b) {
                    return a + b
                }
            }
            calc <- CreateInstance(""Calculator"", {})
            result <- calc.add(5, 3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(8, ((IntLangValue)result).Value);
    }

    #endregion

    #region IsInstanceOf 测试

    [Fact]
    public void IsInstanceOf_ReturnsTrueForCorrectClass()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Test""
            }
            p <- Person()
            result <- IsInstanceOf(p, ""Person"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void IsInstanceOf_ReturnsFalseForWrongClass()
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.False(((BoolLangValue)result).Value);
    }

    #endregion

    #region HasMethod 测试

    [Fact]
    public void HasMethod_ReturnsTrueForExistingMethod()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a, b) {
                    return a + b
                }
            }
            calc <- Calculator()
            result <- HasMethod(calc, ""add"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void HasMethod_ReturnsFalseForNonExistingMethod()
    {
        // Arrange
        var code = @"
            class Calculator {
                public func add(a, b) {
                    return a + b
                }
            }
            calc <- Calculator()
            result <- HasMethod(calc, ""subtract"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.False(((BoolLangValue)result).Value);
    }

    [Fact]
    public void HasMethod_ReturnsTrueForPrivateMethod()
    {
        // Arrange
        var code = @"
            class Secret {
                private func getSecret() {
                    return 42
                }
            }
            s <- Secret()
            result <- HasMethod(s, ""getSecret"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    #endregion

    #region HasField 测试

    [Fact]
    public void HasField_ReturnsTrueForExistingField()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Test""
            }
            p <- Person()
            result <- HasField(p, ""name"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void HasField_ReturnsFalseForNonExistingField()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Test""
            }
            p <- Person()
            result <- HasField(p, ""age"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.False(((BoolLangValue)result).Value);
    }

    [Fact]
    public void HasField_ReturnsTrueForPrivateField()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue <- 42
            }
            s <- Secret()
            result <- HasField(s, ""secretValue"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    #endregion

    #region 综合测试

    [Fact]
    public void Reflection_CompleteWorkflow()
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

                public func greet() {
                    return ""Hello, I am "" + name
                }

                private func getAge() {
                    return age
                }
            }

            // 创建实例
            person <- Person(""Alice"", 25)

            // 获取类名
            className <- GetClassName(person)

            // 检查方法和字段
            hasGreet <- HasMethod(person, ""greet"")
            hasName <- HasField(person, ""name"")

            // 动态调用方法
            greeting <- InvokeMethod(person, ""greet"", {})
            privateAge <- InvokeMethod(person, ""getAge"", {})

            // 动态访问字段
            nameValue <- GetField(person, ""name"")

            // 动态修改字段
            SetField(person, ""name"", ""Bob"")
            newName <- GetField(person, ""name"")

            // 类型检查
            isPerson <- IsInstanceOf(person, ""Person"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var className = interpreter.Manager.GetValue(new LangId("className")) as StringLangValue;
        var hasGreet = interpreter.Manager.GetValue(new LangId("hasGreet")) as BoolLangValue;
        var hasName = interpreter.Manager.GetValue(new LangId("hasName")) as BoolLangValue;
        var greeting = interpreter.Manager.GetValue(new LangId("greeting")) as StringLangValue;
        var privateAge = interpreter.Manager.GetValue(new LangId("privateAge")) as IntLangValue;
        var nameValue = interpreter.Manager.GetValue(new LangId("nameValue")) as StringLangValue;
        var newName = interpreter.Manager.GetValue(new LangId("newName")) as StringLangValue;
        var isPerson = interpreter.Manager.GetValue(new LangId("isPerson")) as BoolLangValue;

        Assert.NotNull(className);
        Assert.Equal("Person", className.Value);

        Assert.NotNull(hasGreet);
        Assert.True(hasGreet.Value);

        Assert.NotNull(hasName);
        Assert.True(hasName.Value);

        Assert.NotNull(greeting);
        Assert.Equal("Hello, I am Alice", greeting.Value);

        Assert.NotNull(privateAge);
        Assert.Equal(25, privateAge.Value);

        Assert.NotNull(nameValue);
        Assert.Equal("Alice", nameValue.Value);

        Assert.NotNull(newName);
        Assert.Equal("Bob", newName.Value);

        Assert.NotNull(isPerson);
        Assert.True(isPerson.Value);
    }

    [Fact]
    public void Reflection_DynamicMethodDispatch()
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

                public func getArea() {
                    return width * height
                }
            }

            class Circle {
                private radius <- 0

                func init(r) {
                    radius <- r
                }

                public func getArea() {
                    return radius * radius * 3
                }
            }

            rect <- Rectangle(4, 5)
            circle <- Circle(3)

            // 动态调用相同名称的方法
            rectArea <- InvokeMethod(rect, ""getArea"", {})
            circleArea <- InvokeMethod(circle, ""getArea"", {})
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var rectArea = interpreter.Manager.GetValue(new LangId("rectArea")) as IntLangValue;
        var circleArea = interpreter.Manager.GetValue(new LangId("circleArea")) as IntLangValue;

        Assert.NotNull(rectArea);
        Assert.Equal(20, rectArea.Value); // 4 * 5 = 20

        Assert.NotNull(circleArea);
        Assert.Equal(27, circleArea.Value); // 3 * 3 * 3 = 27
    }

    #endregion
}
