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

    [Fact]
    public void IsInstanceOf_WithInheritance_UsesExactClassMatch()
    {
        // Arrange
        var code = @"
            class Animal { }
            class Dog extends Animal { }
            d <- Dog()
            result <- IsInstanceOf(d, ""Animal"")
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

    #region 文档对齐 API 测试

    [Fact]
    public void Reflection_NewApis_ReturnExpectedShapes()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- ""Alice""
                public func greet() {
                    return ""hi""
                }
            }
            p <- Person()
            typeInfo <- GetTypeInfo(""Person"")
            classInfo <- GetClassInfo(p)
            memberMethod <- GetMemberInfo(p, ""greet"")
            memberField <- GetMemberInfo(p, ""name"")
            funcInfo <- GetFunctionInfo(""PrintLine"")
            hasMemberMethod <- HasMember(p, ""greet"")
            hasMemberField <- HasMember(p, ""name"")
            hasMemberMissing <- HasMember(p, ""missing"")
            allTypes <- GetAllTypes()
            typeOfP <- TypeOf(p)
            personType <- GetType(""Person"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var typeInfo = Assert.IsType<DictionaryLangValue>(interpreter.Manager.GetValue(new LangId("typeInfo")));
        Assert.True(HasDictionaryKey(typeInfo, "name"));
        Assert.True(HasDictionaryKey(typeInfo, "methods"));
        Assert.True(HasDictionaryKey(typeInfo, "fields"));
        Assert.True(HasDictionaryKey(typeInfo, "isGeneric"));

        var classInfo = Assert.IsType<DictionaryLangValue>(interpreter.Manager.GetValue(new LangId("classInfo")));
        Assert.True(HasDictionaryKey(classInfo, "className"));
        Assert.True(HasDictionaryKey(classInfo, "methods"));
        Assert.True(HasDictionaryKey(classInfo, "fields"));

        var memberMethod = Assert.IsType<DictionaryLangValue>(interpreter.Manager.GetValue(new LangId("memberMethod")));
        var memberMethodType = Assert.IsType<StringLangValue>(GetDictionaryValue(memberMethod, "type"));
        Assert.Equal("method", memberMethodType.Value);

        var memberField = Assert.IsType<DictionaryLangValue>(interpreter.Manager.GetValue(new LangId("memberField")));
        var memberFieldType = Assert.IsType<StringLangValue>(GetDictionaryValue(memberField, "type"));
        Assert.Equal("field", memberFieldType.Value);

        var funcInfo = Assert.IsType<DictionaryLangValue>(interpreter.Manager.GetValue(new LangId("funcInfo")));
        Assert.True(HasDictionaryKey(funcInfo, "name"));
        Assert.True(HasDictionaryKey(funcInfo, "type"));
        Assert.True(HasDictionaryKey(funcInfo, "parameters"));

        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("hasMemberMethod"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("hasMemberField"))).Value);
        Assert.False(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("hasMemberMissing"))).Value);

        Assert.NotNull(Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("allTypes"))));
        Assert.NotNull(Assert.IsType<TypeLangValue>(interpreter.Manager.GetValue(new LangId("typeOfP"))));
        Assert.NotNull(Assert.IsType<TypeLangValue>(interpreter.Manager.GetValue(new LangId("personType"))));
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

            // 获取类信息
            classInfo <- GetClassInfo(person)

            // 检查方法和字段
            hasGreet <- HasMember(person, ""greet"")
            hasName <- HasMember(person, ""name"")

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
        var classInfo = interpreter.Manager.GetValue(new LangId("classInfo")) as DictionaryLangValue;
        var hasGreet = interpreter.Manager.GetValue(new LangId("hasGreet")) as BoolLangValue;
        var hasName = interpreter.Manager.GetValue(new LangId("hasName")) as BoolLangValue;
        var greeting = interpreter.Manager.GetValue(new LangId("greeting")) as StringLangValue;
        var privateAge = interpreter.Manager.GetValue(new LangId("privateAge")) as IntLangValue;
        var nameValue = interpreter.Manager.GetValue(new LangId("nameValue")) as StringLangValue;
        var newName = interpreter.Manager.GetValue(new LangId("newName")) as StringLangValue;
        var isPerson = interpreter.Manager.GetValue(new LangId("isPerson")) as BoolLangValue;

        Assert.NotNull(classInfo);
        var className = Assert.IsType<StringLangValue>(GetDictionaryValue(classInfo, "className"));
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

    private static bool HasDictionaryKey(DictionaryLangValue dict, string key)
    {
        foreach (var tuple in dict.Tuples)
        {
            if (tuple.Elements.Count < 2)
            {
                continue;
            }

            if (tuple.Elements[0] is StringLangValue keyValue && keyValue.Value == key)
            {
                return true;
            }
        }

        return false;
    }

    private static LangValueType GetDictionaryValue(DictionaryLangValue dict, string key)
    {
        foreach (var tuple in dict.Tuples)
        {
            if (tuple.Elements.Count < 2)
            {
                continue;
            }

            if (tuple.Elements[0] is StringLangValue keyValue && keyValue.Value == key)
            {
                return (LangValueType)tuple.Elements[1];
            }
        }

        throw new Xunit.Sdk.XunitException($"找不到字典键: {key}");
    }
}
