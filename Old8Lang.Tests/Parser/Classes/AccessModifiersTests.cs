using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Classes;

/// <summary>
/// 访问修饰符测试
/// </summary>
[Collection("Sequential")]
public class AccessModifiersTests
{
    #region 访问修饰符正确语法

    /// <summary>
    /// 测试public成员
    /// </summary>
    [Fact]
    public void ParseProgram_PublicMembers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Person {
    public name
    public age
    public email

    public func constructor(name, age, email) {
        this.name <- name
        this.age <- age
        this.email <- email
    }

    public func getDetails() -> string {
        return ""Name: "" + this.name + "", Age: "" + this.age.ToStr() + "", Email: "" + this.email
    }

    public func updateEmail(newEmail) {
        this.email <- newEmail
    }
}

person <- Person(""Alice"", 25, ""alice@example.com"")
details <- person.getDetails()
person.updateEmail(""alice@newdomain.com"")";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试private成员
    /// </summary>
    [Fact]
    public void ParseProgram_PrivateMembers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class BankAccount {
    private balance
    private accountNumber
    public owner

    public func constructor(owner, initialBalance) {
        this.owner <- owner
        this.balance <- initialBalance
        this.accountNumber <- ""ACC"" + Random().Next(1000, 9999).ToStr()
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

    public func getBalance() -> double {
        return this.balance
    }

    private func validateAmount(amount) -> bool {
        return amount > 0 and amount <= 10000
    }
}

account <- BankAccount(""Alice"", 1000)
account.deposit(500)
currentBalance <- account.getBalance()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试static成员
    /// </summary>
    [Fact]
    public void ParseProgram_StaticMembers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class MathUtils {
    public static PI <- 3.14159
    public static E <- 2.71828
    private static counter <- 0

    public static func add(a, b) -> double {
        MathUtils.counter <- MathUtils.counter + 1
        return a + b
    }

    public static func multiply(a, b) -> double {
        MathUtils.counter <- MathUtils.counter + 1
        return a * b
    }

    public static func getOperationCount() -> int {
        return MathUtils.counter
    }

    public static func factorial(n) -> int {
        if n <= 1 {
            return 1
        } else {
            return n * MathUtils.factorial(n - 1)
        }
    }
}

result1 <- MathUtils.add(5, 3)
result2 <- MathUtils.multiply(4, 7)
result3 <- MathUtils.factorial(5)
operationCount <- MathUtils.getOperationCount()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试混合访问修饰符
    /// </summary>
    [Fact]
    public void ParseProgram_MixedAccessModifiers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Logger {
    private logs
    public maxSize
    private static instanceCount <- 0
    public static version <- ""1.0.0""

    public func constructor(maxSize: 100) {
        this.maxSize <- maxSize
        this.logs <- {}
        Logger.instanceCount <- Logger.instanceCount + 1
    }

    public func log(message) {
        this.addLog(message)
    }

    public func getLogs() -> list {
        return this.logs
    }

    public func clearLogs() {
        this.logs <- {}
    }

    private func addLog(message) {
        if this.logs.Count() >= this.maxSize {
            this.logs.Remove(0)
        }
        this.logs.Push(""["" + DateTime.Now().ToStr() + ""] "" + message)
    }

    private func formatMessage(message) -> string {
        return ""["" + DateTime.Now().ToStr() + ""] "" + message
    }

    public static func getInstanceCount() -> int {
        return Logger.instanceCount
    }

    public static func getVersion() -> string {
        return Logger.version
    }
}

logger1 <- Logger(50)
logger2 <- Logger(100)

logger1.log(""First message"")
logger1.log(""Second message"")
logger2.log(""Another logger message"")

logs1 <- logger1.getLogs()
totalInstances <- Logger.getInstanceCount()
version <- Logger.getVersion()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 访问修饰符复杂场景

    /// <summary>
    /// 测试访问修饰符在继承中的表现
    /// </summary>
    [Fact]
    public void ParseProgram_AccessModifiersInInheritance_ParsesSuccessfully()
    {
        // Arrange
        var code = """

                   class Vehicle {
                       protected brand
                       protected model
                       private serialNumber
                       public year

                       public func constructor(brand, model, year) {
                           this.brand <- brand
                           this.model <- model
                           this.year <- year
                           this.serialNumber <- "SN-" + Random().Next(10000, 99999).ToStr()
                       }

                       public func getInfo() -> string {
                           return this.year.ToStr() + " " + this.brand + " " + this.model
                       }

                       protected func getSerialNumber() -> string {
                           return this.serialNumber
                       }

                       private func generateReport() -> string {
                           return "Vehicle Report: " + this.getInfo()
                       }
                   }

                   class Car extends Vehicle {
                       public doors
                       private fuelType

                       public func constructor(brand, model, year, doors, fuelType) {
                           this.constructor(brand, model, year)  // 调用父类构造函数
                           this.doors <- doors
                           this.fuelType <- fuelType
                       }

                       public func getDetailedInfo() -> string {
                           return this.getInfo() + " (" + this.doors.ToStr() + " doors, " + this.fuelType + ")"
                       }

                       public func getFullReport() -> string {
                           // 可以访问protected成员
                           return "Car: " + this.getDetailedInfo() + " Serial: " + this.getSerialNumber()
                       }

                       private func calculateEfficiency() -> double {
                           // 私有方法实现油耗计算
                           if this.fuelType == "electric" {
                               return 100.0
                           } else if this.fuelType == "hybrid" {
                               return 50.0
                           } else {
                               return 25.0
                           }
                       }
                   }

                   myCar <- Car("Toyota", "Camry", 2024, 4, "hybrid")
                   carInfo <- myCar.getDetailedInfo()
                   fullReport <- myCar.getFullReport()
                   """;
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试静态访问修饰符与实例访问修饰符的混合
    /// </summary>
    [Fact]
    public void ParseProgram_StaticInstanceAccessMix_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class ConfigManager {
    private static configs
    public static currentConfig
    private instanceId
    public instanceName

    public static func constructor() {
        ConfigManager.configs <- {}
        ConfigManager.currentConfig <- """"
    }

    public func constructor(instanceName) {
        this.instanceName <- instanceName
        this.instanceId <- Random().Next(1000, 9999)
    }

    public static func addConfig(key, value) {
        ConfigManager.configs[key] <- value
    }

    public static func getConfig(key) {
        return ConfigManager.configs[key]
    }

    public static func setCurrentConfig(key) {
        ConfigManager.currentConfig <- key
    }

    public func loadConfig(key) {
        if ConfigManager.configs.HasKey(key) {
            this.processConfig(ConfigManager.configs[key])
        }
    }

    private func processConfig(config) {
        // 处理配置的私有逻辑
    }

    public func getInstanceInfo() -> string {
        return ""Instance: "" + this.instanceName + "" (ID: "" + this.instanceId.ToStr() + "")""
    }
}

ConfigManager.addConfig(""database"", ""localhost:5432"")
ConfigManager.addConfig(""timeout"", ""30"")
ConfigManager.setCurrentConfig(""database"")

manager1 <- ConfigManager(""Manager1"")
manager2 <- ConfigManager(""Manager2"")

manager1.loadConfig(""database"")
manager2.loadConfig(""timeout"")

info1 <- manager1.getInstanceInfo()
dbConfig <- ConfigManager.getConfig(""database"")";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 访问修饰符边界情况

    /// <summary>
    /// 测试默认访问修饰符（如果没有指定）
    /// </summary>
    [Fact]
    public void ParseProgram_DefaultAccessModifiers_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class DefaultAccessClass {
    // 没有明确指定访问修饰符的成员
    publicField  // 应该使用默认访问级别
    privateField // 应该使用默认访问级别

    // 没有明确指定访问修饰符的方法
    publicMethod() {
        return this.publicField
    }

    privateMethod() {
        return this.privateField
    }

    public func constructor() {
        this.publicField <- ""public""
        this.privateField <- ""private""
    }
}

instance <- DefaultAccessClass()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试访问修饰符与类型注解的结合
    /// </summary>
    [Fact]
    public void ParseProgram_AccessModifiersWithTypes_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class TypedAccessClass {
    public intField:int
    private stringField:string
    protected doubleField:double
    public static boolField:bool

    public func constructor(intVal:int, stringVal:string, doubleVal:double) {
        this.intField <- intVal
        this.stringField <- stringVal
        this.doubleField <- doubleVal
    }

    public func getIntValue() -> int {
        return this.intField
    }

    private func processString(input:string) -> string {
        return input.ToUpper()
    }

    protected func calculateDouble(value:double) -> double {
        return value * 2.0
    }

    public static func toggleBool() -> bool {
        TypedAccessClass.boolField <- not TypedAccessClass.boolField
        return TypedAccessClass.boolField
    }
}

instance <- TypedAccessClass(42, ""hello"", 3.14)
TypedAccessClass.boolField <- true
newValue <- TypedAccessClass.toggleBool()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的访问修饰符语法

    /// <summary>
    /// 测试无效的访问修饰符组合
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidAccessModifierCombination_ThrowsSyntaxError()
    {
        // Arrange
        var code = """
                   class TestClass {
                       public private field  // 无效的组合访问修饰符
                       public static private method() {  // 多个修饰符的顺序可能错误
                           return "test"
                       }
                   }
                   """;
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试访问修饰符放在错误的位置
    /// </summary>
    [Fact]
    public void ParseProgram_AccessModifierInWrongPosition_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
class TestClass {
    func public publicMethod() {  // 访问修饰符应该在func之后
        return ""test""
    }

    func constructor() public {  // 访问修饰符在参数列表之后
        // constructor body
    }
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的类语法
    /// </summary>
    [Fact]
    public void ParseProgram_UnknownAccessModifier_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
class TestClass {
    public field  // 缺少类型和分号
        return ""test""
    }
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}