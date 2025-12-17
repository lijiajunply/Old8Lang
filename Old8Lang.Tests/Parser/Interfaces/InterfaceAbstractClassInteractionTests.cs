using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Interfaces;

/// <summary>
/// 接口与抽象类交互测试
/// </summary>
[Collection("Sequential")]
public class InterfaceAbstractClassInteractionTests
{
    #region 接口与抽象类组合使用

    /// <summary>
    /// 测试抽象类实现接口
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassImplementingInterface_ParsesSuccessfully()
    {
        // Arrange
        var code = """

                   interface IComparable {
                       func CompareTo(other) -> int
                   }

                   interface ICloneable {
                       func Clone() -> object
                   }

                   abstract class Shape implements IComparable, ICloneable {
                       protected name
                       protected area

                       public func constructor(name:string) {
                           this.name <- name
                           this.area <- 0
                       }

                       // IComparable 实现（提供默认实现）
                       public func CompareTo(other) -> int {
                           if this.area < other.area {
                               return -1
                           } else if this.area > other.area {
                               return 1
                           } else {
                               return 0
                           }
                       }

                       // ICloneable 作为抽象方法，由子类实现
                       abstract func Clone() -> object

                       abstract func CalculateArea() -> double

                       public func GetInfo() -> string {
                           return "Shape: " + this.name + ", Area: " + this.area.ToStr()
                       }
                   }

                   class Circle extends Shape {
                       private radius

                       public func constructor(radius:double) {
                           super("Circle")
                           this.radius <- radius
                           this.CalculateArea()
                       }

                       public func CalculateArea() -> double {
                           this.area <- 3.14159 * this.radius * this.radius
                           return this.area
                       }

                       public func Clone() -> object {
                           return Circle(this.radius)
                       }
                   }

                   class Rectangle extends Shape {
                       private width
                       private height

                       public func constructor(width:double, height:double) {
                           super("Rectangle")
                           this.width <- width
                           this.height <- height
                           this.CalculateArea()
                       }

                       public func CalculateArea() -> double {
                           this.area <- this.width * this.height
                           return this.area
                       }

                       public func Clone() -> object {
                           return Rectangle(this.width, this.height)
                       }
                   }

                   circle1 <- Circle(5.0)
                   circle2 <- Circle(3.0)
                   rect1 <- Rectangle(4.0, 6.0)

                   comparison <- circle1.CompareTo(circle2)
                   circleClone <- circle1.Clone()
                   rectClone <- rect1.Clone()
                   """;
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试复杂的多层继承和接口实现
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexInheritanceInterfaceHierarchy_ParsesSuccessfully()
    {
        // Arrange
        var code = """

                   interface ISerializable {
                       func Serialize() -> string
                       func Deserialize(data:string) -> void
                   }

                   interface IDisposable {
                       func Dispose() -> void
                   }

                   interface ILogger {
                       func Log(message:string) -> void
                       func GetLogs() -> list
                   }

                   abstract class Component implements IDisposable {
                       protected name
                       protected isDisposed

                       public func constructor(name:string) {
                           this.name <- name
                           this.isDisposed <- false
                       }

                       public func Dispose() -> void {
                           if not this.isDisposed {
                               this.isDisposed <- true
                               PrintLine("Component " + this.name + " disposed")
                           }
                       }

                       abstract func Initialize() -> void
                       abstract func Update() -> void
                   }

                   abstract class NetworkComponent extends Component implements ISerializable {
                       protected address
                       protected port

                       public func constructor(name:string, address:string, port:int) {
                           super(name)
                           this.address <- address
                           this.port <- port
                       }

                       public func Serialize() -> string {
                           return "Name:" + this.name + " Address:" + this.address + " Port:" + this.port.ToStr()
                       }

                       public func Deserialize(data:string) -> void {
                           PrintLine("Deserializing network component: " + data)
                       }

                       public func Update() -> void {
                           PrintLine("Network component updating...")
                       }
                   }

                   class Server extends NetworkComponent implements ILogger {
                       private logs
                       private maxLogSize

                       public func constructor(name:string, address:string, port:int) {
                           super(name, address, port)
                           this.logs <- {}
                           this.maxLogSize <- 100
                       }

                       public func Initialize() -> void {
                           PrintLine("Server " + this.name + " initializing on " + this.address + ":" + this.port.ToStr())
                       }

                       public func Update() -> void {
                           super.Update()
                           PrintLine("Server processing requests...")
                       }

                       public func Log(message:string) -> void {
                           if this.logs.Count() < this.maxLogSize {
                               this.logs.Push(message)
                           }
                           PrintLine("[SERVER] " + message)
                       }

                       public func GetLogs() -> list {
                           return this.logs
                       }

                       public func Dispose() -> void {
                           this.Log("Server shutting down...")
                           super.Dispose()
                       }
                   }

                   server <- Server("MainServer", "192.168.1.1", 8080)
                   server.Initialize()
                   server.Log("Server started successfully")
                   server.Update()
                   logs <- server.GetLogs()
                   serializedData <- server.Serialize()
                   server.Dispose()
                   """;
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试接口作为方法参数和返回值
    /// </summary>
    [Fact]
    public void ParseProgram_InterfaceAsParametersAndReturnValues_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IDrawable {
    func Draw() -> void
    func GetArea() -> double
}

interface IMovable {
    func Move(x:double, y:double) -> void
    func GetPosition() -> dict
}

abstract class Shape implements IDrawable {
    protected x
    protected y

    public func constructor(x:double, y:double) {
        this.x <- x
        this.y <- y
    }

    public func GetPosition() -> dict {
        return {""x"": this.x, ""y"": this.y}
    }

    abstract func Draw() -> void
    abstract func GetArea() -> double
}

class Circle extends Shape implements IMovable {
    private radius

    public func constructor(x:double, y:double, radius:double) {
        super(x, y)
        this.radius <- radius
    }

    public func Draw() -> void {
        PrintLine(""Drawing circle at ("" + this.x.ToStr() + "", "" + this.y.ToStr() + "") with radius "" + this.radius.ToStr())
    }

    public func GetArea() -> double {
        return 3.14159 * this.radius * this.radius
    }

    public func Move(x:double, y:double) -> void {
        this.x <- this.x + x
        this.y <- this.y + y
    }
}

class Rectangle extends Shape implements IMovable {
    private width
    private height

    public func constructor(x:double, y:double, width:double, height:double) {
        super(x, y)
        this.width <- width
        this.height <- height
    }

    public func Draw() -> void {
        PrintLine(""Drawing rectangle at ("" + this.x.ToStr() + "", "" + this.y.ToStr() + "") with size "" + this.width.ToStr() + ""x"" + this.height.ToStr())
    }

    public func GetArea() -> double {
        return this.width * this.height
    }

    public func Move(x:double, y:double) -> void {
        this.x <- this.x + x
        this.y <- this.y + y
    }
}

// 使用接口作为参数的函数
func DrawAllShapes(shapes:list) -> void {
    for shape in shapes {
        shape.Draw()
    }
}

func GetLargestShape(shapes:list) -> IDrawable {
    largest <- null
    largestArea <- -1

    for shape in shapes {
        area <- shape.GetArea()
        if area > largestArea {
            largestArea <- area
            largest <- shape
        }
    }

    return largest
}

func MoveAllMovableShapes(shapes:list, dx:double, dy:double) -> void {
    for shape in shapes {
        shape.Move(dx, dy)
    }
}

// 创建图形集合
circle1 <- Circle(0, 0, 5)
circle2 <- Circle(10, 10, 3)
rect1 <- Rectangle(20, 20, 4, 6)
rect2 <- Rectangle(30, 30, 8, 2)

shapes <- {circle1, circle2, rect1, rect2}

// 使用这些函数
PrintLine(""Drawing all shapes:"")
DrawAllShapes(shapes)

largestShape <- GetLargestShape(shapes)
PrintLine(""Largest shape area: "" + largestShape.GetArea().ToStr())

PrintLine(""Moving all shapes..."")
MoveAllMovableShapes(shapes, 5, 5)

PrintLine(""Drawing shapes after moving:"")
DrawAllShapes(shapes)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 接口默认实现与抽象类方法

    /// <summary>
    /// 测试接口默认实现与抽象类方法的交互
    /// </summary>
    [Fact]
    public void ParseProgram_InterfaceDefaultImplementationInteraction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface ILogger {
    func Log(message:string) -> void
    func GetLogLevel() -> string

    // 默认实现（语法上支持，根据语言特性可能有不同实现方式）
    func LogError(message:string) -> void {
        Log(""[ERROR] "" + message)
    }

    func LogWarning(message:string) -> void {
        Log(""[WARNING] "" + message)
    }
}

abstract class BaseLogger implements ILogger {
    protected logLevel

    public func constructor(level:string) {
        this.logLevel <- level
    }

    public func GetLogLevel() -> string {
        return this.logLevel
    }

    public func Log(message:string) -> void {
        PrintLine(""["" + this.logLevel + ""] "" + message)
    }

    // 抽象方法，子类必须实现
    abstract func ClearLogs() -> void
    abstract func ExportLogs() -> string
}

class FileLogger extends BaseLogger {
    private fileName
    private logs

    public func constructor(fileName:string) {
        super(""FILE"")
        this.fileName <- fileName
        this.logs <- {}
    }

    public func ClearLogs() -> void {
        this.logs <- {}
        PrintLine(""File logs cleared"")
    }

    public func ExportLogs() -> string {
        content <- ""File: "" + this.fileName + ""\nLogs:\n""
        for log in this.logs {
            content <- content + log + ""\n""
        }
        return content
    }

    public func WriteToFile(data:string) -> void {
        this.logs.Push(data)
        Log(""Written to file: "" + this.fileName)
    }
}

class ConsoleLogger extends BaseLogger {
    private maxMessages

    public func constructor(maxMessages:int) {
        super(""CONSOLE"")
        this.maxMessages <- maxMessages
    }

    public func ClearLogs() -> void {
        // Console不需要清除日志
        PrintLine(""Console does not store logs"")
    }

    public func ExportLogs() -> string {
        return ""Console logs cannot be exported""
    }
}

fileLogger <- FileLogger(""app.log"")
consoleLogger <- ConsoleLogger(100)

fileLogger.Log(""Application started"")
fileLogger.LogError(""Something went wrong"")
fileLogger.LogWarning(""Warning message"")

consoleLogger.Log(""Console logging started"")
consoleLogger.LogError(""Console error"")

fileLogger.WriteToFile(""Test data"")
exported <- fileLogger.ExportLogs()
fileLogger.ClearLogs()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 运行时类型检查与转换

    /// <summary>
    /// 测试运行时接口和抽象类的类型检查
    /// </summary>
    [Fact]
    public void ParseProgram_RuntimeTypeChecking_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IWorker {
    func Work() -> void
    func GetSalary() -> double
}

interface IDriver {
    func Drive() -> void
    func GetLicense() -> string
}

abstract class Person {
    protected name
    protected age

    public func constructor(name:string, age:int) {
        this.name <- name
        this.age <- age
    }

    public func GetName() -> string {
        return this.name
    }

    public func GetAge() -> int {
        return this.age
    }
}

class Employee extends Person implements IWorker {
    private salary
    private position

    public func constructor(name:string, age:int, salary:double, position:string) {
        super(name, age)
        this.salary <- salary
        this.position <- position
    }

    public func Work() -> void {
        PrintLine(this.name + "" is working as "" + this.position)
    }

    public func GetSalary() -> double {
        return this.salary
    }
}

class Driver extends Person implements IDriver {
    private licenseNumber
    private vehicleType

    public func constructor(name:string, age:int, license:string, vehicle:string) {
        super(name, age)
        this.licenseNumber <- license
        this.vehicleType <- vehicle
    }

    public func Drive() -> void {
        PrintLine(this.name + "" is driving a "" + this.vehicleType)
    }

    public func GetLicense() -> string {
        return this.licenseNumber
    }
}

class DeliveryDriver extends Driver implements IWorker {
    private salary
    private deliveries

    public func constructor(name:string, age:int, license:string, salary:double) {
        super(name, age, license, ""Van"")
        this.salary <- salary
        this.deliveries <- 0
    }

    public func Work() -> void {
        this.deliveries <- this.deliveries + 1
        PrintLine(this.name + "" completed delivery #"" + this.deliveries.ToStr())
    }

    public func GetSalary() -> double {
        return this.salary
    }

    public func GetDeliveriesCount() -> int {
        return this.deliveries
    }
}

// 类型检查函数
func ProcessWorker(worker:IWorker) -> void {
    worker.Work()
    PrintLine(""Salary: $"" + worker.GetSalary().ToStr())
}

func ProcessDriver(driver:IDriver) -> void {
    driver.Drive()
    PrintLine(""License: "" + driver.GetLicense())
}

// 创建不同类型的对象
emp <- Employee(""Alice"", 30, 50000.0, ""Software Engineer"")
driver <- Driver(""Bob"", 35, ""D123456"", ""Car"")
deliveryDriver <- DeliveryDriver(""Charlie"", 25, ""D789012"", 40000.0)

// 直接调用方法，不需要运行时类型检查
ProcessWorker(emp)
ProcessDriver(driver)
ProcessWorker(deliveryDriver)
ProcessDriver(deliveryDriver)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误处理和边界情况

    /// <summary>
    /// 测试抽象类实现接口但未实现所有方法
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassMissingInterfaceMethods_ParsesSuccessfully()
    {
        // Arrange
        var code = """

                   interface ITestInterface {
                       func Method1() -> void
                       func Method2() -> string
                   }

                   abstract class AbstractTest implements ITestInterface {
                       // 只实现了部分接口方法
                       public func Method1() -> void {
                           PrintLine("Method1 implemented in abstract class")
                       }

                       // Method2 未实现，应该成为抽象方法
                       // abstract func Method2() -> string
                   }
                   """;

        // 语法上可能正确，但语义上应该报错
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试接口方法签名与抽象类方法不匹配
    /// </summary>
    [Fact]
    public void ParseProgram_InterfaceMethodSignatureMismatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IInterface {
    func DoSomething(param:int) -> string
}

abstract class AbstractClass implements IInterface {
    // 参数类型或返回类型不匹配
    public func DoSomething(param:string) -> void {
        PrintLine(""Parameter type mismatch"")
    }
}";

        // 语法解析可能成功，但语义检查应该报错
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}