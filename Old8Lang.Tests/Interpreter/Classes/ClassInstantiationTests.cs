using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// 类实例化测试
/// </summary>
public class ClassInstantiationTests
{
    [Fact]
    public void ClassInstantiation_DefaultConstructor_CreatesInstanceWithDefaults()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string
                public age:int
                func Init() {
                    name <- ""Unknown""
                    age <- 0
                }
            }
            person <- Person()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("person"));
        Assert.NotNull(result);
        Assert.IsType<AnyLangValue>(result);
    }

    [Fact]
    public void ClassInstantiation_ParameterizedConstructor_CreatesInstanceWithParameters()
    {
        // Arrange
        var code = @"
            class Person {
                public name:string
                public age:int
                func Init(n:string, a:int) {
                    name <- n
                    age <- a
                }
            }
            person <- Person(""Alice"", 30)
            personName <- person.name
            personAge <- person.age
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var person = interpreter.Manager.GetValue(new LangId("person"));
        var name = interpreter.Manager.GetValue(new LangId("personName"));
        var age = interpreter.Manager.GetValue(new LangId("personAge"));

        Assert.NotNull(person);
        Assert.IsType<AnyLangValue>(person);

        Assert.NotNull(name);
        Assert.IsType<StringLangValue>(name);
        Assert.Equal("Alice", ((StringLangValue)name).Value);

        Assert.NotNull(age);
        Assert.IsType<IntLangValue>(age);
        Assert.Equal(30, ((IntLangValue)age).Value);
    }

    [Fact]
    public void ClassInstantiation_MultipleInstances_CreatesMultipleIndependentInstances()
    {
        // Arrange
        var code = @"
            class Counter {
                public value:int
                func Init(v:int) {
                    value <- v
                }
                func Increment() -> void {
                    value <- value + 1
                }
            }
            counter1 <- Counter(10)
            counter2 <- Counter(20)
            counter1.Increment()
            counter1.Increment()
            counter2.Increment()
            result1 <- counter1.value
            result2 <- counter2.value
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(12, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(21, ((IntLangValue)result2).Value);
    }

    [Fact]
    public void ClassInstantiation_NestedObjects_CreatesNestedObjectInstances()
    {
        // Arrange
        var code = @"
            class Address {
                public street:string
                public city:string
                func Init(s:string, c:string) {
                    street <- s
                    city <- c
                }
            }
            class Person {
                public name:string
                public address:Address
                func Init(n:string, addr:Address) {
                    name <- n
                    address <- addr
                }
            }
            addr <- Address(""123 Main St"", ""Anytown"")
            person <- Person(""John Doe"", addr)
            streetName <- person.address.street
            cityName <- person.address.city
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var street = interpreter.Manager.GetValue(new LangId("streetName"));
        var city = interpreter.Manager.GetValue(new LangId("cityName"));

        Assert.NotNull(street);
        Assert.IsType<StringLangValue>(street);
        Assert.Equal("123 Main St", ((StringLangValue)street).Value);

        Assert.NotNull(city);
        Assert.IsType<StringLangValue>(city);
        Assert.Equal("Anytown", ((StringLangValue)city).Value);
    }

    [Fact]
    public void ClassInstantiation_ArrayOfObjects_CreatesArrayOfInstances()
    {
        // Arrange
        var code = @"
            class Point {
                public x:int
                public y:int
                func Init(xPos:int, yPos:int) {
                    x <- xPos
                    y <- yPos
                }
                func Distance() -> double {
                    return (x * x + y * y).ToSqrt()
                }
            }
            points <- [Point(0, 0), Point(3, 4), Point(5, 12)]
            distances <- [0.0, 0.0, 0.0]
            for i in 0..<len(points) {
                distances[i] <- points[i].Distance()
            }
            result1 <- distances[1]
            result2 <- distances[2]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<DoubleLangValue>(result1);
        Assert.Equal(5.0, ((DoubleLangValue)result1).Value); // sqrt(3^2 + 4^2) = 5

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(13.0, ((DoubleLangValue)result2).Value); // sqrt(5^2 + 12^2) = 13
    }

    [Fact]
    public void ClassInstantiation_DefaultParameters_UsesDefaultParameters()
    {
        // Arrange
        var code = @"
            class Product {
                public name:string
                public price:double
                public category:string
                func Init(n:string, p:double, c:""General"") {
                    name <- n
                    price <- p
                    category <- c
                }
            }
            product1 <- Product(""Laptop"", 999.99)
            product2 <- Product(""Mouse"", 25.50, ""Electronics"")
            category1 <- product1.category
            category2 <- product2.category
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var category1 = interpreter.Manager.GetValue(new LangId("category1"));
        var category2 = interpreter.Manager.GetValue(new LangId("category2"));

        Assert.NotNull(category1);
        Assert.IsType<StringLangValue>(category1);
        Assert.Equal("General", ((StringLangValue)category1).Value);

        Assert.NotNull(category2);
        Assert.IsType<StringLangValue>(category2);
        Assert.Equal("Electronics", ((StringLangValue)category2).Value);
    }

    [Fact]
    public void ClassInstantiation_MixedTypeParameters_HandlesVariousParameterTypes()
    {
        // Arrange
        var code = @"
            class LogEntry {
                public timestamp:int
                public message:string
                public level:string
                public isError:bool
                func Init(ts:int, msg:string, lvl:string, err:bool) {
                    timestamp <- ts
                    message <- msg
                    level <- lvl
                    isError <- err
                }
            }
            entry1 <- LogEntry(123456, ""System started"", ""INFO"", false)
            entry2 <- LogEntry(123457, ""Critical error"", ""ERROR"", true)
            isError1 <- entry1.isError
            isError2 <- entry2.isError
            message1 <- entry1.message
            message2 <- entry2.message
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var error1 = interpreter.Manager.GetValue(new LangId("isError1"));
        var error2 = interpreter.Manager.GetValue(new LangId("isError2"));
        var msg1 = interpreter.Manager.GetValue(new LangId("message1"));
        var msg2 = interpreter.Manager.GetValue(new LangId("message2"));

        Assert.NotNull(error1);
        Assert.IsType<BoolLangValue>(error1);
        Assert.False(((BoolLangValue)error1).Value);

        Assert.NotNull(error2);
        Assert.IsType<BoolLangValue>(error2);
        Assert.True(((BoolLangValue)error2).Value);

        Assert.NotNull(msg1);
        Assert.IsType<StringLangValue>(msg1);
        Assert.Equal("System started", ((StringLangValue)msg1).Value);

        Assert.NotNull(msg2);
        Assert.IsType<StringLangValue>(msg2);
        Assert.Equal("Critical error", ((StringLangValue)msg2).Value);
    }

    [Fact]
    public void ClassInstantiation_WithMethodCalls_CallsMethodsDuringInitialization()
    {
        // Arrange
        var code = @"
            class Database {
                public connected:bool
                public connectionCount:int
                func Init() {
                    connected <- false
                    connectionCount <- 0
                    Connect()
                }
                func Connect() -> void {
                    if not connected {
                        connected <- true
                        connectionCount <- connectionCount + 1
                    }
                }
                func Disconnect() -> void {
                    if connected {
                        connected <- false
                    }
                }
            }
            db1 <- Database()
            db2 <- Database()
            db1.Disconnect()
            connected1 <- db1.connected
            connected2 <- db2.connected
            connections <- db2.connectionCount
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var connected1 = interpreter.Manager.GetValue(new LangId("connected1"));
        var connected2 = interpreter.Manager.GetValue(new LangId("connected2"));
        var connections = interpreter.Manager.GetValue(new LangId("connections"));

        Assert.NotNull(connected1);
        Assert.IsType<BoolLangValue>(connected1);
        Assert.False(((BoolLangValue)connected1).Value);

        Assert.NotNull(connected2);
        Assert.IsType<BoolLangValue>(connected2);
        Assert.True(((BoolLangValue)connected2).Value);

        Assert.NotNull(connections);
        Assert.IsType<IntLangValue>(connections);
        Assert.Equal(1, ((IntLangValue)connections).Value);
    }

    [Fact]
    public void ClassInstantiation_ComplexObject_Graph_HandlesComplexObjectGraphs()
    {
        // Arrange
        var code = @"
            class Engine {
                public horsepower:int
                public cylinders:int
                func Init(hp:int, cyl:int) {
                    horsepower <- hp
                    cylinders <- cyl
                }
            }
            class Wheel {
                public size:int
                public brand:string
                func Init(s:int, b:string) {
                    size <- s
                    brand <- b
                }
            }
            class Car {
                public make:string
                public model:string
                public engine:Engine
                public wheels:{Wheel}
                func Init(mk:string, mdl:string) {
                    make <- mk
                    model <- mdl
                    engine <- Engine(300, 6)
                    wheels <- {}
                    wheels.Add(Wheel(18, ""Michelin""))
                    wheels.Add(Wheel(18, ""Michelin""))
                    wheels.Add(Wheel(18, ""Michelin""))
                    wheels.Add(Wheel(18, ""Michelin""))
                }
            }
            car <- Car(""Tesla"", ""Model S"")
            horsepower <- car.engine.horsepower
            wheelCount <- car.len(wheels)
            wheelBrand <- car.wheels[0].brand
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var hp = interpreter.Manager.GetValue(new LangId("horsepower"));
        var wheelCount = interpreter.Manager.GetValue(new LangId("wheelCount"));
        var brand = interpreter.Manager.GetValue(new LangId("wheelBrand"));

        Assert.NotNull(hp);
        Assert.IsType<IntLangValue>(hp);
        Assert.Equal(300, ((IntLangValue)hp).Value);

        Assert.NotNull(wheelCount);
        Assert.IsType<IntLangValue>(wheelCount);
        Assert.Equal(4, ((IntLangValue)wheelCount).Value);

        Assert.NotNull(brand);
        Assert.IsType<StringLangValue>(brand);
        Assert.Equal("Michelin", ((StringLangValue)brand).Value);
    }

    [Fact]
    public void ClassInstantiation_WithValidation_ValidatesParameters()
    {
        // Arrange
        var code = @"
            class BankAccount {
                public balance:double
                public accountNumber:string
                func Init(accNum:string, initialBalance:double) {
                    if len(accNum) < 5 {
                        accountNumber <- ""INVALID""
                    } else {
                        accountNumber <- accNum
                    }

                    if initialBalance < 0 {
                        balance <- 0.0
                    } else {
                        balance <- initialBalance
                    }
                }
                func IsValid() -> bool {
                    return accountNumber != ""INVALID"" and balance >= 0
                }
            }
            account1 <- BankAccount(""12345"", 1000.0)
            account2 <- BankAccount(""12"", -50.0)
            valid1 <- account1.IsValid()
            valid2 <- account2.IsValid()
            balance1 <- account1.balance
            balance2 <- account2.balance
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var valid1 = interpreter.Manager.GetValue(new LangId("valid1"));
        var valid2 = interpreter.Manager.GetValue(new LangId("valid2"));
        var bal1 = interpreter.Manager.GetValue(new LangId("balance1"));
        var bal2 = interpreter.Manager.GetValue(new LangId("balance2"));

        Assert.NotNull(valid1);
        Assert.IsType<BoolLangValue>(valid1);
        Assert.True(((BoolLangValue)valid1).Value);

        Assert.NotNull(valid2);
        Assert.IsType<BoolLangValue>(valid2);
        Assert.False(((BoolLangValue)valid2).Value);

        Assert.NotNull(bal1);
        Assert.IsType<DoubleLangValue>(bal1);
        Assert.Equal(1000.0, ((DoubleLangValue)bal1).Value);

        Assert.NotNull(bal2);
        Assert.IsType<DoubleLangValue>(bal2);
        Assert.Equal(0.0, ((DoubleLangValue)bal2).Value);
    }

    [Fact]
    public void ClassInstantiation_FactoryPattern_CreatesObjectsViaFactory()
    {
        // Arrange
        var code = @"
            class Shape {
                public type:string
                public area:double
                func Init() {
                    type <- ""Unknown""
                    area <- 0.0
                }
                func GetInfo() -> string {
                    return type + "" with area "" + area.ToStr()
                }
            }
            class Circle < Shape {
                func Init(radius:double) {
                    type <- ""Circle""
                    area <- 3.14159 * radius * radius
                }
            }
            class Rectangle < Shape {
                func Init(width:double, height:double) {
                    type <- ""Rectangle""
                    area <- width * height
                }
            }
            func CreateCircle(radius:double) -> Shape {
                return Circle(radius)
            }
            func CreateRectangle(width:double, height:double) -> Shape {
                return Rectangle(width, height)
            }
            shape1 <- CreateCircle(5.0)
            shape2 <- CreateRectangle(10.0, 20.0)
            info1 <- shape1.GetInfo()
            info2 <- shape2.GetInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var info1 = interpreter.Manager.GetValue(new LangId("info1"));
        var info2 = interpreter.Manager.GetValue(new LangId("info2"));

        Assert.NotNull(info1);
        Assert.IsType<StringLangValue>(info1);
        Assert.Contains("Circle", ((StringLangValue)info1).Value);

        Assert.NotNull(info2);
        Assert.IsType<StringLangValue>(info2);
        Assert.Contains("Rectangle", ((StringLangValue)info2).Value);
    }

    [Fact]
    public void ClassInstantiation_NullInstances_HandlesNullObjectReferences()
    {
        // Arrange
        var code = @"
            class Node {
                public value:int
                public next:Node
                func Init(v:int) {
                    value <- v
                    next <- null
                }
            }
            node1 <- Node(10)
            node2 <- Node(20)
            node1.next <- node2
            node3 <- Node(30)
            // node3.next remains null
            nextValue1 <- node1.next.value
            nextValue2 <- node3.next
            isNull <- nextValue2 == null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var nextValue1 = interpreter.Manager.GetValue(new LangId("nextValue1"));
        var nextValue2 = interpreter.Manager.GetValue(new LangId("nextValue2"));
        var isNull = interpreter.Manager.GetValue(new LangId("isNull"));

        Assert.NotNull(nextValue1);
        Assert.IsType<IntLangValue>(nextValue1);
        Assert.Equal(20, ((IntLangValue)nextValue1).Value);

        Assert.NotNull(isNull);
        Assert.IsType<BoolLangValue>(isNull);
        Assert.True(((BoolLangValue)isNull).Value);
    }

    [Fact]
    public void ClassInstantiation_SelfReference_HandlesSelfReferences()
    {
        // Arrange
        var code = @"
            class LinkedList {
                public value:int
                public next:LinkedList
                func Init(v:int) {
                    value <- v
                    next <- null
                }
                func Add(newValue:int) -> LinkedList {
                    newNode <- LinkedList(newValue)
                    next <- newNode
                    return newNode
                }
                func GetSum() -> int {
                    sum <- value
                    current <- next
                    while current != null {
                        sum <- sum + current.value
                        current <- current.next
                    }
                    return sum
                }
            }
            head <- LinkedList(1)
            node2 <- head.Add(2)
            node3 <- node2.Add(3)
            sum <- head.GetSum()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var sum = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.NotNull(sum);
        Assert.IsType<IntLangValue>(sum);
        Assert.Equal(6, ((IntLangValue)sum).Value); // 1+2+3 = 6
    }

    [Fact]
    public void ClassInstantiation_ObjectCloning_ClonesObjectsCorrectly()
    {
        // Arrange
        var code = @"
            class DataPoint {
                public x:double
                public y:double
                public label:string
                func Init(xPos:double, yPos:double, lbl:string) {
                    x <- xPos
                    y <- yPos
                    label <- lbl
                }
                func Clone() -> DataPoint {
                    return DataPoint(x, y, label)
                }
            }
            original <- DataPoint(3.14, 2.71, ""PI"")
            clone <- original.Clone()
            // Modify clone
            clone.x <- 6.28
            clone.label <- ""2PI""
            originalX <- original.x
            cloneX <- clone.x
            originalLabel <- original.label
            cloneLabel <- clone.label
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var origX = interpreter.Manager.GetValue(new LangId("originalX"));
        var clonX = interpreter.Manager.GetValue(new LangId("cloneX"));
        var origLabel = interpreter.Manager.GetValue(new LangId("originalLabel"));
        var clonLabel = interpreter.Manager.GetValue(new LangId("cloneLabel"));

        Assert.NotNull(origX);
        Assert.IsType<DoubleLangValue>(origX);
        Assert.Equal(3.14, ((DoubleLangValue)origX).Value);

        Assert.NotNull(clonX);
        Assert.IsType<DoubleLangValue>(clonX);
        Assert.Equal(6.28, ((DoubleLangValue)clonX).Value);

        Assert.NotNull(origLabel);
        Assert.IsType<StringLangValue>(origLabel);
        Assert.Equal("PI", ((StringLangValue)origLabel).Value);

        Assert.NotNull(clonLabel);
        Assert.IsType<StringLangValue>(clonLabel);
        Assert.Equal("2PI", ((StringLangValue)clonLabel).Value);
    }

    [Fact]
    public void ClassInstantiation_PolymorphicInstances_HandlesPolymorphicCreation()
    {
        // Arrange
        var code = @"
            class Animal {
                public name:string
                func Init(n:string) {
                    name <- n
                }
                func MakeSound() -> string {
                    return ""Generic animal sound""
                }
            }
            class Dog < Animal {
                func Init(n:string) {
                    name <- n
                }
                func MakeSound() -> string {
                    return ""Woof!""
                }
            }
            class Cat < Animal {
                func Init(n:string) {
                    name <- n
                }
                func MakeSound() -> string {
                    return ""Meow!""
                }
            }
            func CreateAnimal(type:string, name:string) -> Animal {
                if type == ""dog"" {
                    return Dog(name)
                } else if type == ""cat"" {
                    return Cat(name)
                } else {
                    return Animal(name)
                }
            }
            pet1 <- CreateAnimal(""dog"", ""Buddy"")
            pet2 <- CreateAnimal(""cat"", ""Whiskers"")
            pet3 <- CreateAnimal(""unknown"", ""Creature"")
            sound1 <- pet1.MakeSound()
            sound2 <- pet2.MakeSound()
            sound3 <- pet3.MakeSound()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var sound1 = interpreter.Manager.GetValue(new LangId("sound1"));
        var sound2 = interpreter.Manager.GetValue(new LangId("sound2"));
        var sound3 = interpreter.Manager.GetValue(new LangId("sound3"));

        Assert.NotNull(sound1);
        Assert.IsType<StringLangValue>(sound1);
        Assert.Equal("Woof!", ((StringLangValue)sound1).Value);

        Assert.NotNull(sound2);
        Assert.IsType<StringLangValue>(sound2);
        Assert.Equal("Meow!", ((StringLangValue)sound2).Value);

        Assert.NotNull(sound3);
        Assert.IsType<StringLangValue>(sound3);
        Assert.Equal("Generic animal sound", ((StringLangValue)sound3).Value);
    }
}