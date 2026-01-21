using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Reflection;

/// <summary>
/// 虚拟机模式反射系统测试
/// 测试反射功能在虚拟机模式下的字节码编译和执行
/// </summary>
[Collection("Sequential")]
public class VMReflectionTests
{
    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 捕获控制台输出
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // 执行字节码
            var vm = new Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region GetClassName 测试

    [Fact]
    public void GetClassName_ReturnsCorrectClassName()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- ""test""
            }
            p <- Person()
            result <- GetClassName(p)
            PrintLine(result)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Person", output);
    }

    [Fact]
    public void GetClassName_WithDifferentClasses_ReturnsCorrectNames()
    {
        // Arrange
        var code = @"
            class Animal {
                public species:string <- ""Unknown""
            }
            class Vehicle {
                public brand:string <- ""Unknown""
            }
            a <- Animal()
            v <- Vehicle()
            PrintLine(GetClassName(a))
            PrintLine(GetClassName(v))
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Animal", lines[0]);
        Assert.Equal("Vehicle", lines[1]);
    }

    #endregion

    #region GetClassMethods 测试

    [Fact]
    public void GetClassMethods_ReturnsMethodList()
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
            PrintLine(methods.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotEmpty(output);
        Assert.Contains("add", output);
        Assert.Contains("subtract", output);
    }

    [Fact]
    public void GetClassMethods_IncludesPrivateMethods()
    {
        // Arrange
        var code = @"
            class Secret {
                public func publicMethod() -> void { }
                private func privateMethod() -> void { }
            }
            s <- Secret()
            methods <- GetClassMethods(s)
            PrintLine(methods.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotEmpty(output);
        Assert.Contains("publicMethod", output);
        Assert.Contains("privateMethod", output);
    }

    #endregion

    #region GetClassFields 测试

    [Fact]
    public void GetClassFields_ReturnsFieldList()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- ""Unknown""
                public age:int <- 0
            }
            p <- Person()
            fields <- GetClassFields(p)
            PrintLine(fields.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotEmpty(output);
        Assert.Contains("name", output);
        Assert.Contains("age", output);
    }

    [Fact]
    public void GetClassFields_IncludesPrivateFields()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue:int <- 42
                public publicValue:int <- 100
            }
            s <- Secret()
            fields <- GetClassFields(s)
            PrintLine(fields.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotEmpty(output);
        Assert.Contains("secretValue", output);
        Assert.Contains("publicValue", output);
    }

    #endregion

    #region GetMethodInfo 测试

    [Fact]
    public void GetMethodInfo_ReturnsMethodDetails()
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
            PrintLine(info.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotEmpty(output);
        Assert.Contains("add", output);
    }

    [Fact]
    public void GetMethodInfo_ForPrivateMethod_ReturnsDetails()
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
            PrintLine(info.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotEmpty(output);
        Assert.Contains("getSecret", output);
    }

    #endregion

    #region GetFieldInfo 测试

    [Fact]
    public void GetFieldInfo_ReturnsFieldDetails()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- ""Test""
            }
            p <- Person()
            info <- GetFieldInfo(p, ""name"")
            PrintLine(info.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotEmpty(output);
        Assert.Contains("name", output);
    }

    [Fact]
    public void GetFieldInfo_ForPrivateField_ReturnsDetails()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue:int <- 42
            }
            s <- Secret()
            info <- GetFieldInfo(s, ""secretValue"")
            PrintLine(info.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.NotEmpty(output);
        Assert.Contains("secretValue", output);
    }

    #endregion

    #region InvokeMethod 测试

    [Fact]
    public void InvokeMethod_CallsPublicMethod()
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

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("30", output);
    }

    [Fact]
    public void InvokeMethod_CallsPrivateMethod()
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

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("42", output);
    }

    [Fact]
    public void InvokeMethod_WithNoArguments()
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
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Hello, World!", output);
    }

    [Fact]
    public void InvokeMethod_WithMultipleArguments()
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
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("25", output); // 10 + 5 * 3 = 25
    }

    [Fact]
    public void InvokeMethod_WithStringReturn()
    {
        // Arrange
        var code = @"
            class Formatter {
                public func format(prefix:string, value:int) -> string {
                    return prefix + value.ToStr()
                }
            }
            f <- Formatter()
            result <- InvokeMethod(f, ""format"", {""Value: "", 42})
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Value: 42", output);
    }

    #endregion

    #region GetField 测试

    [Fact]
    public void GetField_ReturnsPublicFieldValue()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- ""Alice""
            }
            p <- Person()
            result <- GetField(p, ""name"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Alice", output);
    }

    [Fact]
    public void GetField_ReturnsPrivateFieldValue()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretCode:int <- 12345
            }
            s <- Secret()
            result <- GetField(s, ""secretCode"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("12345", output);
    }

    [Fact]
    public void GetField_ReturnsIntegerField()
    {
        // Arrange
        var code = @"
            class Counter {
                public count:int <- 100
            }
            c <- Counter()
            result <- GetField(c, ""count"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("100", output);
    }

    [Fact]
    public void GetField_ReturnsBooleanField()
    {
        // Arrange
        var code = @"
            class Flag {
                public isActive:bool <- true
            }
            f <- Flag()
            result <- GetField(f, ""isActive"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("True", output);
    }

    #endregion

    #region SetField 测试

    [Fact]
    public void SetField_ModifiesPublicField()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- ""Unknown""
            }
            p <- Person()
            SetField(p, ""name"", ""Bob"")
            PrintLine(p.name)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Bob", output);
    }

    [Fact]
    public void SetField_ModifiesPrivateField()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue:int <- 0
                public func getSecret() -> int {
                    return secretValue
                }
            }
            s <- Secret()
            SetField(s, ""secretValue"", 999)
            result <- s.getSecret()
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("999", output);
    }

    [Fact]
    public void SetField_ThenGetField_ReturnsNewValue()
    {
        // Arrange
        var code = @"
            class Data {
                public value:int <- 0
            }
            d <- Data()
            SetField(d, ""value"", 42)
            result <- GetField(d, ""value"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("42", output);
    }

    [Fact]
    public void SetField_ModifiesStringField()
    {
        // Arrange
        var code = @"
            class Message {
                public text:string <- ""Hello""
            }
            m <- Message()
            SetField(m, ""text"", ""Goodbye"")
            result <- GetField(m, ""text"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Goodbye", output);
    }

    #endregion

    #region CreateInstance 测试

    [Fact]
    public void CreateInstance_CreatesObjectWithNoArgs()
    {
        // Arrange
        var code = @"
            class Simple {
                public value:int <- 10
            }
            obj <- CreateInstance(""Simple"", {})
            PrintLine(obj.value.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("10", output);
    }

    [Fact]
    public void CreateInstance_CreatesObjectWithArgs()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- """"
                public age:int <- 0
                public func init(n:string, a:int) -> void {
                    name <- n
                    age <- a
                }
            }
            obj <- CreateInstance(""Person"", {""Alice"", 25})
            PrintLine(obj.name)
            PrintLine(obj.age.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Alice", lines[0]);
        Assert.Equal("25", lines[1]);
    }

    [Fact]
    public void CreateInstance_CanCallMethodsOnCreatedObject()
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

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("8", output);
    }

    [Fact]
    public void CreateInstance_CanAccessFieldsOnCreatedObject()
    {
        // Arrange
        var code = @"
            class Config {
                public setting:string <- ""default""
            }
            cfg <- CreateInstance(""Config"", {})
            PrintLine(cfg.setting)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("default", output);
    }

    #endregion

    #region IsInstanceOf 测试

    [Fact]
    public void IsInstanceOf_ReturnsTrueForCorrectClass()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- ""Test""
            }
            p <- Person()
            result <- IsInstanceOf(p, ""Person"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("True", output);
    }

    [Fact]
    public void IsInstanceOf_ReturnsFalseForWrongClass()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- ""Test""
            }
            class Animal {
                public species:string <- ""Unknown""
            }
            p <- Person()
            result <- IsInstanceOf(p, ""Animal"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("False", output);
    }

    [Fact]
    public void IsInstanceOf_WithDynamicallyCreatedInstance()
    {
        // Arrange
        var code = @"
            class Widget {
                public id:int <- 0
            }
            w <- CreateInstance(""Widget"", {})
            result <- IsInstanceOf(w, ""Widget"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("True", output);
    }

    #endregion

    #region HasMethod 测试

    [Fact]
    public void HasMethod_ReturnsTrueForExistingMethod()
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

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("true", output);
    }

    [Fact]
    public void HasMethod_ReturnsFalseForNonExistingMethod()
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

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("false", output);
    }

    [Fact]
    public void HasMethod_ReturnsTrueForPrivateMethod()
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

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("true", output);
    }

    [Fact]
    public void HasMethod_ReturnsTrueForInitMethod()
    {
        // Arrange
        var code = @"
            class Person {
                public func init(name:string) -> void { }
            }
            p <- Person(""Test"")
            result <- HasMethod(p, ""init"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("true", output);
    }

    #endregion

    #region HasField 测试

    [Fact]
    public void HasField_ReturnsTrueForExistingField()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- ""Test""
            }
            p <- Person()
            result <- HasField(p, ""name"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("true", output);
    }

    [Fact]
    public void HasField_ReturnsFalseForNonExistingField()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string <- ""Test""
            }
            p <- Person()
            result <- HasField(p, ""age"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("false", output);
    }

    [Fact]
    public void HasField_ReturnsTrueForPrivateField()
    {
        // Arrange
        var code = @"
            class Secret {
                private secretValue:int <- 42
            }
            s <- Secret()
            result <- HasField(s, ""secretValue"")
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("true", output);
    }

    #endregion

    #region 综合测试

    [Fact]
    public void Reflection_CompleteWorkflow()
    {
        // Arrange - 测试完整的反射工作流
        var code = @"
            class Person {
                private name:string <- ""Unknown""
                private age:int <- 0

                public func init(n:string, a:int) -> void {
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

            privateAge <- InvokeMethod(person, ""getAge"", {})
            PrintLine(""Age: "" + privateAge.ToStr())

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

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("Class: Person", lines);
        Assert.Contains("Has greet: True", lines);
        Assert.Contains("Has name: True", lines);
        Assert.Contains("Greeting: Hello, I am Alice", lines);
        Assert.Contains("Age: 25", lines);
        Assert.Contains("Name: Alice", lines);
        Assert.Contains("New name: Bob", lines);
        Assert.Contains("Is Person: True", lines);
    }

    [Fact]
    public void Reflection_DynamicMethodDispatch()
    {
        // Arrange - 测试动态方法分发
        var code = @"
            class Rectangle {
                private width:int <- 0
                private height:int <- 0

                public func init(w:int, h:int) -> void {
                    width <- w
                    height <- h
                }

                public func getArea() -> int {
                    return width * height
                }
            }

            class Circle {
                private radius:int <- 0

                public func init(r:int) -> void {
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

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("Rectangle area: 20", lines); // 4 * 5 = 20
        Assert.Contains("Circle area: 27", lines);    // 3 * 3 * 3 = 27
    }

    [Fact]
    public void Reflection_ChainedOperations()
    {
        // Arrange - 测试链式反射操作
        var code = @"
            class Counter {
                private count:int <- 0

                public func increment() -> int {
                    count <- count + 1
                    return count
                }

                public func getCount() -> int {
                    return count
                }
            }

            c <- Counter()

            // 多次调用方法
            InvokeMethod(c, ""increment"", {})
            InvokeMethod(c, ""increment"", {})
            InvokeMethod(c, ""increment"", {})

            result <- InvokeMethod(c, ""getCount"", {})
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("3", output);
    }

    [Fact]
    public void Reflection_ModifyAndVerify()
    {
        // Arrange - 测试修改后验证
        var code = @"
            class Config {
                private setting1:int <- 0
                private setting2:string <- """"
                private setting3:bool <- false
            }

            cfg <- Config()

            // 修改所有字段
            SetField(cfg, ""setting1"", 100)
            SetField(cfg, ""setting2"", ""enabled"")
            SetField(cfg, ""setting3"", true)

            // 验证修改
            PrintLine(GetField(cfg, ""setting1"").ToStr())
            PrintLine(GetField(cfg, ""setting2"").ToStr())
            PrintLine(GetField(cfg, ""setting3"").ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("100", lines[0]);
        Assert.Equal("enabled", lines[1]);
        Assert.Equal("True", lines[2]);
    }

    [Fact]
    public void Reflection_CreateAndInspect()
    {
        // Arrange - 测试动态创建和检查
        var code = @"
            class Product {
                public name:string <- """"
                public price:int <- 0

                public func init(n:string, p:int) -> void {
                    name <- n
                    price <- p
                }

                public func getInfo() -> string {
                    return name + "": "" + price.ToStr()
                }
            }

            // 动态创建实例
            product <- CreateInstance(""Product"", {""Widget"", 99})

            // 检查类型
            PrintLine(""Is Product: "" + IsInstanceOf(product, ""Product"").ToStr())

            // 检查成员
            PrintLine(""Has name: "" + HasField(product, ""name"").ToStr())
            PrintLine(""Has getInfo: "" + HasMethod(product, ""getInfo"").ToStr())

            // 调用方法
            info <- InvokeMethod(product, ""getInfo"", {})
            PrintLine(""Info: "" + info.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("Is Product: True", lines);
        Assert.Contains("Has name: True", lines);
        Assert.Contains("Has getInfo: True", lines);
        Assert.Contains("Info: Widget: 99", lines);
    }

    #endregion
}
