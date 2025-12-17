using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Interfaces;

/// <summary>
/// 抽象类语法测试
/// </summary>
[Collection("Sequential")]
public class AbstractClassTests
{
    #region 抽象类基础语法

    /// <summary>
    /// 测试基本抽象类声明
    /// </summary>
    [Fact]
    public void ParseProgram_BasicAbstractClass_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
abstract class Shape {
    protected Name
    protected color

    public func constructor(name:string, color:string) {
        this.Name <- name
        this.color <- color
    }

    abstract func CalculateArea() -> double
    abstract func Draw() -> void

    public func GetDescription() -> string {
        return ""Shape: "" + this.Name + "", Color: "" + this.color
    }

    public func SetColor(color:string) -> void {
        this.color <- color
    }
}

class Circle : Shape {
    private radius

    public func constructor(radius:double) {
        super(""Circle"", ""Red"")
        this.radius <- radius
    }

    public override func CalculateArea() -> double {
        return 3.14159 * this.radius * this.radius
    }

    public override func Draw() -> void {
        PrintLine(""Drawing a circle with radius "" + this.radius.ToStr())
    }
}

circle <- Circle(5.0)
area <- circle.CalculateArea()
circle.Draw()
description <- circle.GetDescription()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试抽象类继承链
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassInheritanceChain_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
abstract class Animal {
    protected name
    protected age

    public func constructor(name:string, age:int) {
        this.name <- name
        this.age <- age
    }

    abstract func MakeSound() -> void
    abstract func Move() -> void

    public func Eat() -> void {
        PrintLine(this.name + "" is eating"")
    }

    public func Sleep() -> void {
        PrintLine(this.name + "" is sleeping"")
    }
}

abstract class Mammal : Animal {
    protected furColor

    public func constructor(name:string, age:int, furColor:string) {
        super(name, age)
        this.furColor <- furColor
    }

    abstract func GiveBirth() -> void

    public func GrowFur() -> void {
        PrintLine(this.name + "" is growing fur"")
    }

    public override func Move() -> void {
        PrintLine(this.name + "" is walking on legs"")
    }
}

class Dog : Mammal {
    private breed

    public func constructor(name:string, age:int, furColor:string, breed:string) {
        super(name, age, furColor)
        this.breed <- breed
    }

    public override func MakeSound() -> void {
        PrintLine(this.name + "" says: Woof!"")
    }

    public override func GiveBirth() -> void {
        PrintLine(this.name + "" is giving birth to puppies"")
    }
}

dog <- Dog(""Buddy"", 3, ""Brown"", ""Golden Retriever"")
dog.MakeSound()
dog.Move()
dog.Eat()
dog.GiveBirth()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试抽象类实现接口
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassImplementingInterface_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IDrawable {
    func Draw() -> void
    func GetPosition() -> dict
}

interface IMovable {
    func Move(x:double, y:double) -> void
    func GetSpeed() -> double
}

abstract class GameObject : IDrawable, IMovable {
    protected x
    protected y
    protected speed

    public func constructor(x:double, y:double, speed:double) {
        this.x <- x
        this.y <- y
        this.speed <- speed
    }

    public func GetPosition() -> dict {
        return {""x"": this.x, ""y"": this.y}
    }

    public func GetSpeed() -> double {
        return this.speed
    }

    public func Move(x:double, y:double) -> void {
        this.x <- this.x + x
        this.y <- this.y + y
    }

    abstract func Draw() -> void
    abstract func Update() -> void
}

class Player : GameObject {
    private health
    private score

    public func constructor(x:double, y:double) {
        super(x, y, 5.0)
        this.health <- 100
        this.score <- 0
    }

    public override func Draw() -> void {
        PrintLine(""Drawing player at ("" + this.x.ToStr() + "", "" + this.y.ToStr() + "")"")
    }

    public override func Update() -> void {
        PrintLine(""Updating player state"")
    }

    public func TakeDamage(amount:int) -> void {
        this.health <- this.health - amount
    }

    public func AddScore(points:int) -> void {
        this.score <- this.score + points
    }
}

player <- Player(10.0, 20.0)
player.Draw()
player.Move(5.0, 3.0)
player.Update()
player.TakeDamage(10)
player.AddScore(100)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试抽象类的静态成员
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassStaticMembers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
abstract class DatabaseConnection {
    public static connectionPoolSize <- 10
    public static activeConnections <- 0
    private static connectionString

    public static func SetConnectionString(connStr:string) -> void {
        DatabaseConnection.connectionString <- connStr
    }

    public static func GetConnectionString() -> string {
        return DatabaseConnection.connectionString
    }

    public static func GetAvailableConnections() -> int {
        return connectionPoolSize - activeConnections
    }

    abstract func Connect() -> bool
    abstract func Disconnect() -> void
    abstract func ExecuteQuery(query:string) -> dict
}

class MySQLConnection : DatabaseConnection {
    private isConnected

    public func constructor() {
        this.isConnected <- false
        activeConnections <- activeConnections + 1
    }

    public override func Connect() -> bool {
        if not this.isConnected {
            this.isConnected <- true
            PrintLine(""MySQL connection established"")
            return true
        }
        return false
    }

    public override func Disconnect() -> void {
        if this.isConnected {
            this.isConnected <- false
            activeConnections <- activeConnections - 1
            PrintLine(""MySQL connection closed"")
        }
    }

    public override func ExecuteQuery(query:string) -> dict {
        if this.isConnected {
            PrintLine(""Executing MySQL query: "" + query)
            return {""status"": ""success"", ""rows"": 1}
        } else {
            return {""status"": ""error"", ""message"": ""Not connected""}
        }
    }
}

DatabaseConnection.SetConnectionString(""mysql://localhost:3306/mydb"")
connection1 <- MySQLConnection()
connection1.Connect()
result <- connection1.ExecuteQuery(""SELECT * FROM users"")
connection1.Disconnect()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 抽象方法重载

    /// <summary>
    /// 测试抽象方法重载
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractMethodOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
abstract class Processor {
    abstract func Process(data:int) -> string
    abstract func Process(data:string) -> int
    abstract func Process(data:list) -> bool

    public func GetTypeInfo() -> string {
        return ""Abstract Data Processor""
    }
}

class DataProcessor : Processor {
    public override func Process(data:int) -> string {
        return ""Processed integer: "" + data.ToStr()
    }

    public override func Process(data:string) -> int {
        return data.Length()
    }

    public override func Process(data:list) -> bool {
        return data.Count() > 0
    }
}

processor <- DataProcessor()
intResult <- processor.Process(42)
stringResult <- processor.Process(""Hello World"")
listResult <- processor.Process({1, 2, 3})";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 抽象类的构造函数

    /// <summary>
    /// 测试抽象类的构造函数链
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassConstructorChain_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
abstract class Vehicle {
    protected make
    protected model
    protected year

    public func constructor(make:string, model:string, year:int) {
        this.make <- make
        this.model <- model
        this.year <- year
        PrintLine(""Vehicle created: "" + make + "" "" + model + "" "" + year.ToStr())
    }

    abstract func StartEngine() -> void
    abstract func StopEngine() -> void

    public func GetInfo() -> string {
        return this.year.ToStr() + "" "" + this.make + "" "" + this.model
    }
}

abstract class Car : Vehicle {
    protected numDoors
    protected fuelType

    public func constructor(make:string, model:string, year:int, doors:int, fuel:string) {
        super(make, model, year)
        this.numDoors <- doors
        this.fuelType <- fuel
        PrintLine(""Car specific properties set"")
    }

    public override func StartEngine() -> void {
        PrintLine(""Car engine started (""
    }

    public override func StopEngine() -> void {
        PrintLine(""Car engine stopped"")
    }

    abstract func OpenTrunk() -> void
}

class ElectricCar : Car {
    private batteryCapacity

    public func constructor(make:string, model:string, year:int, doors:int) {
        super(make, model, year, doors, ""electric"")
        this.batteryCapacity <- 100
        PrintLine(""Electric car initialized"")
    }

    public override func StartEngine() -> void {
        PrintLine(""Electric motor started silently"")
    }

    public override func StopEngine() -> void {
        PrintLine(""Electric motor stopped"")
    }

    public override func OpenTrunk() -> void {
        PrintLine(""Electric trunk opened"")
    }

    public func ChargeBattery() -> void {
        PrintLine(""Charging battery: "" + this.batteryCapacity.ToStr() + ""%"")

tesla <- ElectricCar(""Tesla"", ""Model 3"", 2023, 4)
tesla.StartEngine()
tesla.OpenTrunk()
tesla.ChargeBattery()
tesla.StopEngine()
info <- tesla.GetInfo()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误语法测试

    /// <summary>
    /// 测试不完整的抽象类声明
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteAbstractClass_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class TestAbstract {";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试实例化抽象类
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassInstantiation_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
abstract class AbstractTest {
    abstract func DoSomething() -> void
}

// 语法上可能允许，但运行时应该报错
obj <- AbstractTest()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 语法解析应该成功，但执行时应该报错
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试抽象类中的非抽象方法缺少实现
    /// </summary>
    [Fact]
    public void ParseProgram_AbstractClassNonAbstractMethodWithoutImplementation_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
abstract class TestAbstract {
    abstract func AbstractMethod() -> void

    // 非抽象方法应该有实现
    public func ConcreteMethod() -> void
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试子类未实现父类的所有抽象方法
    /// </summary>
    [Fact]
    public void ParseProgram_ClassNotImplementingAllAbstractMethods_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
abstract class AbstractBase {
    abstract func Method1() -> void
    abstract func Method2() -> string
    abstract func Method3() -> int
}

class IncompleteChild : AbstractBase {
    // 只实现了部分抽象方法
    public override func Method1() -> void {
        PrintLine(""Method1 implemented"")
    }

    public override func Method2() -> string {
        return ""Method2 implemented""
    }

    // 缺少Method3的实现
}";

        // 语法上可能正确，但语义上应该报错
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 语法解析应该成功，语义检查应该报错
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}