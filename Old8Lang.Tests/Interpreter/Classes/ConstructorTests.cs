using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// 构造函数解释模式测试
/// </summary>
public class ConstructorTests
{
    [Fact]
    public void Constructor_DefaultConstructor_CreatesInstanceCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- """"
                public age <- 0

                func init() {
                    this.name <- ""Unknown""
                    this.age <- 0
                }
            }
            person <- Person()
            result <- person.name
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Unknown", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Constructor_ParameterizedConstructor_InitializesCorrectly()
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
            person <- Person(""Alice"", 25)
            resultName <- person.name
            resultAge <- person.age
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultName = interpreter.Manager.GetValue(new LangId("resultName"));
        var resultAge = interpreter.Manager.GetValue(new LangId("resultAge"));

        Assert.NotNull(resultName);
        Assert.IsType<StringLangValue>(resultName);
        Assert.Equal("Alice", ((StringLangValue)resultName).Value);

        Assert.NotNull(resultAge);
        Assert.IsType<IntLangValue>(resultAge);
        Assert.Equal(25, ((IntLangValue)resultAge).Value);
    }

    [Fact]
    public void Constructor_WithDefaultValues_InitializesCorrectly()
    {
        // Arrange
        var code = """

                               class Settings {
                                   public theme <- "light"
                                   public fontSize <- 12
                                   public notifications <- true

                                   func init(theme: "dark", fontSize: 14) {
                                       this.theme <- theme
                                       this.fontSize <- fontSize
                                   }
                               }
                               settings1 <- Settings()
                               settings2 <- Settings("light")
                               settings3 <- Settings("blue", 16)
                               theme1 <- settings1.theme
                               theme2 <- settings2.theme
                               theme3 <- settings3.theme
                           
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var theme1 = interpreter.Manager.GetValue(new LangId("theme1"));
        var theme2 = interpreter.Manager.GetValue(new LangId("theme2"));
        var theme3 = interpreter.Manager.GetValue(new LangId("theme3"));

        Assert.NotNull(theme1);
        Assert.IsType<StringLangValue>(theme1);
        Assert.Equal("dark", ((StringLangValue)theme1).Value);

        Assert.NotNull(theme2);
        Assert.IsType<StringLangValue>(theme2);
        Assert.Equal("light", ((StringLangValue)theme2).Value);

        Assert.NotNull(theme3);
        Assert.IsType<StringLangValue>(theme3);
        Assert.Equal("blue", ((StringLangValue)theme3).Value);
    }

    [Fact]
    public void Constructor_MultipleConstructors_UsesCorrectOne()
    {
        // Arrange
        var code = @"
            class Rectangle {
                public width <- 0
                public height <- 0

                func init() {
                    this.width <- 1
                    this.height <- 1
                }

                func init(side:double) {
                    this.width <- side
                    this.height <- side
                }

                func init(width:double, height:double) {
                    this.width <- width
                    this.height <- height
                }
            }
            rect1 <- Rectangle()
            rect2 <- Rectangle(5.0)
            rect3 <- Rectangle(3.0, 4.0)
            area1 <- rect1.width * rect1.height
            area2 <- rect2.width * rect2.height
            area3 <- rect3.width * rect3.height
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var area1 = interpreter.Manager.GetValue(new LangId("area1"));
        var area2 = interpreter.Manager.GetValue(new LangId("area2"));
        var area3 = interpreter.Manager.GetValue(new LangId("area3"));

        Assert.NotNull(area1);
        Assert.IsType<IntLangValue>(area1);
        Assert.Equal(1, ((IntLangValue)area1).Value);

        Assert.NotNull(area2);
        Assert.IsType<DoubleLangValue>(area2);
        Assert.Equal(25.0, ((DoubleLangValue)area2).Value);

        Assert.NotNull(area3);
        Assert.IsType<DoubleLangValue>(area3);
        Assert.Equal(12.0, ((DoubleLangValue)area3).Value);
    }

    [Fact]
    public void Constructor_WithComplexInitialization_CreatesCorrectObject()
    {
        // Arrange
        var code = @"
            class BankAccount {
                public accountNumber <- """"
                public balance <- 0.0
                public owner <- """"
                public isActive <- true

                func init(accountNumber:string, initialBalance:double, owner:string) {
                    this.accountNumber <- accountNumber
                    this.balance <- initialBalance
                    this.owner <- owner
                    if initialBalance < 0 {
                        this.isActive <- false
                    }
                }
            }
            account <- BankAccount(""12345"", 1000.50, ""John Doe"")
            resultBalance <- account.balance
            resultActive <- account.isActive
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultBalance = interpreter.Manager.GetValue(new LangId("resultBalance"));
        var resultActive = interpreter.Manager.GetValue(new LangId("resultActive"));

        Assert.NotNull(resultBalance);
        Assert.IsType<DoubleLangValue>(resultBalance);
        Assert.Equal(1000.50, ((DoubleLangValue)resultBalance).Value);

        Assert.NotNull(resultActive);
        Assert.IsType<BoolLangValue>(resultActive);
        Assert.True(((BoolLangValue)resultActive).Value);
    }

    [Fact]
    public void Constructor_WithNestedObjects_InitializesCorrectly()
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
            }

            class Person {
                public name <- """"
                public address <- null

                func init(name:string, street:string, city:string) {
                    this.name <- name
                    this.address <- Address(street, city)
                }
            }

            person <- Person(""Alice"", ""123 Main St"", ""New York"")
            resultStreet <- person.address.street
            resultCity <- person.address.city
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultStreet = interpreter.Manager.GetValue(new LangId("resultStreet"));
        var resultCity = interpreter.Manager.GetValue(new LangId("resultCity"));

        Assert.NotNull(resultStreet);
        Assert.IsType<StringLangValue>(resultStreet);
        Assert.Equal("123 Main St", ((StringLangValue)resultStreet).Value);

        Assert.NotNull(resultCity);
        Assert.IsType<StringLangValue>(resultCity);
        Assert.Equal("New York", ((StringLangValue)resultCity).Value);
    }

    [Fact]
    public void Constructor_WithValidation_RejectsInvalidInput()
    {
        // Arrange
        var code = @"
            class User {
                public email <- """"
                public age <- 0

                func init(email:string, age:int) {
                    if age < 0 or age > 150 {
                        return null  // 返回null表示创建失败
                    }
                    if not email.Contains(""@"") {
                        return null
                    }
                    this.email <- email
                    this.age <- age
                }
            }
            validUser <- User(""test@example.com"", 25)
            invalidUser1 <- User(""invalid-email"", 25)
            invalidUser2 <- User(""test@example.com"", -5)
            validResult <- validUser != null
            invalid1Result <- invalidUser1 != null
            invalid2Result <- invalidUser2 != null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var validResult = interpreter.Manager.GetValue(new LangId("validResult"));
        var invalid1Result = interpreter.Manager.GetValue(new LangId("invalid1Result"));
        var invalid2Result = interpreter.Manager.GetValue(new LangId("invalid2Result"));

        Assert.NotNull(validResult);
        Assert.IsType<BoolLangValue>(validResult);
        Assert.True(((BoolLangValue)validResult).Value);

        Assert.NotNull(invalid1Result);
        Assert.IsType<BoolLangValue>(invalid1Result);
        Assert.False(((BoolLangValue)invalid1Result).Value);

        Assert.NotNull(invalid2Result);
        Assert.IsType<BoolLangValue>(invalid2Result);
        Assert.False(((BoolLangValue)invalid2Result).Value);
    }

    [Fact]
    public void Constructor_WithMethodCall_CallsMethodDuringInitialization()
    {
        // Arrange
        var code = @"
            class Logger {
                public logs <- {}

                func init() {
                    this.log(""Logger initialized"")
                }

                func log(message:string) {
                    this.logs.Add(message)
                }
            }

            logger <- Logger()
            resultCount <- len(logger.logs)
            resultMessage <- logger.logs[0]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultCount = interpreter.Manager.GetValue(new LangId("resultCount"));
        var resultMessage = interpreter.Manager.GetValue(new LangId("resultMessage"));

        Assert.NotNull(resultCount);
        Assert.IsType<IntLangValue>(resultCount);
        Assert.Equal(1, ((IntLangValue)resultCount).Value);

        Assert.NotNull(resultMessage);
        Assert.IsType<StringLangValue>(resultMessage);
        Assert.Equal("Logger initialized", ((StringLangValue)resultMessage).Value);
    }

    [Fact]
    public void Constructor_WithArrayParameter_InitializesArrayCorrectly()
    {
        // Arrange
        var code = @"
            class Statistics {
                public numbers <- []
                public sum <- 0

                func init(numbers:array) {
                    this.numbers <- numbers
                    for n in numbers {
                        this.sum <- this.sum + n
                    }
                }
            }

            stats <- Statistics([1, 2, 3, 4, 5])
            resultSum <- stats.sum
            resultCount <- len(stats.numbers)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultSum = interpreter.Manager.GetValue(new LangId("resultSum"));
        var resultCount = interpreter.Manager.GetValue(new LangId("resultCount"));

        Assert.NotNull(resultSum);
        Assert.IsType<IntLangValue>(resultSum);
        Assert.Equal(15, ((IntLangValue)resultSum).Value);

        Assert.NotNull(resultCount);
        Assert.IsType<IntLangValue>(resultCount);
        Assert.Equal(5, ((IntLangValue)resultCount).Value);
    }

    [Fact]
    public void Constructor_WithOptionalParameters_HandlesMissingArguments()
    {
        // Arrange
        var code = """

                               class Configuration {
                                   public host <- "localhost"
                                   public port <- 8080
                                   public timeout <- 30
                                   public sslEnabled <- false

                                   func init(host: "localhost", port: 8080, timeout: 30, sslEnabled: false) {
                                       this.host <- host
                                       this.port <- port
                                       this.timeout <- timeout
                                       this.sslEnabled <- sslEnabled
                                   }
                               }

                               config1 <- Configuration()
                               config2 <- Configuration("example.com")
                               config3 <- Configuration("api.test.com", 443)
                               config4 <- Configuration("secure.api.com", 443, 60, true)

                               host1 <- config1.host
                               port1 <- config1.port
                               host2 <- config2.host
                               port2 <- config2.port
                               host3 <- config3.host
                               port3 <- config3.port
                               ssl4 <- config4.sslEnabled
                           
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var host1 = interpreter.Manager.GetValue(new LangId("host1"));
        var port1 = interpreter.Manager.GetValue(new LangId("port1"));
        var host2 = interpreter.Manager.GetValue(new LangId("host2"));
        var port2 = interpreter.Manager.GetValue(new LangId("port2"));
        var host3 = interpreter.Manager.GetValue(new LangId("host3"));
        var port3 = interpreter.Manager.GetValue(new LangId("port3"));
        var ssl4 = interpreter.Manager.GetValue(new LangId("ssl4"));

        Assert.NotNull(host1);
        Assert.IsType<StringLangValue>(host1);
        Assert.Equal("localhost", ((StringLangValue)host1).Value);

        Assert.NotNull(port1);
        Assert.IsType<IntLangValue>(port1);
        Assert.Equal(8080, ((IntLangValue)port1).Value);

        Assert.NotNull(host2);
        Assert.IsType<StringLangValue>(host2);
        Assert.Equal("example.com", ((StringLangValue)host2).Value);

        Assert.NotNull(host3);
        Assert.IsType<StringLangValue>(host3);
        Assert.Equal("api.test.com", ((StringLangValue)host3).Value);

        Assert.NotNull(ssl4);
        Assert.IsType<BoolLangValue>(ssl4);
        Assert.True(((BoolLangValue)ssl4).Value);
    }

    [Fact]
    public void Constructor_ChainingInitialization_CallsOtherConstructor()
    {
        // Arrange
        var code = @"
            class Product {
                public name <- """"
                public price <- 0.0
                public category <- """"
                public discount <- 0.0

                func init(name:string, price:double) {
                    this.name <- name
                    this.price <- price
                    this.category <- ""General""
                }

                func init(name:string, price:double, category:string, discount:double) {
                    // 先调用基础构造函数
                    this.init(name, price)
                    this.category <- category
                    this.discount <- discount
                }
            }

            product1 <- Product(""Book"", 19.99)
            product2 <- Product(""Laptop"", 999.99, ""Electronics"", 0.1)

            category1 <- product1.category
            discount1 <- product1.discount
            category2 <- product2.category
            discount2 <- product2.discount
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var category1 = interpreter.Manager.GetValue(new LangId("category1"));
        var discount1 = interpreter.Manager.GetValue(new LangId("discount1"));
        var category2 = interpreter.Manager.GetValue(new LangId("category2"));
        var discount2 = interpreter.Manager.GetValue(new LangId("discount2"));

        Assert.NotNull(category1);
        Assert.IsType<StringLangValue>(category1);
        Assert.Equal("General", ((StringLangValue)category1).Value);

        Assert.NotNull(discount1);
        Assert.IsType<DoubleLangValue>(discount1);
        Assert.Equal(0.0, ((DoubleLangValue)discount1).Value);

        Assert.NotNull(category2);
        Assert.IsType<StringLangValue>(category2);
        Assert.Equal("Electronics", ((StringLangValue)category2).Value);

        Assert.NotNull(discount2);
        Assert.IsType<DoubleLangValue>(discount2);
        Assert.Equal(0.1, ((DoubleLangValue)discount2).Value);
    }

    [Fact]
    public void Constructor_WithStaticFields_UsesClassWideState()
    {
        // Arrange
        var code = @"
            class Counter {
                static count <- 0
                public id <- 0

                func init() {
                    Counter.count <- Counter.count + 1
                    this.id <- Counter.count
                }

                static func GetCount() -> int {
                    return Counter.count
                }
            }

            counter1 <- Counter()
            counter2 <- Counter()
            counter3 <- Counter()

            id1 <- counter1.id
            id2 <- counter2.id
            id3 <- counter3.id
            totalCount <- Counter.GetCount()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var id1 = interpreter.Manager.GetValue(new LangId("id1"));
        var id2 = interpreter.Manager.GetValue(new LangId("id2"));
        var id3 = interpreter.Manager.GetValue(new LangId("id3"));
        var totalCount = interpreter.Manager.GetValue(new LangId("totalCount"));

        Assert.NotNull(id1);
        Assert.IsType<IntLangValue>(id1);
        Assert.Equal(1, ((IntLangValue)id1).Value);

        Assert.NotNull(id2);
        Assert.IsType<IntLangValue>(id2);
        Assert.Equal(2, ((IntLangValue)id2).Value);

        Assert.NotNull(id3);
        Assert.IsType<IntLangValue>(id3);
        Assert.Equal(3, ((IntLangValue)id3).Value);

        Assert.NotNull(totalCount);
        Assert.IsType<IntLangValue>(totalCount);
        Assert.Equal(3, ((IntLangValue)totalCount).Value);
    }

    [Fact]
    public void Constructor_WithExpressionParameters_EvaluatesExpressions()
    {
        // Arrange
        var code = @"
            class Calculator {
                public result <- 0

                func init(x:int, y:int, operation: ""add"") {
                    if operation == ""add"" {
                        this.result <- x + y
                    } else if operation == ""multiply"" {
                        this.result <- x * y
                    } else if operation == ""power"" {
                        this.result <- x ^ y
                    }
                }
            }

            calc1 <- Calculator(10, 5)
            calc2 <- Calculator(10, 5, ""multiply"")
            calc3 <- Calculator(2, 3, ""power"")

            result1 <- calc1.result
            result2 <- calc2.result
            result3 <- calc3.result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(15, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(50, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(8, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void Constructor_WithPropertyInitialization_UsesComplexLogic()
    {
        // Arrange
        var code = @"
            class GameCharacter {
                public name <- """"
                public health <- 100
                public maxHealth <- 100
                public isAlive <- true
                public level <- 1
                public experience <- 0

                func init(name:string, level: 1) {
                    this.name <- name
                    this.level <- level
                    this.maxHealth <- 50 + (level * 20)
                    this.health <- this.maxHealth
                    this.isAlive <- true
                    this.experience <- 0
                }

                func TakeDamage(damage:int) {
                    this.health <- this.health - damage
                    if health <= 0 {
                        this.health <- 0
                        this.isAlive <- false
                    }
                }
            }

            hero <- GameCharacter(""Hero"", 3)
            villain <- GameCharacter(""Villain"")

            heroHealth <- hero.health
            heroMaxHealth <- hero.maxHealth
            villainHealth <- villain.health
            villainMaxHealth <- villain.maxHealth
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var heroHealth = interpreter.Manager.GetValue(new LangId("heroHealth"));
        var heroMaxHealth = interpreter.Manager.GetValue(new LangId("heroMaxHealth"));
        var villainHealth = interpreter.Manager.GetValue(new LangId("villainHealth"));
        var villainMaxHealth = interpreter.Manager.GetValue(new LangId("villainMaxHealth"));

        Assert.NotNull(heroHealth);
        Assert.IsType<IntLangValue>(heroHealth);
        Assert.Equal(110, ((IntLangValue)heroHealth).Value); // 50 + (3 * 20)

        Assert.NotNull(heroMaxHealth);
        Assert.IsType<IntLangValue>(heroMaxHealth);
        Assert.Equal(110, ((IntLangValue)heroMaxHealth).Value);

        Assert.NotNull(villainHealth);
        Assert.IsType<IntLangValue>(villainHealth);
        Assert.Equal(70, ((IntLangValue)villainHealth).Value); // 50 + (1 * 20)

        Assert.NotNull(villainMaxHealth);
        Assert.IsType<IntLangValue>(villainMaxHealth);
        Assert.Equal(70, ((IntLangValue)villainMaxHealth).Value);
    }

    [Fact]
    public void Constructor_WithLambdaInitialization_SetsFunctionProperties()
    {
        // Arrange
        var code = @"
            class Processor {
                public processFunc <- null
                public validator <- null

                func init(processor:func, validator:func) {
                    this.processFunc <- processor
                    this.validator <- validator
                }

                func Process(data:int) -> bool {
                    if validator(data) {
                        processFunc(data)
                        return true
                    }
                    return false
                }
            }

            processor <- Processor(
                (x:int) -> PrintLine(""Processing: "" + x.ToStr()),
                (x:int) -> x > 0
            )

            result1 <- processor.Process(10)
            result2 <- processor.Process(-5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<BoolLangValue>(result1);
        Assert.True(((BoolLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.False(((BoolLangValue)result2).Value);
    }

    [Fact]
    public void Constructor_WithConditionalLogic_SelectsInitializationPath()
    {
        // Arrange
        var code = @"
            class Account {
                public accountType <- """"
                public balance <- 0.0
                public interestRate <- 0.0
                public overdraftLimit <- 0.0

                func init(accountType:string, initialBalance:double) {
                    this.accountType <- accountType
                    this.balance <- initialBalance

                    if accountType == ""savings"" {
                        this.interestRate <- 0.02
                        this.overdraftLimit <- 0.0
                    } else if accountType == ""checking"" {
                        this.interestRate <- 0.001
                        this.overdraftLimit <- 500.0
                    } else if accountType == ""business"" {
                        this.interestRate <- 0.015
                        this.overdraftLimit <- 10000.0
                    }
                }
            }

            savings <- Account(""savings"", 1000)
            checking <- Account(""checking"", 500)
            business <- Account(""business"", 5000)

            savingsRate <- savings.interestRate
            checkingOverdraft <- checking.overdraftLimit
            businessRate <- business.interestRate
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var savingsRate = interpreter.Manager.GetValue(new LangId("savingsRate"));
        var checkingOverdraft = interpreter.Manager.GetValue(new LangId("checkingOverdraft"));
        var businessRate = interpreter.Manager.GetValue(new LangId("businessRate"));

        Assert.NotNull(savingsRate);
        Assert.IsType<DoubleLangValue>(savingsRate);
        Assert.Equal(0.02, ((DoubleLangValue)savingsRate).Value);

        Assert.NotNull(checkingOverdraft);
        Assert.IsType<DoubleLangValue>(checkingOverdraft);
        Assert.Equal(500, ((DoubleLangValue)checkingOverdraft).Value);

        Assert.NotNull(businessRate);
        Assert.IsType<DoubleLangValue>(businessRate);
        Assert.Equal(0.015, ((DoubleLangValue)businessRate).Value);
    }
}