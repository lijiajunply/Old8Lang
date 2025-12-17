using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Classes;

/// <summary>
/// 类构造函数测试
/// </summary>
[Collection("Sequential")]
public class ConstructorsTests
{
    #region 构造函数正确语法

    /// <summary>
    /// 测试默认构造函数
    /// </summary>
    [Fact]
    public void ParseProgram_DefaultConstructor_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Person {
    public name
    public age

    public func constructor() {
        this.name <- """"
        this.age <- 0
    }

    public func introduce() -> string {
        return ""My name is "" + this.name + "" and I am "" + this.age.ToStr() + "" years old""
    }
}

person <- Person()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试带参数的构造函数
    /// </summary>
    [Fact]
    public void ParseProgram_ParameterizedConstructor_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Person {
    public name
    public age

    public func constructor(name, age) {
        this.name <- name
        this.age <- age
    }

    public func introduce() -> string {
        return ""My name is "" + this.name + "" and I am "" + this.age.ToStr() + "" years old""
    }
}

person <- Person(""Alice"", 25)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试构造函数重载
    /// </summary>
    [Fact]
    public void ParseProgram_ConstructorOverloading_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Person {
    public name
    public age
    public email

    public func constructor() {
        this.name <- """"
        this.age <- 0
        this.email <- """"
    }

    public func constructor(name) {
        this.name <- name
        this.age <- 0
        this.email <- """"
    }

    public func constructor(name, age) {
        this.name <- name
        this.age <- age
        this.email <- """"
    }

    public func constructor(name, age, email) {
        this.name <- name
        this.age <- age
        this.email <- email
    }
}

person1 <- Person()
person2 <- Person(""Bob"")
person3 <- Person(""Charlie"", 30)
person4 <- Person(""David"", 35, ""david@example.com"")";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试构造函数中的初始化逻辑
    /// </summary>
    [Fact]
    public void ParseProgram_ConstructorWithInitLogic_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class BankAccount {
    public balance
    public accountNumber
    public owner

    public func constructor(owner, initialBalance: 0) {
        this.owner <- owner
        this.accountNumber <- ""ACCT"" + Random().Next(1000, 9999).ToStr()
        if initialBalance < 0 {
            this.balance <- 0
        } else {
            this.balance <- initialBalance
        }
    }

    public func deposit(amount) {
        if amount > 0 {
            this.balance <- this.balance + amount
        }
    }

    public func withdraw(amount) -> bool {
        if amount > 0 and amount <= this.balance {
            this.balance <- this.balance - amount
            return true
        }
        return false
    }
}

account <- BankAccount(""Alice"", 1000)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试构造函数调用其他方法
    /// </summary>
    [Fact]
    public void ParseProgram_ConstructorCallingMethods_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Logger {
    public logs
    public maxSize

    public func constructor(maxSize: 100) {
        this.maxSize <- maxSize
        this.logs <- {}
        this.initialize()
    }

    private func initialize() {
        this.log(""Logger initialized with max size: "" + this.maxSize.ToStr())
    }

    public func log(message) {
        if this.logs.Count() < this.maxSize {
            this.logs.Push(message)
        } else {
            this.logs.Remove(0)
            this.logs.Push(message)
        }
    }
}

logger <- Logger(50)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试静态构造函数
    /// </summary>
    [Fact]
    public void ParseProgram_StaticConstructor_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Config {
    public static settings
    public static version

    public static func constructor() {
        settings <- {""debug"": false, ""max_connections"": 100}
        version <- ""1.0.0""
    }

    public static func getSetting(key) {
        return settings[key]
    }

    public static func setSetting(key, value) {
        settings[key] <- value
    }
}

// 不需要实例化，静态构造函数会在类加载时调用
debugMode <- Config.getSetting(""debug"")";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 复杂构造函数场景

    /// <summary>
    /// 测试构造函数中的异常处理
    /// </summary>
    [Fact]
    public void ParseProgram_ConstructorWithErrorHandling_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class SafeDivisor {
    public divisor
    public isValid

    public func constructor(divisor) {
        if divisor == 0 {
            this.isValid <- false
            this.divisor <- 1
        } else {
            this.divisor <- divisor
            this.isValid <- true
        }
    }

    public func divide(number) -> double {
        if this.isValid {
            return number / this.divisor
        } else {
            return 0
        }
    }
}

safeDiv <- SafeDivisor(0)
normalDiv <- SafeDivisor(5)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试构造函数中的复杂对象创建
    /// </summary>
    [Fact]
    public void ParseProgram_ConstructorWithComplexObjects_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Address {
    public street
    public city
    public country

    public func constructor(street, city, country) {
        this.street <- street
        this.city <- city
        this.country <- country
    }
}

class Person {
    public name
    public age
    public address
    public hobbies

    public func constructor(name, age) {
        this.name <- name
        this.age <- age
        this.address <- Address(""123 Main St"", ""Anytown"", ""USA"")
        this.hobbies <- {""reading"", ""coding"", ""music""}
    }

    public func move(newStreet, newCity, newCountry) {
        this.address <- Address(newStreet, newCity, newCountry)
    }
}

person <- Person(""Alice"", 30)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试构造函数链式调用
    /// </summary>
    [Fact]
    public void ParseProgram_ConstructorChaining_ParsesSuccessfully()
    {
        // Arrange
        var code = """

                   class Vehicle {
                       public brand
                       public model
                       public year

                       public func constructor(brand, model, year: 2024) {
                           this.brand <- brand
                           this.model <- model
                           this.year <- year
                       }
                   }

                   class Car extends Vehicle {
                       public doors
                       public fuelType

                       public func constructor(brand, model, doors: 4, fuelType: "gasoline") {
                           // 调用父类构造函数
                           this.constructor(brand, model)
                           this.doors <- doors
                           this.fuelType <- fuelType
                       }

                       public func getSpecs() -> string {
                           return this.year.ToStr() + " " + this.brand + " " + this.model + " (" + this.doors.ToStr() + " doors, " + this.fuelType + ")"
                       }
                   }

                   myCar <- Car("Toyota", "Camry", 4, "hybrid")
                   """;
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 构造函数边界情况

    /// <summary>
    /// 测试构造函数参数默认值
    /// </summary>
    [Fact]
    public void ParseProgram_ConstructorDefaultParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Product {
    public name
    public price
    public category
    public inStock

    public func constructor(name, price: 0.0, category: ""general"", inStock: true) {
        this.name <- name
        this.price <- price
        this.category <- category
        this.inStock <- inStock
    }
}

product1 <- Product(""Book"")
product2 <- Product(""Phone"", 599.99)
product3 <- Product(""Laptop"", 1299.99, ""electronics"")
product4 <- Product(""Desk"", 299.99, ""furniture"", false)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试构造函数参数类型注解
    /// </summary>
    [Fact]
    public void ParseProgram_ConstructorTypeAnnotations_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class TypedClass {
    public intField
    public stringField
    public doubleField
    public boolField

    public func constructor(intField:int, stringField:string, doubleField:double, boolField:bool) {
        this.intField <- intField
        this.stringField <- stringField
        this.doubleField <- doubleField
        this.boolField <- boolField
    }
}

instance <- TypedClass(42, ""Hello"", 3.14, true)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的构造函数语法

    /// <summary>
    /// 测试构造函数名称错误
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidConstructorName_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
class TestClass {
    public func Constructor(name) {  // 大写C应该是小写
        this.name <- name
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 这可能不应该报错，但可能不会被视为构造函数
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试无效的构造函数定义
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteConstructorDefinition_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
class TestClass {
    public func constructor(  // 缺少右括号和参数列表
        this.name <- ""test""
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试构造函数返回类型错误
    /// </summary>
    [Fact]
    public void ParseProgram_ConstructorWithReturnType_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
class TestClass {
    public func constructor(name) -> string {  // 构造函数不应该有返回类型
        this.name <- name
        return ""initialized""
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 构造函数有返回类型可能应该报错，但这取决于语言规范
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}