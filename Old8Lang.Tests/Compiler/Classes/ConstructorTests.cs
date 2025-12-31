using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 编译器模式下的高级类功能测试 - 构造函数
/// </summary>
public class ConstructorTests
{
    private readonly ITestOutputHelper _output;

    public ConstructorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicConstructor_CompilesAndExecutesCorrectly()
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
    public void DefaultConstructor_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class SimpleClass {
                public value <- 42
                
                func init() {
                    // 默认构造函数设置默认值
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
    public void ConstructorWithDefaultParameters_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Config {
                public host <- ""localhost""
                public port <- 8080
                public debug <- false
                
                func init(host: ""localhost"", port: 8080, debug: false) {
                    this.host <- host
                    this.port <- port
                    this.debug <- debug
                }
            }
            
            config1 <- Config()
            config2 <- Config(""example.com"")
            config3 <- Config(""example.com"", 9000)
            config4 <- Config(""example.com"", 9000, true)
            
            Assert.Equal(""localhost"", config1.host)
            Assert.Equal(8080, config1.port)
            Assert.Equal(false, config1.debug)
            
            Assert.Equal(""example.com"", config2.host)
            Assert.Equal(8080, config2.port)
            Assert.Equal(false, config2.debug)
            
            Assert.Equal(""example.com"", config3.host)
            Assert.Equal(9000, config3.port)
            Assert.Equal(false, config3.debug)
            
            Assert.Equal(""example.com"", config4.host)
            Assert.Equal(9000, config4.port)
            Assert.Equal(true, config4.debug)
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
    public void ConstructorWithValidation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Account {
                private balance <- 0
                private owner <- """"
                
                func init(owner:string, initialBalance:double) {
                    if initialBalance < 0 {
                        throw ""Initial balance cannot be negative""
                    }
                    if owner == """" {
                        throw ""Owner name cannot be empty""
                    }
                    this.owner <- owner
                    this.balance <- initialBalance
                }
                
                func getBalance() -> double {
                    return this.balance
                }
                
                func getOwner() -> string {
                    return this.owner
                }
            }
            
            validAccount <- Account(""Alice"", 1000.0)
            balance1 <- validAccount.getBalance()
            owner1 <- validAccount.getOwner()
            
            Assert.Equal(1000.0, balance1)
            Assert.Equal(""Alice"", owner1)
            
            // 测试验证逻辑
            caughtError <- """"
            try {
                invalidAccount <- Account(""Bob"", -500.0)
            } catch (e) {
                caughtError <- e
            }
            
            Assert.Equal(""Initial balance cannot be negative"", caughtError)
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
    public void ConstructorWithComplexLogic_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class DataProcessor {
                private data <- {}
                private processedCount <- 0
                
                func init(rawData:list) {
                    // 构造函数中的复杂初始化逻辑
                    this.data <- {}
                    i <- 0
                    while i < rawData.Count() {
                        item <- rawData[i]
                        if item > 0 {
                            this.data.Add(item * 2)
                            this.processedCount <- this.processedCount + 1
                        }
                        i <- i + 1
                    }
                }
                
                func getDataCount() -> int {
                    return this.data.Count()
                }
                
                func getProcessedCount() -> int {
                    return this.processedCount
                }
                
                func getDataItem(index:int) -> int {
                    return this.data[index]
                }
            }
            
            rawData <- {1, -2, 3, -4, 5, 0}
            processor <- DataProcessor(rawData)
            
            dataCount <- processor.getDataCount()
            processedCount <- processor.getProcessedCount()
            item1 <- processor.getDataItem(0)
            item2 <- processor.getDataItem(1)
            item3 <- processor.getDataItem(2)
            
            Assert.Equal(3, dataCount)        // 只包含正数：1, 3, 5
            Assert.Equal(3, processedCount)   // 处理了3个项目
            Assert.Equal(2, item1)          // 1 * 2
            Assert.Equal(6, item2)          // 3 * 2
            Assert.Equal(10, item3)         // 5 * 2
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
    public void ConstructorWithDependencyInjection_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Logger {
                private prefix <- """"
                
                func init(prefix:string) {
                    this.prefix <- prefix
                }
                
                func log(message:string) -> string {
                    return ""["" + this.prefix + ""] "" + message
                }
            }
            
            class Database {
                private name <- """"
                
                func init(name:string) {
                    this.name <- name
                }
                
                func connect() -> string {
                    return ""Connected to "" + this.name
                }
            }
            
            class Service {
                private logger
                private database
                
                func init(logger:Logger, database:Database) {
                    this.logger <- logger
                    this.database <- database
                }
                
                func start() -> string {
                    logMsg <- this.logger.log(""Service starting"")
                    connMsg <- this.database.connect()
                    return logMsg + ""; "" + connMsg
                }
            }
            
            logger <- Logger(""APP"")
            database <- Database(""production"")
            service <- Service(logger, database)
            
            result <- service.start()
            expected <- ""[APP] Service starting; Connected to production""
            
            Assert.Equal(expected, result)
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
    public void ConstructorWithStaticInitialization_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class ConnectionPool {
                public static maxConnections <- 10
                public static currentConnections <- 0
                private static connections <- {}
                
                public static func initialize(max:int) -> void {
                    ConnectionPool.maxConnections <- max
                    ConnectionPool.currentConnections <- 0
                    ConnectionPool.connections <- {}
                    
                    // 预创建连接
                    i <- 0
                    while i < max {
                        ConnectionPool.connections.Add(""connection_"" + i.ToStr())
                        i <- i + 1
                    }
                }
                
                public static func getConnection() -> string? {
                    if ConnectionPool.currentConnections >= ConnectionPool.maxConnections {
                        return null
                    }
                    
                    conn <- ConnectionPool.connections[ConnectionPool.currentConnections]
                    ConnectionPool.currentConnections <- ConnectionPool.currentConnections + 1
                    return conn
                }
                
                public static func getStats() -> string {
                    return ""Current: "" + ConnectionPool.currentConnections.ToStr() + 
                           ""/Max: "" + ConnectionPool.maxConnections.ToStr()
                }
            }
            
            // 静态初始化
            ConnectionPool.initialize(5)
            stats1 <- ConnectionPool.getStats()
            
            conn1 <- ConnectionPool.getConnection()
            conn2 <- ConnectionPool.getConnection()
            stats2 <- ConnectionPool.getStats()
            
            Assert.Equal(""Current: 0/Max: 5"", stats1)
            Assert.Equal(""Current: 2/Max: 5"", stats2)
            Assert.Equal(""connection_0"", conn1)
            Assert.Equal(""connection_1"", conn2)
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
    public void ConstructorChaining_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Shape {
                protected name <- """"
                protected area <- 0.0
                
                func init(name:string) {
                    this.name <- name
                }
                
                func getName() -> string {
                    return this.name
                }
                
                func getArea() -> double {
                    return this.area
                }
            }
            
            class Rectangle extends Shape {
                private width <- 0
                private height <- 0
                
                func init(width:double, height:double) {
                    // 调用父类构造函数
                    this.init(""Rectangle"")  // 模拟构造函数链式调用
                    this.width <- width
                    this.height <- height
                    this.area <- width * height
                }
                
                func getWidth() -> double {
                    return this.width
                }
                
                func getHeight() -> double {
                    return this.height
                }
            }
            
            class Circle extends Shape {
                private radius <- 0.0
                
                func init(radius:double) {
                    this.init(""Circle"")  // 模拟构造函数链式调用
                    this.radius <- radius
                    this.area <- 3.14159 * radius * radius
                }
                
                func getRadius() -> double {
                    return this.radius
                }
            }
            
            rect <- Rectangle(5.0, 3.0)
            circle <- Circle(2.0)
            
            Assert.Equal(""Rectangle"", rect.getName())
            Assert.Equal(15.0, rect.getArea())
            Assert.Equal(5.0, rect.getWidth())
            Assert.Equal(3.0, rect.getHeight())
            
            Assert.Equal(""Circle"", circle.getName())
            Assert.True(circle.getArea() > 12.5 && circle.getArea() < 12.6)  // π * 2^2
            Assert.Equal(2.0, circle.getRadius())
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
    public void ConstructorWithOptionalParameters_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class UserProfile {
                public name <- """"
                public age <- 0
                public email <- """"
                public active <- true
                
                func init(name:string, age: 18, email: """", active: true) {
                    this.name <- name
                    this.age <- age
                    this.email <- email
                    this.active <- active
                }
                
                func getInfo() -> string {
                    return ""Name: "" + this.name + 
                           "", Age: "" + this.age.ToStr() + 
                           "", Email: "" + this.email + 
                           "", Active: "" + (this.active ? ""yes"" : ""no"")
                }
            }
            
            user1 <- UserProfile(""Alice"")
            user2 <- UserProfile(""Bob"", 25)
            user3 <- UserProfile(""Charlie"", 30, ""charlie@example.com"")
            user4 <- UserProfile(""David"", 35, ""david@example.com"", false)
            
            info1 <- user1.getInfo()
            info2 <- user2.getInfo()
            info3 <- user3.getInfo()
            info4 <- user4.getInfo()
            
            Assert.Equal(""Name: Alice, Age: 18, Email: , Active: yes"", info1)
            Assert.Equal(""Name: Bob, Age: 25, Email: , Active: yes"", info2)
            Assert.Equal(""Name: Charlie, Age: 30, Email: charlie@example.com, Active: yes"", info3)
            Assert.Equal(""Name: David, Age: 35, Email: david@example.com, Active: no"", info4)
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
    public void ConstructorWithResourceManagement_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class ResourceManager {
                private resources <- {}
                private initialized <- false
                
                func init() {
                    // 模拟资源初始化
                    this.resources.Add(""database_connection"")
                    this.resources.Add(""file_handler"")
                    this.resources.Add(""network_socket"")
                    this.initialized <- true
                }
                
                func isInitialized() -> bool {
                    return this.initialized
                }
                
                func getResourceCount() -> int {
                    return this.resources.Count()
                }
                
                func hasResource(name:string) -> bool {
                    i <- 0
                    while i < this.resources.Count() {
                        if this.resources[i] == name {
                            return true
                        }
                        i <- i + 1
                    }
                    return false
                }
            }
            
            class Service {
                private resourceManager
                
                func init() {
                    this.resourceManager <- ResourceManager()
                }
                
                func isReady() -> bool {
                    return this.resourceManager.isInitialized() && 
                           this.resourceManager.getResourceCount() > 0 &&
                           this.resourceManager.hasResource(""database_connection"")
                }
            }
            
            service <- Service()
            isReady <- service.isReady()
            
            Assert.Equal(true, isReady)
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