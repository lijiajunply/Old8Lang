using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 编译器模式下的高级类功能测试 - Mixin
/// </summary>
public class MixinTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void BasicMixin_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixin Timestampable {
                public createdAt <- 0
                public updatedAt <- 0
                
                public func initTimestamp() -> void {
                    this.createdAt <- Time.Now()
                    this.updatedAt <- Time.Now()
                }
                
                public func updateTimestamp() -> void {
                    this.updatedAt <- Time.Now()
                }
                
                public func getAge() -> double {
                    return this.updatedAt - this.createdAt
                }
            }
            
            class Article with Timestampable {
                public title <- """"
                public content <- """"
                
                func init(title:string, content:string) {
                    this.initTimestamp()
                    this.title <- title
                    this.content <- content
                }
                
                public func updateContent(newContent:string) -> void {
                    this.content <- newContent
                    this.updateTimestamp()
                }
            }
            
            article <- Article(""Hello"", ""World"")
            
            Assert.Equal(""Hello"", article.title)
            Assert.Equal(""World"", article.content)
            Assert.True(article.createdAt > 0)
            Assert.True(article.updatedAt > 0)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MultipleMixins_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixin Identifiable {
                public id <- 0
                
                public func setId(newId:int) -> void {
                    this.id <- newId
                }
                
                public func getId() -> int {
                    return this.id
                }
            }
            
            mixin Nameable {
                public name <- """"
                
                public func setName(newName:string) -> void {
                    this.name <- newName
                }
                
                public func getName() -> string {
                    return this.name
                }
            }
            
            class User with Identifiable, Nameable {
                public email <- """"
                
                func init(id:int, name:string, email:string) {
                    this.setId(id)
                    this.setName(name)
                    this.email <- email
                }
                
                public func getEmail() -> string {
                    return this.email
                }
            }
            
            user <- User(1, ""Alice"", ""alice@example.com"")
            
            Assert.Equal(1, user.getId())
            Assert.Equal(""Alice"", user.getName())
            Assert.Equal(""alice@example.com"", user.getEmail())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixinWithInheritance_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixin Loggable {
                public logCount <- 0
                
                public func log(message:string) -> void {
                    this.logCount <- this.logCount + 1
                }
                
                public func getLogCount() -> int {
                    return this.logCount
                }
            }
            
            class Animal {
                public name <- """"
                
                func init(name:string) {
                    this.name <- name
                }
                
                public func speak() -> string {
                    return ""Sound""
                }
            }
            
            class Dog extends Animal with Loggable {
                public func speak() -> string {
                    this.log(""Dog barked"")
                    return ""Woof""
                }
            }
            
            dog <- Dog(""Buddy"")
            Assert.Equal(""Buddy"", dog.name)
            
            sound1 <- dog.speak()
            sound2 <- dog.speak()
            
            Assert.Equal(""Woof"", sound1)
            Assert.Equal(""Woof"", sound2)
            Assert.Equal(2, dog.getLogCount())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixinMethodOverride_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixin Comparable {
                public func equals(other:object) -> bool {
                    return this == other
                }
                
                public func toString() -> string {
                    return ""Comparable object""
                }
            }
            
            class Point with Comparable {
                public x <- 0
                public y <- 0
                
                func init(x:int, y:int) {
                    this.x <- x
                    this.y <- y
                }
                
                public func equals(other:Point) -> bool {
                    return this.x == other.x && this.y == other.y
                }
                
                public func toString() -> string {
                    return ""("" + this.x.ToStr() + "", "" + this.y.ToStr() + "")""
                }
            }
            
            point1 <- Point(5, 10)
            point2 <- Point(5, 10)
            point3 <- Point(3, 7)
            
            Assert.True(point1.equals(point2))
            Assert.False(point1.equals(point3))
            Assert.Equal(""(5, 10)"", point1.toString())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixinWithState_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixin Countable {
                private count <- 0
                
                public func increment() -> void {
                    this.count <- this.count + 1
                }
                
                public func decrement() -> void {
                    this.count <- this.count - 1
                }
                
                public func getCount() -> int {
                    return this.count
                }
                
                public func reset() -> void {
                    this.count <- 0
                }
            }
            
            class Counter with Countable {
                public name <- """"
                
                func init(name:string) {
                    this.name <- name
                }
            }
            
            counter <- Counter(""My Counter"")
            Assert.Equal(0, counter.getCount())
            
            counter.increment()
            counter.increment()
            counter.increment()
            
            Assert.Equal(3, counter.getCount())
            
            counter.decrement()
            Assert.Equal(2, counter.getCount())
            
            counter.reset()
            Assert.Equal(0, counter.getCount())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixinWithCollections_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixin CollectionHelper {
                public func isEmpty(list:list) -> bool {
                    return list.Count() == 0
                }
                
                public func first(list:list) -> object? {
                    if list.Count() > 0 {
                        return list[0]
                    }
                    return null
                }
                
                public func last(list:list) -> object? {
                    if list.Count() > 0 {
                        return list[list.Count() - 1]
                    }
                    return null
                }
            }
            
            class ListProcessor with CollectionHelper {
                public items <- {}
                
                func init() {
                    this.items <- {}
                }
                
                public func addItem(item:int) -> void {
                    this.items.Add(item)
                }
                
                public func getItems() -> list {
                    return this.items
                }
            }
            
            processor <- ListProcessor()
            Assert.True(processor.isEmpty(processor.getItems()))
            
            processor.addItem(1)
            processor.addItem(2)
            processor.addItem(3)
            
            Assert.False(processor.isEmpty(processor.getItems()))
            Assert.Equal(1, processor.first(processor.getItems()))
            Assert.Equal(3, processor.last(processor.getItems()))
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixinWithValidation_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixin Validatable {
                public errors <- {}
                
                public func addError(error:string) -> void {
                    this.errors.Add(error)
                }
                
                public func clearErrors() -> void {
                    this.errors <- {}
                }
                
                public func hasErrors() -> bool {
                    return this.errors.Count() > 0
                }
                
                public func getErrors() -> list {
                    return this.errors
                }
            }
            
            class User with Validatable {
                public name <- """"
                public email <- """"
                
                func init(name:string, email:string) {
                    this.name <- name
                    this.email <- email
                    this.validate()
                }
                
                public func validate() -> void {
                    this.clearErrors()
                    
                    if this.name == """" {
                        this.addError(""Name is required"")
                    }
                    
                    if this.email == """" {
                        this.addError(""Email is required"")
                    }
                }
            }
            
            user1 <- User("""", """")
            Assert.True(user1.hasErrors())
            Assert.Equal(2, user1.getErrors().Count())
            
            user2 <- User(""Alice"", ""alice@example.com"")
            Assert.False(user2.hasErrors())
            Assert.Equal(0, user2.getErrors().Count())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixinWithComputedProperties_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixin AreaCalculator {
                public func getArea() -> double {
                    return 0.0
                }
            }
            
            class Rectangle with AreaCalculator {
                public width <- 0.0
                public height <- 0.0
                
                func init(width:double, height:double) {
                    this.width <- width
                    this.height <- height
                }
                
                public func getArea() -> double {
                    return this.width * this.height
                }
            }
            
            class Circle with AreaCalculator {
                public radius <- 0.0
                
                func init(radius:double) {
                    this.radius <- radius
                }
                
                public func getArea() -> double {
                    return 3.14159 * this.radius * this.radius
                }
            }
            
            rect <- Rectangle(5.0, 3.0)
            circle <- Circle(2.0)
            
            Assert.Equal(15.0, rect.getArea())
            Assert.True(circle.getArea() > 12.5 && circle.getArea() < 12.6)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixinChaining_CompilesAndExecutesCorrectly()
    {
        var code = @"
            mixin BaseMixin {
                public baseValue <- 0
                
                public func setBaseValue(value:int) -> void {
                    this.baseValue <- value
                }
                
                public func getBaseValue() -> int {
                    return this.baseValue
                }
            }
            
            mixin ExtendedMixin : BaseMixin {
                public extendedValue <- 0
                
                public func setExtendedValue(value:int) -> void {
                    this.extendedValue <- value
                }
                
                public func getExtendedValue() -> int {
                    return this.extendedValue
                }
                
                public func getTotal() -> int {
                    return this.baseValue + this.extendedValue
                }
            }
            
            class Calculator with ExtendedMixin {
                public func init(base:int, extended:int) {
                    this.setBaseValue(base)
                    this.setExtendedValue(extended)
                }
            }
            
            calc <- Calculator(10, 20)
            
            Assert.Equal(10, calc.getBaseValue())
            Assert.Equal(20, calc.getExtendedValue())
            Assert.Equal(30, calc.getTotal())
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixinWithInterface_CompilesAndExecutesCorrectly()
    {
        var code = @"
            interface IProcessable {
                func process() -> string
            }
            
            mixin TimestampMixin {
                public timestamp <- 0
                
                public func setTimestamp() -> void {
                    this.timestamp <- Time.Now()
                }
            }
            
            class Task implements IProcessable with TimestampMixin {
                public name <- """"
                
                func init(name:string) {
                    this.name <- name
                    this.setTimestamp()
                }
                
                public func process() -> string {
                    return ""Processed: "" + this.name
                }
                
                public func getTimestamp() -> double {
                    return this.timestamp
                }
            }
            
            task <- Task(""My Task"")
            
            Assert.Equal(""My Task"", task.name)
            Assert.Equal(""Processed: My Task"", task.process())
            Assert.True(task.getTimestamp() > 0)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
