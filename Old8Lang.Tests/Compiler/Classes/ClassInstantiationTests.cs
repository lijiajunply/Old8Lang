using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 编译器模式下的高级类功能测试 - 类实例化
/// </summary>
public class ClassInstantiationTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void BasicClassInstantiation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- """"
                age <- 0
                
                func init(n:string, a:int) {
                    this.name <- n
                    this.age <- a
                }
            }
            
            person <- Person(""Alice"", 30)
            Assert.Equal(""Alice"", person.name)
            Assert.Equal(30, person.age)
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
    public void ClassWithDefaultConstructor_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class SimpleClass {
                public value <- 0
                
                func init() {
                    this.value <- 42
                }
            }
            
            instance <- SimpleClass()
            Assert.Equal(42, instance.value)
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
    public void MultipleInstances_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Counter {
                private count <- 0
                
                func init(start:int) {
                    this.count <- start
                }
                
                func increment() -> int {
                    this.count <- this.count + 1
                    return this.count
                }
                
                func getCount() -> int {
                    return this.count
                }
            }
            
            counter1 <- Counter(10)
            counter2 <- Counter(20)
            
            result1 <- counter1.increment()  // 11
            result2 <- counter2.increment()  // 21
            result3 <- counter1.getCount()    // 11
            result4 <- counter2.getCount()    // 21
            result5 <- counter1.increment()  // 12
            
            Assert.Equal(11, result1)
            Assert.Equal(21, result2)
            Assert.Equal(11, result3)
            Assert.Equal(21, result4)
            Assert.Equal(12, result5)
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
    public void ClassWithMethods_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                private result <- 0
                
                func add(value:int) -> void {
                    this.result <- this.result + value
                }
                
                func multiply(value:int) -> void {
                    this.result <- this.result * value
                }
                
                func getResult() -> int {
                    return this.result
                }
                
                func reset() -> void {
                    this.result <- 0
                }
            }
            
            calc <- Calculator()
            calc.add(5)
            calc.multiply(2)
            calc.add(3)
            result <- calc.getResult()
            
            Assert.Equal(13, result)  // ((0 + 5) * 2) + 3 = 13
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
    public void ClassWithPublicPrivateFields_CompilesAndExecutesCorrectly()
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
                
                func getPublicField() -> string {
                    return this.publicField
                }
                
                func getPrivateField() -> string {
                    return this.privateField
                }
                
                func setPrivateField(value:string) -> void {
                    this.privateField <- value
                }
            }
            
            test <- AccessTest()
            test.publicField <- ""externally modified""
            test.setPrivateField(""internally modified"")
            
            publicResult <- test.getPublicField()
            privateResult <- test.getPrivateField()
            
            Assert.Equal(""externally modified"", publicResult)
            Assert.Equal(""internally modified"", privateResult)
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
    public void ClassWithStaticFields_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class StaticCounter {
                public static count <- 0
                
                public static func increment() -> int {
                    StaticCounter.count <- StaticCounter.count + 1
                    return StaticCounter.count
                }
                
                public static func getCount() -> int {
                    return StaticCounter.count
                }
                
                public static func reset() -> void {
                    StaticCounter.count <- 0
                }
            }
            
            result1 <- StaticCounter.increment()  // 1
            result2 <- StaticCounter.increment()  // 2
            result3 <- StaticCounter.getCount()    // 2
            StaticCounter.reset()
            result4 <- StaticCounter.getCount()    // 0
            result5 <- StaticCounter.increment()  // 1
            
            Assert.Equal(1, result1)
            Assert.Equal(2, result2)
            Assert.Equal(2, result3)
            Assert.Equal(0, result4)
            Assert.Equal(1, result5)
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
    public void NestedClassInstantiation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Address {
                public street <- """"
                public city <- """"
                
                func init(street:string, city:string) {
                    this.street <- street
                    this.city <- city
                }
                
                func getFullAddress() -> string {
                    return this.street + "", "" + this.city
                }
            }
            
            class Person {
                public name <- """"
                private address
                
                func init(name:string, street:string, city:string) {
                    this.name <- name
                    this.address <- Address(street, city)
                }
                
                func getAddress() -> string {
                    return this.address.getFullAddress()
                }
            }
            
            person <- Person(""Alice"", ""123 Main St"", ""New York"")
            addressResult <- person.getAddress()
            
            Assert.Equal(""Alice"", person.name)
            Assert.Equal(""123 Main St, New York"", addressResult)
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
    public void ClassWithComplexTypes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class DataProcessor {
                public data_list <- {}
                public data_dict <- {}
                
                func init() {
                    this.data_list <- {1, 2, 3}
                    this.data_dict <- {""key1"": ""value1"", ""key2"": ""value2""}
                }
                
                func addItem(item:int) -> void {
                    this.data_list.Add(item)
                }
                
                func addDictEntry(key:string, value:string) -> void {
                    this.data_dict[key] <- value
                }
                
                func getListCount() -> int {
                    return this.data_list.Count()
                }
                
                func getDictValue(key:string) -> string {
                    return this.data_dict.GetOrElse(key, ""default"")
                }
            }
            
            processor <- DataProcessor()
            processor.addItem(4)
            processor.addItem(5)
            processor.addDictEntry(""key3"", ""value3"")
            
            listCount <- processor.getListCount()
            value1 <- processor.getDictValue(""key1"")
            value2 <- processor.getDictValue(""key2"")
            value3 <- processor.getDictValue(""key3"")
            missing <- processor.getDictValue(""missing"")
            
            Assert.Equal(5, listCount)
            Assert.Equal(""value1"", value1)
            Assert.Equal(""value2"", value2)
            Assert.Equal(""value3"", value3)
            Assert.Equal(""default"", missing)
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
    public void ClassInstanceInArray_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Point {
                public x <- 0
                public y <- 0
                
                func init(x:int, y:int) {
                    this.x <- x
                    this.y <- y
                }
                
                func distance() -> double {
                    return (this.x * this.x + this.y * this.y).Sqrt()
                }
                
                func toString() -> string {
                    return ""("" + this.x.ToStr() + "", "" + this.y.ToStr() + "")""
                }
            }
            
            points <- [Point(0, 0), Point(3, 4), Point(1, 1)]
            
            dist1 <- points[0].distance()  // sqrt(0^2 + 0^2) = 0
            dist2 <- points[1].distance()  // sqrt(3^2 + 4^2) = 5
            dist3 <- points[2].distance()  // sqrt(1^2 + 1^2) = sqrt(2)
            
            str1 <- points[0].toString()
            str2 <- points[1].toString()
            str3 <- points[2].toString()
            
            Assert.Equal(0.0, dist1)
            Assert.Equal(5.0, dist2)
            Assert.True(dist3 > 1.4 && dist3 < 1.5)
            Assert.Equal(""(0, 0)"", str1)
            Assert.Equal(""(3, 4)"", str2)
            Assert.Equal(""(1, 1)"", str3)
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
    public void ClassInstanceInDictionary_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class User {
                public id <- 0
                public name <- """"
                
                func init(id:int, name:string) {
                    this.id <- id
                    this.name <- name
                }
                
                func getInfo() -> string {
                    return ""ID: "" + this.id.ToStr() + "", Name: "" + this.name
                }
            }
            
            users <- {}
            users[""user1""] <- User(1, ""Alice"")
            users[""user2""] <- User(2, ""Bob"")
            users[""user3""] <- User(3, ""Charlie"")
            
            info1 <- users[""user1""].getInfo()
            info2 <- users[""user2""].getInfo()
            info3 <- users[""user3""].getInfo()
            
            Assert.Equal(""ID: 1, Name: Alice"", info1)
            Assert.Equal(""ID: 2, Name: Bob"", info2)
            Assert.Equal(""ID: 3, Name: Charlie"", info3)
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
    public void ClassWithMethodOverloading_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class OverloadTest {
                public value <- 0
                
                func process() -> string {
                    return ""no parameters""
                }
                
                func process(input:int) -> string {
                    this.value <- input
                    return ""int: "" + input.ToStr()
                }
                
                func process(input:string) -> string {
                    this.value <- input.Length()
                    return ""string: "" + input
                }
                
                func getValue() -> int {
                    return this.value
                }
            }
            
            test <- OverloadTest()
            result1 <- test.process()           // no parameters
            result2 <- test.process(42)         // int parameter
            result3 <- test.process(""hello"")    // string parameter
            valueAfterInt <- test.getValue()
            valueAfterString <- test.getValue()
            
            Assert.Equal(""no parameters"", result1)
            Assert.Equal(""int: 42"", result2)
            Assert.Equal(""string: hello"", result3)
            Assert.Equal(42, valueAfterInt)
            Assert.Equal(5, valueAfterString)
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