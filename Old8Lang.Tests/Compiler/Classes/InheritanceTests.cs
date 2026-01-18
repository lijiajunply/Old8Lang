using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 编译器模式下的高级类功能测试 - 继承
/// </summary>
public class InheritanceTests
{
    private readonly ITestOutputHelper _output;

    public InheritanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicInheritance_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Animal {
                public name <- """"
                
                func init(name:string) {
                    this.name <- name
                }
                
                func speak() -> string {
                    return ""Some sound""
                }
            }
            
            class Dog : Animal {
                public breed <- """"
                
                func init(name:string, breed:string) {
                    super.init(name)
                    this.breed <- breed
                }
                
                func speak() -> string {
                    return ""Woof""
                }
                
                func fetch() -> string {
                    return ""Fetching""
                }
            }
            
            dog <- Dog(""Buddy"", ""Golden Retriever"")
            Assert.Equal(""Buddy"", dog.name)
            Assert.Equal(""Golden Retriever"", dog.breed)
            Assert.Equal(""Woof"", dog.speak())
            Assert.Equal(""Fetching"", dog.fetch())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InheritanceWithMethodOverriding_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Shape {
                func area() -> double {
                    return 0.0
                }
                
                func perimeter() -> double {
                    return 0.0
                }
            }
            
            class Rectangle : Shape {
                public width <- 0.0
                public height <- 0.0
                
                func init(width:double, height:double) {
                    this.width <- width
                    this.height <- height
                }
                
                func area() -> double {
                    return this.width * this.height
                }
                
                func perimeter() -> double {
                    return 2 * (this.width + this.height)
                }
            }
            
            class Circle : Shape {
                public radius <- 0.0
                
                func init(radius:double) {
                    this.radius <- radius
                }
                
                func area() -> double {
                    return 3.14159 * this.radius * this.radius
                }
                
                func perimeter() -> double {
                    return 2 * 3.14159 * this.radius
                }
            }
            
            rect <- Rectangle(5.0, 3.0)
            circle <- Circle(2.0)
            
            Assert.Equal(15.0, rect.area())
            Assert.Equal(16.0, rect.perimeter())
            Assert.True(circle.area() > 12.5 && circle.area() < 12.6)
            Assert.True(circle.perimeter() > 12.5 && circle.perimeter() < 12.7)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MultiLevelInheritance_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Vehicle {
                public speed <- 0
                
                func init(speed:int) {
                    this.speed <- speed
                }
                
                func move() -> string {
                    return ""Moving at "" + this.speed.ToStr() + "" km/h""
                }
            }
            
            class Car : Vehicle {
                public fuel <- 0
                
                func init(speed:int, fuel:int) {
                    super.init(speed)
                    this.fuel <- fuel
                }
                
                func drive() -> string {
                    return ""Driving with "" + this.fuel.ToStr() + ""L fuel""
                }
            }
            
            class ElectricCar : Car {
                public battery <- 0
                
                func init(speed:int, fuel:int, battery:int) {
                    super.init(speed, fuel)
                    this.battery <- battery
                }
                
                func charge() -> string {
                    return ""Charging to "" + this.battery.ToStr() + ""%""
                }
            }
            
            ev <- ElectricCar(100, 0, 80)
            Assert.Equal(100, ev.speed)
            Assert.Equal(0, ev.fuel)
            Assert.Equal(80, ev.battery)
            Assert.Equal(""Moving at 100 km/h"", ev.move())
            Assert.Equal(""Driving with 0L fuel"", ev.drive())
            Assert.Equal(""Charging to 80%"", ev.charge())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InheritanceWithProtectedAccess_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class BaseClass {
                protected value <- 0
                
                func init(v:int) {
                    this.value <- v
                }
                
                func getValue() -> int {
                    return this.value
                }
                
                func setValue(v:int) -> void {
                    this.value <- v
                }
            }
            
            class DerivedClass : BaseClass {
                public func doubleValue() -> int {
                    return this.value * 2
                }
                
                public func addValue(v:int) -> void {
                    this.value <- this.value + v
                }
            }
            
            derived <- DerivedClass(10)
            Assert.Equal(10, derived.getValue())
            Assert.Equal(20, derived.doubleValue())
            
            derived.addValue(5)
            Assert.Equal(15, derived.getValue())
            Assert.Equal(30, derived.doubleValue())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InheritanceWithSuperMethodCall_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Parent {
                func greet() -> string {
                    return ""Hello from Parent""
                }
                
                func process() -> string {
                    return ""Parent processing""
                }
            }
            
            class Child : Parent {
                func greet() -> string {
                    parentGreeting <- super.greet()
                    return parentGreeting + "" and Child""
                }
                
                func process() -> string {
                    parentResult <- super.process()
                    return parentResult + "" enhanced by Child""
                }
            }
            
            child <- Child()
            Assert.Equal(""Hello from Parent and Child"", child.greet())
            Assert.Equal(""Parent processing enhanced by Child"", child.process())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InheritanceWithPolymorphism_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Employee {
                public name <- """"
                
                func init(name:string) {
                    this.name <- name
                }
                
                func calculateSalary() -> double {
                    return 30000.0
                }
            }
            
            class Manager : Employee {
                public bonus <- 0.0
                
                func init(name:string, bonus:double) {
                    super.init(name)
                    this.bonus <- bonus
                }
                
                func calculateSalary() -> double {
                    baseSalary <- super.calculateSalary()
                    return baseSalary + this.bonus
                }
            }
            
            class Developer : Employee {
                public overtimeHours <- 0
                
                func init(name:string, overtimeHours:int) {
                    super.init(name)
                    this.overtimeHours <- overtimeHours
                }
                
                func calculateSalary() -> double {
                    baseSalary <- super.calculateSalary()
                    overtimePay <- this.overtimeHours * 100.0
                    return baseSalary + overtimePay
                }
            }
            
            emp <- Employee(""John"")
            mgr <- Manager(""Alice"", 10000.0)
            dev <- Developer(""Bob"", 20)
            
            Assert.Equal(30000.0, emp.calculateSalary())
            Assert.Equal(40000.0, mgr.calculateSalary())
            Assert.Equal(32000.0, dev.calculateSalary())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InheritanceWithConstructorChaining_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Grandparent {
                public generation <- 1
                
                func init() {
                    this.generation <- 1
                }
                
                func getGeneration() -> string {
                    return ""Grandparent""
                }
            }
            
            class Parent : Grandparent {
                public parentField <- ""parent""
                
                func init() {
                    super.init()
                    this.generation <- 2
                }
                
                func getGeneration() -> string {
                    return ""Parent""
                }
            }
            
            class Child : Parent {
                public childField <- ""child""
                
                func init() {
                    super.init()
                    this.generation <- 3
                }
                
                func getGeneration() -> string {
                    return ""Child""
                }
            }
            
            child <- Child()
            Assert.Equal(3, child.generation)
            Assert.Equal(""parent"", child.parentField)
            Assert.Equal(""child"", child.childField)
            Assert.Equal(""Child"", child.getGeneration())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InheritanceWithAbstractMethod_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Shape {
                func area() -> double {
                    return 0.0
                }
            }
            
            class Triangle : Shape {
                public base <- 0.0
                public height <- 0.0
                
                func init(base:double, height:double) {
                    this.base <- base
                    this.height <- height
                }
                
                func area() -> double {
                    return 0.5 * this.base * this.height
                }
            }
            
            triangle <- Triangle(10.0, 5.0)
            Assert.Equal(25.0, triangle.area())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MultipleInstancesWithInheritance_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Animal {
                public name <- """"
                
                func init(name:string) {
                    this.name <- name
                }
                
                func speak() -> string {
                    return ""Sound""
                }
            }
            
            class Cat : Animal {
                func speak() -> string {
                    return ""Meow""
                }
            }
            
            cat1 <- Cat(""Whiskers"")
            cat2 <- Cat(""Mittens"")
            cat3 <- Cat(""Fluffy"")
            
            Assert.Equal(""Whiskers"", cat1.name)
            Assert.Equal(""Mittens"", cat2.name)
            Assert.Equal(""Fluffy"", cat3.name)
            Assert.Equal(""Meow"", cat1.speak())
            Assert.Equal(""Meow"", cat2.speak())
            Assert.Equal(""Meow"", cat3.speak())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InheritanceWithStaticFields_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Base {
                public static count <- 0
                
                public static func increment() -> int {
                    Base.count <- Base.count + 1
                    return Base.count
                }
            }
            
            class Derived : Base {
                public static func doubleIncrement() -> int {
                    Base.count <- Base.count + 2
                    return Base.count
                }
            }
            
            result1 <- Derived.increment()
            result2 <- Derived.doubleIncrement()
            result3 <- Derived.increment()
            
            Assert.Equal(1, result1)
            Assert.Equal(3, result2)
            Assert.Equal(4, result3)
            Assert.Equal(4, Base.count)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
