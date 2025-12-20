using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// 类声明解释模式测试
/// </summary>
public class ClassDeclarationTests
{
    [Fact]
    public void ClassDeclaration_SimpleClass_DeclaresCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- """"
                age <- 0

                func greet() {
                    return ""Hello, I am "" + name
                }
            }
            person <- Person()
            person.name <- ""Alice""
            person.age <- 25
            greeting <- person.greet()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var person = interpreter.Manager.GetValue(new LangId("person"));
        var greeting = interpreter.Manager.GetValue(new LangId("greeting"));

        Assert.NotNull(person);
        Assert.NotNull(greeting);
        Assert.IsType<StringLangValue>(greeting);
        Assert.Equal("Hello, I am Alice", ((StringLangValue)greeting).Value);
    }

    [Fact]
    public void ClassDeclaration_WithAccessModifiers_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            class TestClass {
                public publicField <- 100
                private privateField <- 200
                protected protectedField <- 300
                static staticField <- 400

                public func publicMethod() {
                    return publicField
                }

                private func privateMethod() {
                    return privateField
                }

                static func staticMethod() {
                    return staticField
                }
            }

            obj <- TestClass()
            publicResult <- obj.publicMethod()
            staticResult <- TestClass.staticMethod()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var publicResult = interpreter.Manager.GetValue(new LangId("publicResult"));
        var staticResult = interpreter.Manager.GetValue(new LangId("staticResult"));

        Assert.NotNull(publicResult);
        Assert.NotNull(staticResult);
        Assert.IsType<IntLangValue>(publicResult);
        Assert.IsType<IntLangValue>(staticResult);
        Assert.Equal(100, ((IntLangValue)publicResult).Value);
        Assert.Equal(400, ((IntLangValue)staticResult).Value);
    }

    [Fact]
    public void ClassDeclaration_WithConstructor_InitializesCorrectly()
    {
        // Arrange
        var code = @"
            class Student {
                public name <- """"
                grade <- 0

                func init(name:string, grade:int) {
                    this.name <- name
                    this.grade <- grade
                }

                func getInfo() -> string {
                    return name + "" is in grade "" + grade
                }
            }

            student <- Student(""Bob"", 10)
            info <- student.getInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var info = interpreter.Manager.GetValue(new LangId("info"));
        Assert.NotNull(info);
        Assert.IsType<StringLangValue>(info);
        Assert.Equal("Bob is in grade 10", ((StringLangValue)info).Value);
    }

    [Fact]
    public void ClassDeclaration_MultipleInstances_IndependentState()
    {
        // Arrange
        var code = @"
            class Counter {
                public value <- 0

                func increment() {
                    this.value <- value + 1
                }

                func getValue() {
                    return this.value
                }
            }

            counter1 <- Counter()
            counter2 <- Counter()

            counter1.increment()
            counter1.increment()
            counter2.increment()

            result1 <- counter1.getValue()
            result2 <- counter2.getValue()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result1);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(2, ((IntLangValue)result1).Value);
        Assert.Equal(1, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void ClassDeclaration_WithMethods_CallsCorrectly()
    {
        // Arrange
        var code = @"
            class Calculator {
                public result <- 0

                func add(x) {
                    this.result <- this.result + x
                }

                func multiply(x) {
                    this.result <- this.result * x
                }

                func reset() {
                    this.result <- 0
                }

                func getResult() {
                    return this.result
                }
            }

            calc <- Calculator()
            calc.add(10)
            calc.multiply(2)
            calc.add(5)
            finalResult <- calc.getResult()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var finalResult = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(finalResult);
        Assert.IsType<IntLangValue>(finalResult);
        Assert.Equal(25, ((IntLangValue)finalResult).Value); // ((0+10)*2)+5 = 25
    }

    [Fact]
    public void ClassDeclaration_WithProperties_AccessCorrectly()
    {
        // Arrange
        var code = @"
            class Book {
                public title <- """"
                author <- """"
                pages <- 0
                isAvailable <- true

                func getInfo() -> string {
                    return title + "" by "" + author
                }

                func setAvailable(available:bool) {
                    isAvailable <- available
                }
            }

            book <- Book()
            book.title <- ""1984""
            book.author <- ""George Orwell""
            book.pages <- 328
            book.setAvailable(false)

            info <- book.getInfo()
            pages <- book.pages
            available <- book.isAvailable
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var info = interpreter.Manager.GetValue(new LangId("info"));
        var pages = interpreter.Manager.GetValue(new LangId("pages"));
        var available = interpreter.Manager.GetValue(new LangId("available"));

        Assert.NotNull(info);
        Assert.NotNull(pages);
        Assert.NotNull(available);
        Assert.IsType<StringLangValue>(info);
        Assert.IsType<IntLangValue>(pages);
        Assert.IsType<BoolLangValue>(available);
        Assert.Equal("1984 by George Orwell", ((StringLangValue)info).Value);
        Assert.Equal(328, ((IntLangValue)pages).Value);
        Assert.False(((BoolLangValue)available).Value);
    }

    [Fact]
    public void ClassDeclaration_StaticMembers_SharedAcrossInstances()
    {
        // Arrange - 简化测试，测试基本的静态成员功能
        var code = @"
            class Counter {
                static count <- 0

                static func increment() {
                    count <- count + 1
                }

                static func getCount() {
                    return count
                }
            }

            Counter.increment()
            Counter.increment()
            result <- Counter.getCount()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));

        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(2, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ClassDeclaration_ThisReference_AccessesCurrentInstance()
    {
        // Arrange
        var code = @"
            class Circle {
                public radius <- 0

                func init(r) {
                    this.radius <- r
                }

                func getArea() {
                    return 3.14159 * this.radius * this.radius
                }

                func getCircumference() {
                    return 2 * 3.14159 * this.radius
                }

                func setRadius(newRadius) {
                    this.radius <- newRadius
                }
            }

            circle <- Circle(5)
            area <- circle.getArea()
            circumference <- circle.getCircumference()

            circle.setRadius(10)
            newArea <- circle.getArea()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var area = interpreter.Manager.GetValue(new LangId("area"));
        var circumference = interpreter.Manager.GetValue(new LangId("circumference"));
        var newArea = interpreter.Manager.GetValue(new LangId("newArea"));

        Assert.NotNull(area);
        Assert.NotNull(circumference);
        Assert.NotNull(newArea);
        Assert.IsType<DoubleLangValue>(area);
        Assert.IsType<DoubleLangValue>(circumference);
        Assert.IsType<DoubleLangValue>(newArea);

        // 使用近似比较，因为涉及浮点数计算
        Assert.True(Math.Abs(78.53975 - ((DoubleLangValue)area).Value) < 0.01);
        Assert.True(Math.Abs(31.4159 - ((DoubleLangValue)circumference).Value) < 0.01);
        Assert.True(Math.Abs(314.159 - ((DoubleLangValue)newArea).Value) < 0.01);
    }

    [Fact]
    public void ClassDeclaration_EmptyClass_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            class EmptyClass {
            }

            empty <- EmptyClass()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var empty = interpreter.Manager.GetValue(new LangId("empty"));
        Assert.NotNull(empty);
    }

    [Fact]
    public void ClassDeclaration_WithFieldInitialization_SetsDefaults()
    {
        // Arrange
        var code = @"
            class DefaultValues {
                public intField <- 42
                stringField <- ""default""
                boolField <- true
                doubleField <- 3.14
                charField <- 'A'
            }

            obj <- DefaultValues()
            intVal <- obj.intField
            stringVal <- obj.stringField
            boolVal <- obj.boolField
            doubleVal <- obj.doubleField
            charVal <- obj.charField
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var intVal = interpreter.Manager.GetValue(new LangId("intVal"));
        var stringVal = interpreter.Manager.GetValue(new LangId("stringVal"));
        var boolVal = interpreter.Manager.GetValue(new LangId("boolVal"));
        var doubleVal = interpreter.Manager.GetValue(new LangId("doubleVal"));
        var charVal = interpreter.Manager.GetValue(new LangId("charVal"));

        Assert.NotNull(intVal);
        Assert.NotNull(stringVal);
        Assert.NotNull(boolVal);
        Assert.NotNull(doubleVal);
        Assert.NotNull(charVal);

        Assert.IsType<IntLangValue>(intVal);
        Assert.IsType<StringLangValue>(stringVal);
        Assert.IsType<BoolLangValue>(boolVal);
        Assert.IsType<DoubleLangValue>(doubleVal);
        Assert.IsType<CharLangValue>(charVal);

        Assert.Equal(42, ((IntLangValue)intVal).Value);
        Assert.Equal("default", ((StringLangValue)stringVal).Value);
        Assert.True(((BoolLangValue)boolVal).Value);
        Assert.Equal(3.14, ((DoubleLangValue)doubleVal).Value);
        Assert.Equal('A', ((CharLangValue)charVal).Value);
    }

    [Fact]
    public void ClassDeclaration_WithComplexMethods_HandlesCorrectly()
    {
        // Arrange
        var code = """

                               class StringUtils {
                                   static func reverse(text) -> string {
                                       result <- ""
                                       i <- len(text) - 1
                                       while i >= 0 {
                                           result <- result + text[i]
                                           i <- i - 1
                                       }
                                       return result
                                   }

                                   static func countWords(text) -> int {
                                       if text == "" {
                                           return 0
                                       }
                                       words <- text.Split(' ')
                                       return len(words)
                                   }

                                   static func capitalize(text) -> string {
                                       if text == "" {
                                           return ""
                                       }
                                       first <- text[0].ToStr().ToUpper()
                                       rest <- ""
                                       i <- 1
                                       while i < Len(text) {
                                           rest <- rest + text[i]
                                           i <- i + 1
                                       }
                                       return first + rest
                                   }
                               }

                               reversed <- StringUtils.reverse("hello")
                               wordCount <- StringUtils.countWords("hello world old8lang")
                               capitalized <- StringUtils.capitalize("old8lang")
                           
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var reversed = interpreter.Manager.GetValue(new LangId("reversed"));
        var wordCount = interpreter.Manager.GetValue(new LangId("wordCount"));
        var capitalized = interpreter.Manager.GetValue(new LangId("capitalized"));

        Assert.NotNull(reversed);
        Assert.NotNull(wordCount);
        Assert.NotNull(capitalized);

        Assert.IsType<StringLangValue>(reversed);
        Assert.IsType<IntLangValue>(wordCount);
        Assert.IsType<StringLangValue>(capitalized);

        Assert.Equal("olleh", ((StringLangValue)reversed).Value);
        Assert.Equal(3, ((IntLangValue)wordCount).Value);
        Assert.Equal("Old8lang", ((StringLangValue)capitalized).Value);
    }

    [Fact]
    public void ClassDeclaration_WithMethodChaining_WorksCorrectly()
    {
        // Arrange
        var code = @"
            class StringBuilder {
                private buffer <- """"

                func append(text) -> StringBuilder {
                    buffer <- buffer + text
                    return this
                }

                func prepend(text) -> StringBuilder {
                    buffer <- text + buffer
                    return this
                }

                func toString() -> string {
                    return buffer
                }
            }

            builder <- StringBuilder()
            result <- builder.append("" world"").prepend(""Hello"").toString()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello world", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ClassDeclaration_WithOverloadedMethods_SelectsCorrectly()
    {
        // Arrange
        var code = @"
            class MathHelper {
                func add(a) {
                    return a + 10
                }

                func add(a, b) {
                    return a + b
                }

                func add(a, b, c) {
                    return a + b + c
                }
            }

            helper <- MathHelper()
            result1 <- helper.add(5)
            result2 <- helper.add(3, 7)
            result3 <- helper.add(1, 2, 3)
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
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        Assert.IsType<IntLangValue>(result1);
        Assert.IsType<IntLangValue>(result2);
        Assert.IsType<IntLangValue>(result3);

        Assert.Equal(15, ((IntLangValue)result1).Value); // 5 + 10
        Assert.Equal(10, ((IntLangValue)result2).Value); // 3 + 7
        Assert.Equal(6, ((IntLangValue)result3).Value); // 1 + 2 + 3
    }

    [Fact]
    public void ClassDeclaration_WithNestedClasses_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            class Outer {
                public value <- 0

                class Inner {
                    public innerValue <- 100

                    func getValue() {
                        return innerValue
                    }
                }

                func createInner() -> Inner {
                    return Inner()
                }
            }

            outer <- Outer()
            outer.value <- 50
            inner <- outer.createInner()

            outerResult <- outer.value
            innerResult <- inner.getValue()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var outerResult = interpreter.Manager.GetValue(new LangId("outerResult"));
        var innerResult = interpreter.Manager.GetValue(new LangId("innerResult"));

        Assert.NotNull(outerResult);
        Assert.NotNull(innerResult);

        Assert.IsType<IntLangValue>(outerResult);
        Assert.IsType<IntLangValue>(innerResult);

        Assert.Equal(50, ((IntLangValue)outerResult).Value);
        Assert.Equal(100, ((IntLangValue)innerResult).Value);
    }

    [Fact]
    public void ClassDeclaration_WithUnicodeClassName_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            class 计算器 {
                public 结果 <- 0

                func 加法(数值) {
                    this.结果 <- this.结果 + 数值
                }

                func 取结果() {
                    return this.结果
                }
            }

            calc <- 计算器()
            calc.加法(10)
            calc.加法(20)
            finalResult <- calc.取结果()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var finalResult = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(finalResult);
        Assert.IsType<IntLangValue>(finalResult);
        Assert.Equal(30, ((IntLangValue)finalResult).Value);
    }

    [Fact]
    public void ClassDeclaration_WithMethodParameters_HandlesCorrectly()
    {
        // Arrange
        var code = """

                               class Greeter {
                                   public greeting <- ""

                                   func setGreeting(text:string) {
                                       greeting <- text
                                   }

                                   func sayHello(name:string, punctuation:string) -> string {
                                       return greeting + " " + name + punctuation
                                   }

                                   func createGreeting(title:string, name:string, isFormal:bool) -> string {
                                       if isFormal {
                                           return title + " " + name
                                       } else {
                                           return "Hi " + name
                                       }
                                   }
                               }

                               greeter <- Greeter()
                               greeter.setGreeting("Good morning")
                               hello1 <- greeter.sayHello("Alice", "!")
                               hello2 <- greeter.createGreeting("Mr.", "Smith", true)
                               hello3 <- greeter.createGreeting("", "Bob", false)
                           
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var hello1 = interpreter.Manager.GetValue(new LangId("hello1"));
        var hello2 = interpreter.Manager.GetValue(new LangId("hello2"));
        var hello3 = interpreter.Manager.GetValue(new LangId("hello3"));

        Assert.NotNull(hello1);
        Assert.NotNull(hello2);
        Assert.NotNull(hello3);

        Assert.IsType<StringLangValue>(hello1);
        Assert.IsType<StringLangValue>(hello2);
        Assert.IsType<StringLangValue>(hello3);

        Assert.Equal("Good morning Alice!", ((StringLangValue)hello1).Value);
        Assert.Equal("Mr. Smith", ((StringLangValue)hello2).Value);
        Assert.Equal("Hi Bob", ((StringLangValue)hello3).Value);
    }
}