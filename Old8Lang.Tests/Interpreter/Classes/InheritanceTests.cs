using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// 继承和接口解释模式测试
/// </summary>
public class InheritanceTests
{
    [Fact]
    public void ClassInheritance_BasicInheritance_InheritsMethods()
    {
        // Arrange
        var code = @"
            class Animal {
                public name <- """"

                func init(n:string) {
                    name <- n
                }

                func speak() -> string {
                    return ""Some sound""
                }

                func getName() -> string {
                    return name
                }
            }

            class Dog extends Animal {
                func speak() -> string {
                    return ""Woof!""
                }

                func wagTail() -> string {
                    return ""Wagging tail""
                }
            }

            dog <- Dog(""Buddy"")
            sound <- dog.speak()
            tailAction <- dog.wagTail()
            dogName <- dog.getName()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var sound = interpreter.Manager.GetValue(new LangId("sound"));
        var tailAction = interpreter.Manager.GetValue(new LangId("tailAction"));
        var dogName = interpreter.Manager.GetValue(new LangId("dogName"));

        Assert.NotNull(sound);
        Assert.NotNull(tailAction);
        Assert.NotNull(dogName);

        Assert.IsType<StringLangValue>(sound);
        Assert.IsType<StringLangValue>(tailAction);
        Assert.IsType<StringLangValue>(dogName);

        Assert.Equal("Woof!", ((StringLangValue)sound).Value);
        Assert.Equal("Wagging tail", ((StringLangValue)tailAction).Value);
        Assert.Equal("Buddy", ((StringLangValue)dogName).Value);
    }

    [Fact]
    public void ClassInheritance_MultipleInheritanceLevels_AccessesAncestorMethods()
    {
        // Arrange
        var code = @"
            class Vehicle {
                public speed <- 0
                public brand <- """"

                func init(b:string) {
                    brand <- b
                }

                func accelerate(amount) {
                    speed <- speed + amount
                }

                func getInfo() -> string {
                    return brand + "" vehicle at "" + speed + "" km/h""
                }
            }

            class Car extends Vehicle {
                public doors <- 4

                func openDoors() -> string {
                    return ""Opening "" + doors + "" doors""
                }

                func getInfo() -> string {
                    return brand + "" car with "" + doors + "" doors at "" + speed + "" km/h""
                }
            }

            class SportsCar extends Car {
                func accelerate(amount) {
                    speed <- speed + amount * 2  // Sports cars accelerate faster
                }

                func turboBoost() -> string {
                    speed <- speed + 50
                    return ""Turbo boost activated!""
                }
            }

            sportsCar <- SportsCar(""Ferrari"")
            sportsCar.doors <- 2
            sportsCar.accelerate(30)
            carInfo <- sportsCar.getInfo()
            turboResult <- sportsCar.turboBoost()
            finalInfo <- sportsCar.getInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var carInfo = interpreter.Manager.GetValue(new LangId("carInfo"));
        var turboResult = interpreter.Manager.GetValue(new LangId("turboResult"));
        var finalInfo = interpreter.Manager.GetValue(new LangId("finalInfo"));

        Assert.NotNull(carInfo);
        Assert.NotNull(turboResult);
        Assert.NotNull(finalInfo);

        Assert.IsType<StringLangValue>(carInfo);
        Assert.IsType<StringLangValue>(turboResult);
        Assert.IsType<StringLangValue>(finalInfo);

        Assert.Equal("Ferrari car with 2 doors at 60 km/h", ((StringLangValue)carInfo).Value); // 30 * 2 = 60
        Assert.Equal("Turbo boost activated!", ((StringLangValue)turboResult).Value);
        Assert.Equal("Ferrari car with 2 doors at 110 km/h", ((StringLangValue)finalInfo).Value); // 60 + 50 = 110
    }

    [Fact]
    public void ClassInheritance_Polymorphism_CallsCorrectMethods()
    {
        // Arrange
        var code = @"
            class Shape {
                func getArea() -> double {
                    return 0.0
                }

                func getPerimeter() -> double {
                    return 0.0
                }

                func getType() -> string {
                    return ""Generic Shape""
                }
            }

            class Circle extends Shape {
                public radius <- 0.0

                func init(r) {
                    radius <- r
                }

                func getArea() -> double {
                    return 3.14159 * radius * radius
                }

                func getPerimeter() -> double {
                    return 2 * 3.14159 * radius
                }

                func getType() -> string {
                    return ""Circle""
                }
            }

            class Rectangle extends Shape {
                public width <- 0.0
                public height <- 0.0

                func init(w, h) {
                    width <- w
                    height <- h
                }

                func getArea() -> double {
                    return width * height
                }

                func getPerimeter() -> double {
                    return 2 * (width + height)
                }

                func getType() -> string {
                    return ""Rectangle""
                }
            }

            circle <- Circle(5.0)
            rectangle <- Rectangle(4.0, 6.0)

            circleArea <- circle.getArea()
            circlePerimeter <- circle.getPerimeter()
            circleType <- circle.getType()

            rectArea <- rectangle.getArea()
            rectPerimeter <- rectangle.getPerimeter()
            rectType <- rectangle.getType()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var circleArea = interpreter.Manager.GetValue(new LangId("circleArea"));
        var circlePerimeter = interpreter.Manager.GetValue(new LangId("circlePerimeter"));
        var circleType = interpreter.Manager.GetValue(new LangId("circleType"));
        var rectArea = interpreter.Manager.GetValue(new LangId("rectArea"));
        var rectPerimeter = interpreter.Manager.GetValue(new LangId("rectPerimeter"));
        var rectType = interpreter.Manager.GetValue(new LangId("rectType"));

        Assert.NotNull(circleArea);
        Assert.NotNull(circlePerimeter);
        Assert.NotNull(circleType);
        Assert.NotNull(rectArea);
        Assert.NotNull(rectPerimeter);
        Assert.NotNull(rectType);

        Assert.IsType<DoubleLangValue>(circleArea);
        Assert.IsType<DoubleLangValue>(circlePerimeter);
        Assert.IsType<StringLangValue>(circleType);
        Assert.IsType<DoubleLangValue>(rectArea);
        Assert.IsType<DoubleLangValue>(rectPerimeter);
        Assert.IsType<StringLangValue>(rectType);

        Assert.True(Math.Abs(78.53975 - ((DoubleLangValue)circleArea).Value) < 0.01);
        Assert.True(Math.Abs(31.4159 - ((DoubleLangValue)circlePerimeter).Value) < 0.01);
        Assert.Equal("Circle", ((StringLangValue)circleType).Value);
        Assert.Equal(24.0, ((DoubleLangValue)rectArea).Value);
        Assert.Equal(20.0, ((DoubleLangValue)rectPerimeter).Value);
        Assert.Equal("Rectangle", ((StringLangValue)rectType).Value);
    }

    [Fact]
    public void InterfaceDeclaration_BasicInterface_ImplementsCorrectly()
    {
        // Arrange
        var code = @"
            interface IDrawable {
                func draw() -> string
                func getArea() -> double
            }

            interface IMovable {
                func move(x, y)
                func getPosition() -> string
            }

            class Circle implements IDrawable, IMovable {
                public radius <- 5.0
                private x <- 0.0
                private y <- 0.0

                func draw() -> string {
                    return ""Drawing circle at ("" + x + "", "" + y + "") with radius "" + radius
                }

                func getArea() -> double {
                    return 3.14159 * radius * radius
                }

                func move(x, y) {
                    this.x <- x
                    this.y <- y
                }

                func getPosition() -> string {
                    return ""("" + x + "", "" + y + "")""
                }
            }

            circle <- Circle()
            circle.move(10, 20)
            drawResult <- circle.draw()
            areaResult <- circle.getArea()
            positionResult <- circle.getPosition()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var drawResult = interpreter.Manager.GetValue(new LangId("drawResult"));
        var areaResult = interpreter.Manager.GetValue(new LangId("areaResult"));
        var positionResult = interpreter.Manager.GetValue(new LangId("positionResult"));

        Assert.NotNull(drawResult);
        Assert.NotNull(areaResult);
        Assert.NotNull(positionResult);

        Assert.IsType<StringLangValue>(drawResult);
        Assert.IsType<DoubleLangValue>(areaResult);
        Assert.IsType<StringLangValue>(positionResult);

        Assert.Contains("Drawing circle at (10, 20)", ((StringLangValue)drawResult).Value);
        Assert.True(Math.Abs(78.53975 - ((DoubleLangValue)areaResult).Value) < 0.01);
        Assert.Contains("(10, 20)", ((StringLangValue)positionResult).Value);
    }

    [Fact]
    public void ClassInheritance_WithInterface_CombinesFeatures()
    {
        // Arrange
        var code = @"
            interface ISerializable {
                func serialize() -> string
                func deserialize(data:string)
            }

            interface ILoggable {
                func log(message:string) -> string
                func getLogs() -> string
            }

            class Product implements ISerializable, ILoggable {
                public name <- """"
                public price <- 0.0
                private logs <- """"

                func init(n:string, p:double) {
                    name <- n
                    price <- p
                }

                func serialize() -> string {
                    return ""{""name"":"""" + name + """", ""price"": """" + price + """"}""
                }

                func deserialize(data:string) {
                    // Simplified parsing
                    logs <- logs + ""Deserialized: "" + data + ""\n""
                }

                func log(message:string) -> string {
                    logs <- logs + ""[LOG] "" + message + ""\n""
                    return ""Logged: "" + message
                }

                func getLogs() -> string {
                    return logs
                }
            }

            product <- Product(""Laptop"", 999.99)
            logResult <- product.log(""Product created"")
            serializedData <- product.serialize()
            product.deserialize(serializedData)
            allLogs <- product.getLogs()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var logResult = interpreter.Manager.GetValue(new LangId("logResult"));
        var serializedData = interpreter.Manager.GetValue(new LangId("serializedData"));
        var allLogs = interpreter.Manager.GetValue(new LangId("allLogs"));

        Assert.NotNull(logResult);
        Assert.NotNull(serializedData);
        Assert.NotNull(allLogs);

        Assert.IsType<StringLangValue>(logResult);
        Assert.IsType<StringLangValue>(serializedData);
        Assert.IsType<StringLangValue>(allLogs);

        Assert.Equal("Logged: Product created", ((StringLangValue)logResult).Value);
        Assert.Contains("Laptop", ((StringLangValue)serializedData).Value);
        Assert.Contains("999.99", ((StringLangValue)serializedData).Value);
        Assert.Contains("[LOG] Product created", ((StringLangValue)allLogs).Value);
        Assert.Contains("Deserialized:", ((StringLangValue)allLogs).Value);
    }

    [Fact]
    public void ClassInheritance_AbstractMethods_ImplementationRequired()
    {
        // Arrange
        var code = @"
            class Animal {
                public name <- """"

                func init(n:string) {
                    name <- n
                }

                func speak() -> string {
                    return ""Abstract animal sound""
                }
            }

            class Dog extends Animal {
                func speak() -> string {
                    return ""Woof! My name is "" + name
                }

                func fetch() -> string {
                    return ""Fetching the ball!""
                }
            }

            class Cat extends Animal {
                func speak() -> string {
                    return ""Meow! I'm "" + name
                }

                func purr() -> string {
                    return ""Purring contentedly""
                }
            }

            dog <- Dog(""Rex"")
            cat <- Cat(""Whiskers"")

            dogSound <- dog.speak()
            dogAction <- dog.fetch()

            catSound <- cat.speak()
            catAction <- cat.purr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var dogSound = interpreter.Manager.GetValue(new LangId("dogSound"));
        var dogAction = interpreter.Manager.GetValue(new LangId("dogAction"));
        var catSound = interpreter.Manager.GetValue(new LangId("catSound"));
        var catAction = interpreter.Manager.GetValue(new LangId("catAction"));

        Assert.NotNull(dogSound);
        Assert.NotNull(dogAction);
        Assert.NotNull(catSound);
        Assert.NotNull(catAction);

        Assert.IsType<StringLangValue>(dogSound);
        Assert.IsType<StringLangValue>(dogAction);
        Assert.IsType<StringLangValue>(catSound);
        Assert.IsType<StringLangValue>(catAction);

        Assert.Equal("Woof! My name is Rex", ((StringLangValue)dogSound).Value);
        Assert.Equal("Fetching the ball!", ((StringLangValue)dogAction).Value);
        Assert.Equal("Meow! I'm Whiskers", ((StringLangValue)catSound).Value);
        Assert.Equal("Purring contentedly", ((StringLangValue)catAction).Value);
    }

    [Fact]
    public void ClassInheritance_MethodOverriding_CallsChildMethod()
    {
        // Arrange
        var code = @"
            class Employee {
                public name <- """"
                public salary <- 0.0

                func init(n:string, s:double) {
                    name <- n
                    salary <- s
                }

                func calculateBonus() -> double {
                    return salary * 0.1  // 10% bonus for regular employee
                }

                func getInfo() -> string {
                    return name + "" earns "" + salary + "" with bonus "" + calculateBonus()
                }
            }

            class Manager extends Employee {
                private teamSize <- 0

                func init(n:string, s:double, team:int) {
                    this.init(n, s)  // Call parent constructor
                    teamSize <- team
                }

                func calculateBonus() -> double {
                    return salary * 0.2 + teamSize * 100  // 20% bonus + team bonus
                }

                func getTeamSize() -> int {
                    return teamSize
                }

                func getInfo() -> string {
                    return name + "" (Manager) with team of "" + teamSize + "" earns "" + salary + "" with bonus "" + calculateBonus()
                }
            }

            employee <- Employee(""John"", 50000)
            manager <- Manager(""Sarah"", 80000, 5)

            employeeInfo <- employee.getInfo()
            managerInfo <- manager.getInfo()
            teamSize <- manager.getTeamSize()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var employeeInfo = interpreter.Manager.GetValue(new LangId("employeeInfo"));
        var managerInfo = interpreter.Manager.GetValue(new LangId("managerInfo"));
        var teamSize = interpreter.Manager.GetValue(new LangId("teamSize"));

        Assert.NotNull(employeeInfo);
        Assert.NotNull(managerInfo);
        Assert.NotNull(teamSize);

        Assert.IsType<StringLangValue>(employeeInfo);
        Assert.IsType<StringLangValue>(managerInfo);
        Assert.IsType<IntLangValue>(teamSize);

        Assert.Contains("John earns 50000 with bonus 5000", ((StringLangValue)employeeInfo).Value);
        Assert.Contains("Sarah (Manager) with team of 5 earns 80000", ((StringLangValue)managerInfo).Value);
        Assert.Equal(5, ((IntLangValue)teamSize).Value);
    }

    [Fact]
    public void ClassInheritance_BaseClassConstructor_CalledCorrectly()
    {
        // Arrange
        var code = @"
            class Person {
                public name <- """"
                public age <- 0

                func init(n:string, a:int) {
                    name <- n
                    age <- a
                }

                func getBasicInfo() -> string {
                    return name + "" is "" + age + "" years old""
                }
            }

            class Student extends Person {
                public studentId <- """"
                public grade <- 0

                func init(n:string, a:int, id:string, g:int) {
                    this.init(n, a)  // Call parent constructor
                    studentId <- id
                    grade <- g
                }

                func getStudentInfo() -> string {
                    return getBasicInfo() + "", Student ID: "" + studentId + "", Grade: "" + grade
                }
            }

            student <- Student(""Alice"", 20, ""S12345"", 10)
            basicInfo <- student.getBasicInfo()
            studentInfo <- student.getStudentInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var basicInfo = interpreter.Manager.GetValue(new LangId("basicInfo"));
        var studentInfo = interpreter.Manager.GetValue(new LangId("studentInfo"));

        Assert.NotNull(basicInfo);
        Assert.NotNull(studentInfo);

        Assert.IsType<StringLangValue>(basicInfo);
        Assert.IsType<StringLangValue>(studentInfo);

        Assert.Equal("Alice is 20 years old", ((StringLangValue)basicInfo).Value);
        Assert.Equal("Alice is 20 years old, Student ID: S12345, Grade: 10", ((StringLangValue)studentInfo).Value);
    }

    [Fact]
    public void ClassInheritance_ProtectedMembers_AccessibleInChildClass()
    {
        // Arrange
        var code = @"
            class Vehicle {
                public model <- """"
                protected engineType <- ""unknown""

                func init(m:string, e:string) {
                    model <- m
                    engineType <- e
                }

                protected func getEngineInfo() -> string {
                    return ""Engine: "" + engineType
                }
            }

            class Car extends Vehicle {
                public doors <- 4

                func init(m:string, e:string, d:int) {
                    this.init(m, e)
                    doors <- d
                }

                func getFullInfo() -> string {
                    return model + "" with "" + doors + "" doors, "" + getEngineInfo()
                }
            }

            car <- Car(""Toyota"", ""V6"", 4)
            fullInfo <- car.getFullInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var fullInfo = interpreter.Manager.GetValue(new LangId("fullInfo"));
        Assert.NotNull(fullInfo);
        Assert.IsType<StringLangValue>(fullInfo);
        Assert.Equal("Toyota with 4 doors, Engine: V6", ((StringLangValue)fullInfo).Value);
    }

    [Fact]
    public void ClassInheritance_InterfaceInheritance_CombinesInterfaces()
    {
        // Arrange
        var code = @"
            interface IBasicShape {
                func getName() -> string
            }

            interface IAdvancedShape extends IBasicShape {
                func calculateComplexity() -> int
            }

            interface IRenderable {
                func render() -> string
            }

            class Triangle implements IAdvancedShape, IRenderable {
                public base <- 0
                public height <- 0

                func init(b, h) {
                    base <- b
                    height <- h
                }

                func getName() -> string {
                    return ""Triangle""
                }

                func calculateComplexity() -> int {
                    return 3  // 3 sides
                }

                func render() -> string {
                    return ""Rendering triangle with base "" + base + "" and height "" + height
                }
            }

            triangle <- Triangle(5, 8)
            name <- triangle.getName()
            complexity <- triangle.calculateComplexity()
            renderResult <- triangle.render()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var name = interpreter.Manager.GetValue(new LangId("name"));
        var complexity = interpreter.Manager.GetValue(new LangId("complexity"));
        var renderResult = interpreter.Manager.GetValue(new LangId("renderResult"));

        Assert.NotNull(name);
        Assert.NotNull(complexity);
        Assert.NotNull(renderResult);

        Assert.IsType<StringLangValue>(name);
        Assert.IsType<IntLangValue>(complexity);
        Assert.IsType<StringLangValue>(renderResult);

        Assert.Equal("Triangle", ((StringLangValue)name).Value);
        Assert.Equal(3, ((IntLangValue)complexity).Value);
        Assert.Contains("Rendering triangle with base 5 and height 8", ((StringLangValue)renderResult).Value);
    }
}