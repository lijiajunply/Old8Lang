using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 编译器模式下的高级类功能测试 - 成员访问
/// </summary>
public class MemberAccessTests
{
    private readonly ITestOutputHelper _output;

    public MemberAccessTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicFieldAccess_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- """"
                public age <- 0
                
                func init(name:string, age:int) {
                    this.name <- name
                    this.age <- age
                }
            }
            
            person <- Person(""Alice"", 30)
            
            // 读取字段
            nameValue <- person.name
            ageValue <- person.age
            
            // 修改字段
            person.name <- ""Alice Smith""
            person.age <- 31
            
            newName <- person.name
            newAge <- person.age
            
            Assert.Equal(""Alice"", nameValue)
            Assert.Equal(30, ageValue)
            Assert.Equal(""Alice Smith"", newName)
            Assert.Equal(31, newAge)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MethodAccess_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                private result <- 0
                
                func add(value:int) -> int {
                    this.result <- this.result + value
                    return this.result
                }
                
                func multiply(value:int) -> int {
                    this.result <- this.result * value
                    return this.result
                }
                
                func getResult() -> int {
                    return this.result
                }
                
                func reset() -> void {
                    this.result <- 0
                }
            }
            
            calc <- Calculator()
            
            result1 <- calc.add(5)      // 0 + 5 = 5
            result2 <- calc.multiply(2)  // 5 * 2 = 10
            result3 <- calc.add(3)      // 10 + 3 = 13
            result4 <- calc.getResult()  // 13
            
            calc.reset()
            result5 <- calc.getResult()  // 0
            
            Assert.Equal(5, result1)
            Assert.Equal(10, result2)
            Assert.Equal(13, result3)
            Assert.Equal(13, result4)
            Assert.Equal(0, result5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void PublicPrivateAccess_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class AccessTest {
                public publicField <- ""public""
                private privateField <- ""private""
                
                func init() {
                    this.publicField <- ""modified public""
                    this.privateField <- ""modified private""
                }
                
                public func getPublicField() -> string {
                    return this.publicField
                }
                
                public func getPrivateField() -> string {
                    return this.privateField
                }
                
                public func setPrivateField(value:string) -> void {
                    this.privateField <- value
                }
                
                private func privateMethod() -> string {
                    return ""private method result""
                }
                
                public func callPrivateMethod() -> string {
                    return this.privateMethod()
                }
            }
            
            test <- AccessTest()
            
            // 访问public字段
            test.publicField <- ""externally modified""
            publicFieldValue <- test.getPublicField()
            
            // 通过public方法访问private字段
            test.setPrivateField(""internally modified"")
            privateFieldValue <- test.getPrivateField()
            
            // 通过public方法调用private方法
            privateMethodResult <- test.callPrivateMethod()
            
            Assert.Equal(""externally modified"", publicFieldValue)
            Assert.Equal(""internally modified"", privateFieldValue)
            Assert.Equal(""private method result"", privateMethodResult)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void StaticMemberAccess_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class MathUtils {
                public static PI <- 3.14159
                private static counter <- 0
                
                public static func add(a:int, b:int) -> int {
                    return a + b
                }
                
                public static func multiply(a:int, b:int) -> int {
                    return a * b
                }
                
                public static func incrementCounter() -> int {
                    MathUtils.counter <- MathUtils.counter + 1
                    return MathUtils.counter
                }
                
                public static func getCounter() -> int {
                    return MathUtils.counter
                }
            }
            
            // 访问静态字段
            piValue <- MathUtils.PI
            
            // 访问静态方法
            sumResult <- MathUtils.add(10, 20)
            productResult <- MathUtils.multiply(5, 6)
            
            // 访问静态计数器
            count1 <- MathUtils.incrementCounter()
            count2 <- MathUtils.incrementCounter()
            count3 <- MathUtils.getCounter()
            
            Assert.Equal(3.14159, piValue)
            Assert.Equal(30, sumResult)
            Assert.Equal(30, productResult)
            Assert.Equal(1, count1)
            Assert.Equal(2, count2)
            Assert.Equal(2, count3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ChainedMemberAccess_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Address {
                public street <- """"
                public city <- """"
                public country <- """"
                
                func init(street:string, city:string, country:string) {
                    this.street <- street
                    this.city <- city
                    this.country <- country
                }
                
                func getFullAddress() -> string {
                    return this.street + "", "" + this.city + "", "" + this.country
                }
            }
            
            class Person {
                public name <- """"
                private address
                
                func init(name:string, address:Address) {
                    this.name <- name
                    this.address <- address
                }
                
                func getAddress() -> Address {
                    return this.address
                }
                
                func getFullAddress() -> string {
                    return this.address.getFullAddress()
                }
            }
            
            class Company {
                public name <- """"
                private employees
                
                func init(name:string) {
                    this.name <- name
                    this.employees <- {}
                }
                
                func addEmployee(person:Person) -> void {
                    this.employees.Add(person)
                }
                
                func getFirstEmployee() -> Person {
                    return this.employees[0]
                }
                
                func getFirstEmployeeAddress() -> string {
                    return this.employees[0].getFullAddress()
                }
            }
            
            address <- Address(""123 Main St"", ""New York"", ""USA"")
            person <- Person(""Alice"", address)
            company <- Company(""Tech Corp"")
            company.addEmployee(person)
            
            // 链式访问
            employeeName <- company.getFirstEmployee().name
            employeeAddress <- company.getFirstEmployee().getAddress().street
            fullAddress <- company.getFirstEmployeeAddress()
            
            Assert.Equal(""Alice"", employeeName)
            Assert.Equal(""123 Main St"", employeeAddress)
            Assert.Equal(""123 Main St, New York, USA"", fullAddress)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MemberAccessWithComplexTypes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class DataCollection {
                public numbers <- {}
                public strings <- {}
                public mappings <- {}
                
                func init() {
                    this.numbers <- {1, 2, 3, 4, 5}
                    this.strings <- {""a"", ""b"", ""c""}
                    this.mappings <- {""key1"": ""value1"", ""key2"": ""value2""}
                }
                
                func getNumbers() -> list {
                    return this.numbers
                }
                
                func getStrings() -> list {
                    return this.strings
                }
                
                func getMappings() -> dict {
                    return this.mappings
                }
                
                func addNumber(value:int) -> void {
                    this.numbers.Add(value)
                }
                
                func updateMapping(key:string, value:string) -> void {
                    this.mappings[key] <- value
                }
            }
            
            collection <- DataCollection()
            
            // 访问复杂类型字段
            numbers <- collection.getNumbers()
            strings <- collection.getStrings()
            mappings <- collection.getMappings()
            
            // 修改复杂类型
            collection.addNumber(6)
            collection.updateMapping(""key3"", ""value3"")
            
            updatedNumbers <- collection.getNumbers()
            updatedMappings <- collection.getMappings()
            
            Assert.Equal(5, numbers.Count())
            Assert.Equal(3, strings.Count())
            Assert.Equal(""value1"", mappings[""key1""])
            Assert.Equal(6, updatedNumbers.Count())
            Assert.Equal(""value3"", updatedMappings[""key3""])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MemberAccessWithInheritance_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Shape {
                protected name <- """"
                protected area <- 0.0
                
                func init(name:string) {
                    this.name <- name
                }
                
                public func getName() -> string {
                    return this.name
                }
                
                public func getArea() -> double {
                    return this.area
                }
                
                protected func setArea(area:double) -> void {
                    this.area <- area
                }
            }
            
            class Rectangle extends Shape {
                private width <- 0.0
                private height <- 0.0
                
                func init(width:double, height:double) {
                    this.init(""Rectangle"")
                    this.width <- width
                    this.height <- height
                    this.setArea(width * height)
                }
                
                public func getWidth() -> double {
                    return this.width
                }
                
                public func getHeight() -> double {
                    return this.height
                }
                
                public func getPerimeter() -> double {
                    return 2 * (this.width + this.height)
                }
            }
            
            rect <- Rectangle(5.0, 3.0)
            
            // 访问继承的方法
            shapeName <- rect.getName()
            shapeArea <- rect.getArea()
            
            // 访问自身的方法
            width <- rect.getWidth()
            height <- rect.getHeight()
            perimeter <- rect.getPerimeter()
            
            Assert.Equal(""Rectangle"", shapeName)
            Assert.Equal(15.0, shapeArea)
            Assert.Equal(5.0, width)
            Assert.Equal(3.0, height)
            Assert.Equal(16.0, perimeter)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MemberAccessWithThis_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Counter {
                private count <- 0
                private multiplier <- 1
                
                func init(multiplier:int) {
                    this.multiplier <- multiplier
                    this.count <- 0
                }
                
                public func increment() -> int {
                    this.count <- this.count + this.multiplier
                    return this.count
                }
                
                public func add(value:int) -> int {
                    this.count <- this.count + value
                    return this.count
                }
                
                public func getCount() -> int {
                    return this.count
                }
                
                public func getMultiplier() -> int {
                    return this.multiplier
                }
                
                public func reset() -> void {
                    this.count <- 0
                }
                
                public func complexOperation() -> int {
                    // 使用this访问多个成员
                    result <- this.count
                    result <- result + this.multiplier
                    this.count <- result
                    return this.getCount()
                }
            }
            
            counter <- Counter(5)
            
            // 使用this的内部逻辑
            result1 <- counter.increment()  // 0 + 5 = 5
            result2 <- counter.add(10)      // 5 + 10 = 15
            result3 <- counter.getCount()    // 15
            multiplier <- counter.getMultiplier()  // 5
            result4 <- counter.complexOperation()  // 15 + 5 = 20
            
            counter.reset()
            result5 <- counter.getCount()    // 0
            
            Assert.Equal(5, result1)
            Assert.Equal(15, result2)
            Assert.Equal(15, result3)
            Assert.Equal(5, multiplier)
            Assert.Equal(20, result4)
            Assert.Equal(0, result5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MemberAccessErrorHandling_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class SafeAccess {
                public data <- {}
                public name <- """"
                
                func init() {
                    this.data <- {1, 2, 3}
                    this.name <- ""test""
                }
                
                public func getSafeData(index:int) -> int? {
                    if index < 0 || index >= this.data.Count() {
                        return null
                    }
                    return this.data[index]
                }
                
                public func getSafeName() -> string {
                    if this.name == """" {
                        return ""default""
                    }
                    return this.name
                }
                
                public func setData(index:int, value:int) -> bool {
                    if index < 0 || index >= this.data.Count() {
                        return false
                    }
                    this.data[index] <- value
                    return true
                }
            }
            
            safe <- SafeAccess()
            
            // 安全访问
            value1 <- safe.getSafeData(1)  // 2
            value2 <- safe.getSafeData(10) // null
            nameValue <- safe.getSafeName()  // ""test""
            
            // 安全修改
            success1 <- safe.setData(0, 10)  // true
            success2 <- safe.setData(10, 10) // false
            
            updatedValue <- safe.getSafeData(0)  // 10
            
            Assert.Equal(2, value1)
            Assert.Equal(null, value2)
            Assert.Equal(""test"", nameValue)
            Assert.Equal(true, success1)
            Assert.Equal(false, success2)
            Assert.Equal(10, updatedValue)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}