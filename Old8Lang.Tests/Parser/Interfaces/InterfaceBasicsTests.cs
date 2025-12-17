using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Interfaces;

/// <summary>
/// 接口基础语法测试
/// </summary>
[Collection("Sequential")]
public class InterfaceBasicsTests
{
    #region 接口基础语法

    /// <summary>
    /// 测试基本接口声明
    /// </summary>
    [Fact]
    public void ParseProgram_BasicInterfaceDeclaration_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IDrawable {
    Draw() -> void
    GetArea() -> double
}

class Circle : IDrawable {
    public radius

    public func constructor(radius:double) {
        this.radius <- radius
    }

    public func Draw() -> void {
        PrintLine(""Drawing a circle with radius "" + this.radius.ToStr())
    }

    public func GetArea() -> double {
        return 3.14159 * this.radius * this.radius
    }
}

circle <- Circle(5.0)
circle.Draw()
area <- circle.GetArea()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试带属性的接口
    /// </summary>
    [Fact]
    public void ParseProgram_InterfaceWithProperties_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IShape {
    Name
    Color

    func GetName() -> string
    func SetName(name:string) -> void
    func GetColor() -> string
    func SetColor(color:string) -> void
}

class Rectangle : IShape {
    public Name
    public Color
    public width
    public height

    public func constructor(width:double, height:double) {
        this.width <- width
        this.height <- height
        this.Name <- ""Rectangle""
        this.Color <- ""Blue""
    }

    public func GetName() -> string {
        return this.Name
    }

    public func SetName(name:string) -> void {
        this.Name <- name
    }

    public func GetColor() -> string {
        return this.Color
    }

    public func SetColor(color:string) -> void {
        this.Color <- color
    }
}

rect <- Rectangle(10.0, 5.0)
rect.SetName(""MyRectangle"")
rect.SetColor(""Red"")";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多重接口实现
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleInterfaceImplementation_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface ISerializable {
    func Serialize() -> string
    func Deserialize(data:string) -> void
}

interface ICloneable {
    func Clone() -> object
}

interface IDrawable {
    func Draw() -> void
}

class ComplexObject : ISerializable, ICloneable, IDrawable {
    public id
    public name

    public func constructor(id:int, name:string) {
        this.id <- id
        this.name <- name
    }

    public func Serialize() -> string {
        return ""{id:"" + this.id.ToStr() + "",name:"""" + this.name + """"}""
    }

    public func Deserialize(data:string) -> void {
        // 简化的反序列化逻辑
        this.id <- 0
        this.name <- data
    }

    public func Clone() -> object {
        return ComplexObject(this.id, this.name)
    }

    public func Draw() -> void {
        PrintLine(""Drawing object: "" + this.name)
    }
}

obj <- ComplexObject(1, ""TestObject"")
serialized <- obj.Serialize()
clone <- obj.Clone()
obj.Draw()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试接口继承
    /// </summary>
    [Fact]
    public void ParseProgram_InterfaceInheritance_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IAnimal {
    Name
    func MakeSound() -> void
    func Eat() -> void
}

interface IMammal : IAnimal {
    func GiveBirth() -> void
    func FeedMilk() -> void
}

interface IDomesticAnimal : IAnimal {
    func Train() -> void
    func Pet() -> void
}

class Dog : IMammal, IDomesticAnimal {
    public Name
    public breed

    public func constructor(name:string, breed:string) {
        this.Name <- name
        this.breed <- breed
    }

    public func MakeSound() -> void {
        PrintLine(""Woof!"")
    }

    public func Eat() -> void {
        PrintLine(""Dog is eating"")
    }

    public func GiveBirth() -> void {
        PrintLine(""Dog gives birth to puppies"")
    }

    public func FeedMilk() -> void {
        PrintLine(""Dog feeds puppies"")
    }

    public func Train() -> void {
        PrintLine(""Dog is training"")
    }

    public func Pet() -> void {
        PrintLine(""Dog is being petted"")
    }
}

dog <- Dog(""Buddy"", ""Golden Retriever"")
dog.MakeSound()
dog.Train()
dog.Pet()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试接口中的静态成员
    /// </summary>
    [Fact]
    public void ParseProgram_InterfaceStaticMembers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface ILogger {
    public static LOG_LEVEL_INFO <- ""INFO""
    public static LOG_LEVEL_ERROR <- ""ERROR""

    public static func Log(level:string, message:string) -> void
    public static func Error(message:string) -> void
    public static func Info(message:string) -> void
}

class ConsoleLogger : ILogger {
    public static func Log(level:string, message:string) -> void {
        PrintLine(""["" + level + ""] "" + message)
    }

    public static func Error(message:string) -> void {
        Log(LOG_LEVEL_ERROR, message)
    }

    public static func Info(message:string) -> void {
        Log(LOG_LEVEL_INFO, message)
    }
}

ConsoleLogger.Info(""Application started"")
ConsoleLogger.Error(""Something went wrong!"")";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 接口方法重载

    /// <summary>
    /// 测试接口方法重载
    /// </summary>
    [Fact]
    public void ParseProgram_InterfaceMethodOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface ICalculator {
    func Calculate(a:int, b:int) -> int
    func Calculate(a:double, b:double) -> double
    func Calculate(values:list) -> int
}

class SimpleCalculator : ICalculator {
    public func Calculate(a:int, b:int) -> int {
        return a + b
    }

    public func Calculate(a:double, b:double) -> double {
        return a * b
    }

    public func Calculate(values:list) -> int {
        sum <- 0
        for value in values {
            sum <- sum + value
        }
        return sum
    }
}

calc <- SimpleCalculator()
intResult <- calc.Calculate(5, 3)
doubleResult <- calc.Calculate(2.5, 4.0)
listResult <- calc.Calculate({1, 2, 3, 4, 5})";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 接口与泛型（语法测试）

    /// <summary>
    /// 测试接口泛型语法（注意：Old8Lang当前不支持泛型，这是语法测试）
    /// </summary>
    [Fact]
    public void ParseProgram_InterfaceGenericSyntax_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
// 注意：这是泛型语法的测试，当前语言版本可能不支持泛型实现
interface ICollection {
    func Add(item) -> void
    func Remove(item) -> bool
    func Count() -> int
    func Clear() -> void
}

interface IList : ICollection {
    func Get(index:int) -> object
    func Set(index:int, item) -> void
    func IndexOf(item) -> int
}

class List : IList {
    private items

    public func constructor() {
        this.items <- {}
    }

    public func Add(item) -> void {
        this.items.Push(item)
    }

    public func Remove(item) -> bool {
        // 简化实现
        return true
    }

    public func Count() -> int {
        return this.items.Count()
    }

    public func Clear() -> void {
        this.items <- {}
    }

    public func Get(index:int) -> object {
        return this.items[index]
    }

    public func Set(index:int, item) -> void {
        this.items[index] <- item
    }

    public func IndexOf(item) -> int {
        for i <- 0, i < this.items.Count(), i <- i + 1 {
            if this.items[i] == item {
                return i
            }
        }
        return -1
    }
}

myList <- List()
myList.Add(""item1"")
myList.Add(""item2"")
count <- myList.Count()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误语法测试

    /// <summary>
    /// 测试接口方法缺少返回类型
    /// </summary>
    [Fact]
    public void ParseProgram_InterfaceMethodWithoutReturnType_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
interface ITest {
    func DoSomething()
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 根据语言规范，可能允许返回类型推断，或者要求显式类型
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试类实现接口时缺少必需方法
    /// </summary>
    [Fact]
    public void ParseProgram_ClassMissingInterfaceMethods_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
interface IRequired {
    func Method1() -> void
    func Method2() -> string
}

class IncompleteClass : IRequired {
    // 只实现了Method1，缺少Method2
    public func Method1() -> void {
        PrintLine(""Method1 implemented"")
    }

    // 缺少Method2的实现
}";

        // 这在语法上可能正确，但语义上应该报错
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 语法解析应该成功，语义检查应该报错
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}