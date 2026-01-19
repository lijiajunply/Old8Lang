using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 编译器模式下的高级类功能测试 - 接口
/// </summary>
public class InterfaceTests
{
    private readonly ITestOutputHelper _output;

    public InterfaceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicInterface_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface IDrawable {
                func draw() -> string
            }
            
            class Circle implements IDrawable {
                public radius <- 0.0
                
                func init(radius:double) {
                    this.radius <- radius
                }
                
                func draw() -> string {
                    return ""Drawing circle with radius "" + this.radius.ToStr()
                }
            }
            
            class Square implements IDrawable {
                public side <- 0.0
                
                func init(side:double) {
                    this.side <- side
                }
                
                func draw() -> string {
                    return ""Drawing square with side "" + this.side.ToStr()
                }
            }
            
            circle <- Circle(5.0)
            square <- Square(10.0)
            
            Assert.Equal(""Drawing circle with radius 5"", circle.draw())
            Assert.Equal(""Drawing square with side 10"", square.draw())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InterfaceWithMultipleMethods_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface ICalculator {
                func add(a:int, b:int) -> int
                func subtract(a:int, b:int) -> int
                func multiply(a:int, b:int) -> int
                func divide(a:int, b:int) -> double
            }
            
            class BasicCalculator implements ICalculator {
                func add(a:int, b:int) -> int {
                    return a + b
                }
                
                func subtract(a:int, b:int) -> int {
                    return a - b
                }
                
                func multiply(a:int, b:int) -> int {
                    return a * b
                }
                
                func divide(a:int, b:int) -> double {
                    return a / b.ToDouble()
                }
            }
            
            calc <- BasicCalculator()
            
            Assert.Equal(15, calc.add(10, 5))
            Assert.Equal(5, calc.subtract(10, 5))
            Assert.Equal(50, calc.multiply(10, 5))
            Assert.Equal(2.0, calc.divide(10, 5))
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InterfaceInheritance_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface IAnimal {
                func speak() -> string
            }
            
            interface IPet extends IAnimal {
                func play() -> string
            }
            
            class Dog implements IPet {
                public name <- """"
                
                func init(name:string) {
                    this.name <- name
                }
                
                func speak() -> string {
                    return ""Woof""
                }
                
                func play() -> string {
                    return ""Playing fetch""
                }
            }
            
            dog <- Dog(""Buddy"")
            Assert.Equal(""Buddy"", dog.name)
            Assert.Equal(""Woof"", dog.speak())
            Assert.Equal(""Playing fetch"", dog.play())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MultipleInterfaceImplementation_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface IReadable {
                func read() -> string
            }
            
            interface IWritable {
                func write(content:string) -> void
            }
            
            interface IFile extends IReadable, IWritable {
                func getName() -> string
            }
            
            class TextFile implements IFile {
                public name <- """"
                public content <- """"
                
                func init(name:string) {
                    this.name <- name
                    this.content <- """"
                }
                
                func read() -> string {
                    return this.content
                }
                
                func write(content:string) -> void {
                    this.content <- content
                }
                
                func getName() -> string {
                    return this.name
                }
            }
            
            file <- TextFile(""example.txt"")
            Assert.Equal(""example.txt"", file.getName())
            Assert.Equal("""", file.read())
            
            file.write(""Hello, World!"")
            Assert.Equal(""Hello, World!"", file.read())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InterfaceWithProperties_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface IContainer {
                func getCapacity() -> int
                func getCurrentSize() -> int
                func isEmpty() -> bool
                func isFull() -> bool
            }
            
            class Box implements IContainer {
                public capacity <- 10
                public items <- {}
                
                func init(capacity:int) {
                    this.capacity <- capacity
                    this.items <- {}
                }
                
                func addItem(item:int) -> void {
                    if this.items.Count() < this.capacity {
                        this.items.Add(item)
                    }
                }
                
                func getCapacity() -> int {
                    return this.capacity
                }
                
                func getCurrentSize() -> int {
                    return this.items.Count()
                }
                
                func isEmpty() -> bool {
                    return this.items.Count() == 0
                }
                
                func isFull() -> bool {
                    return this.items.Count() >= this.capacity
                }
            }
            
            box <- Box(5)
            Assert.Equal(5, box.getCapacity())
            Assert.Equal(0, box.getCurrentSize())
            Assert.True(box.isEmpty())
            Assert.False(box.isFull())
            
            box.AddItem(1)
            box.AddItem(2)
            box.AddItem(3)
            
            Assert.Equal(5, box.getCapacity())
            Assert.Equal(3, box.getCurrentSize())
            Assert.False(box.isEmpty())
            Assert.False(box.isFull())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InterfaceWithDefaultImplementation_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface ILogger {
                func log(message:string) -> void {
                    PrintLine(""[LOG] "" + message)
                }
            }
            
            class ConsoleLogger implements ILogger {
                public func log(message:string) -> void {
                    PrintLine(""[CONSOLE] "" + message)
                }
            }
            
            class FileLogger implements ILogger {
            }
            
            consoleLogger <- ConsoleLogger()
            fileLogger <- FileLogger()
            
            consoleLogger.log(""Hello from console"")
            fileLogger.log(""Hello from file"")
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InterfacePolymorphism_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface IShape {
                func area() -> double
                func perimeter() -> double
            }
            
            class Rectangle implements IShape {
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
            
            class Triangle implements IShape {
                public base_len <- 0.0
                public height <- 0.0
                
                func init(base_len:double, height:double) {
                    this.base_len <- base_len
                    this.height <- height
                }
                
                func area() -> double {
                    return 0.5 * this.base_len * this.height
                }
                
                func perimeter() -> double {
                    return this.base_len * 3
                }
            }
            
            shapes <- {Rectangle(5.0, 3.0), Triangle(4.0, 3.0)}
            
            rectArea <- shapes[0].area()
            rectPerimeter <- shapes[0].perimeter()
            triArea <- shapes[1].area()
            triPerimeter <- shapes[1].perimeter()
            
            Assert.Equal(15.0, rectArea)
            Assert.Equal(16.0, rectPerimeter)
            Assert.Equal(6.0, triArea)
            Assert.Equal(12.0, triPerimeter)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InterfaceWithClassInheritance_CompilesAndExecutesCorrectly()
    {
        var code = @"
            class Animal {
                public name <- """"
                
                func init(name:string) {
                    this.name <- name
                }
                
                func getName() -> string {
                    return this.name
                }
            }
            
            interface ISwimmable {
                func swim() -> string
            }
            
            class Fish extends Animal implements ISwimmable {
                public func swim() -> string {
                    return ""Swimming in water""
                }
            }
            
            fish <- Fish(""Nemo"")
            Assert.Equal(""Nemo"", fish.getName())
            Assert.Equal(""Swimming in water"", fish.swim())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InterfaceWithStaticMethods_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface IFactory {
                public static func create(type:string) -> object
            }
            
            class WidgetFactory implements IFactory {
                public static func create(type:string) -> object {
                    return ""Created: "" + type
                }
            }
            
            result1 <- WidgetFactory.create(""WidgetA"")
            result2 <- WidgetFactory.create(""WidgetB"")
            
            Assert.Equal(""Created: WidgetA"", result1)
            Assert.Equal(""Created: WidgetB"", result2)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void InterfaceWithGenerics_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface IRepository {
                func save(item:object) -> void
                func getById(id:int) -> object?
                func getAll() -> list
            }
            
            class InMemoryRepository implements IRepository {
                private items <- {}
                private nextId <- 1
                
                func save(item:object) -> void {
                    this.items.Add(item)
                }
                
                func getById(id:int) -> object? {
                    if id >= 0 && id < this.items.Count() {
                        return this.items[id]
                    }
                    return null
                }
                
                func getAll() -> list {
                    return this.items
                }
            }
            
            repo <- InMemoryRepository()
            repo.save(""Item 1"")
            repo.save(""Item 2"")
            repo.save(""Item 3"")
            
            allItems <- repo.getAll()
            Assert.Equal(3, allItems.Count())
            
            item1 <- repo.getById(0)
            item2 <- repo.getById(1)
            item3 <- repo.getById(2)
            
            Assert.Equal(""Item 1"", item1)
            Assert.Equal(""Item 2"", item2)
            Assert.Equal(""Item 3"", item3)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
