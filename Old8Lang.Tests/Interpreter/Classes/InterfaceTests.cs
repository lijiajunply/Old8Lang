using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Classes;

/// <summary>
/// 接口解释模式测试
/// </summary>
public class InterfaceTests
{
    [Fact]
    public void Interface_SimpleInterface_DeclaresCorrectly()
    {
        // Arrange
        var code = @"
            interface Drawable {
                func Draw() -> void
            }

            class Circle < Drawable {
                public radius <- 0

                func Circle(r:double) {
                    radius <- r
                }

                func Draw() -> void {
                    // Implementation would draw the circle
                }
            }

            circle <- Circle(5.0)
            result <- circle.radius
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(5.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void _Interface_MultipleInterfaces_ImplementsMultipleInterfaces()
    {
        // Arrange
        var code = @"
            interface Shape {
                func GetArea() -> double
            }

            interface Movable {
                func Move(dx:double, dy:double) -> void
            }

            class Rectangle < Shape, Movable {
                public width <- 0
                public height <- 0
                public x <- 0
                public y <- 0

                func Rectangle(w:double, h:double) {
                    width <- w
                    height <- h
                }

                func GetArea() -> double {
                    return width * height
                }

                func Move(dx:double, dy:double) -> void {
                    x <- x + dx
                    y <- y + dy
                }
            }

            rect <- Rectangle(10.0, 5.0)
            rect.Move(2.0, 3.0)
            area <- rect.GetArea()
            finalX <- rect.x
            finalY <- rect.y
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var area = interpreter.Manager.GetValue(new LangId("area"));
        var finalX = interpreter.Manager.GetValue(new LangId("finalX"));
        var finalY = interpreter.Manager.GetValue(new LangId("finalY"));

        Assert.NotNull(area);
        Assert.IsType<DoubleLangValue>(area);
        Assert.Equal(50.0, ((DoubleLangValue)area).Value);

        Assert.NotNull(finalX);
        Assert.IsType<DoubleLangValue>(finalX);
        Assert.Equal(2.0, ((DoubleLangValue)finalX).Value);

        Assert.NotNull(finalY);
        Assert.IsType<DoubleLangValue>(finalY);
        Assert.Equal(3.0, ((DoubleLangValue)finalY).Value);
    }

    [Fact]
    public void Interface_Inheritance_ExtendsOtherInterfaces()
    {
        // Arrange
        var code = @"
            interface Shape {
                func GetArea() -> double
            }

            interface SolidShape < Shape {
                func GetVolume() -> double
            }

            class Cube < SolidShape {
                public side <- 0

                func Cube(s:double) {
                    side <- s
                }

                func GetArea() -> double {
                    return 6 * side * side
                }

                func GetVolume() -> double {
                    return side ^ 3
                }
            }

            cube <- Cube(3.0)
            surfaceArea <- cube.GetArea()
            volume <- cube.GetVolume()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var surfaceArea = interpreter.Manager.GetValue(new LangId("surfaceArea"));
        var volume = interpreter.Manager.GetValue(new LangId("volume"));

        Assert.NotNull(surfaceArea);
        Assert.IsType<DoubleLangValue>(surfaceArea);
        Assert.Equal(54.0, ((DoubleLangValue)surfaceArea).Value); // 6 * 3 * 3

        Assert.NotNull(volume);
        Assert.IsType<DoubleLangValue>(volume);
        Assert.Equal(27.0, ((DoubleLangValue)volume).Value); // 3^3
    }

    [Fact]
    public void Interface_DefaultMethods_ImplementsDefaultBehavior()
    {
        // Arrange
        var code = @"
            interface Loggable {
                func Log(message:string) -> void {
                    // Default implementation
                    PrintLine(""[LOG] "" + message)
                }
            }

            class User < Loggable {
                public name <- """"

                func User(n:string) {
                    name <- n
                }

                func Log(message:string) -> void {
                    // Override default implementation
                    PrintLine(""[USER: "" + name + ""] "" + message)
                }
            }

            class Product < Loggable {
                public id <- 0
                public name <- """"

                func Product(i:int, n:string) {
                    id <- i
                    name <- n
                }
            }

            user <- User(""Alice"")
            product <- Product(123, ""Widget"")

            // This would use the overridden method
            user.Log(""Login successful"")
            // This would use the default implementation
            product.Log(""Product created"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert - The test mainly checks that the interface declarations compile and work
        var userName = interpreter.Manager.GetValue(new LangId("user.name"));
        var productId = interpreter.Manager.GetValue(new LangId("product.id"));

        Assert.NotNull(userName);
        Assert.IsType<StringLangValue>(userName);
        Assert.Equal("Alice", ((StringLangValue)userName).Value);

        Assert.NotNull(productId);
        Assert.IsType<IntLangValue>(productId);
        Assert.Equal(123, ((IntLangValue)productId).Value);
    }

    [Fact]
    public void Interface_Polymorphism_UseInterfaceAsType()
    {
        // Arrange
        var code = @"
            interface Drawable {
                func Draw() -> string
            }

            class Circle < Drawable {
                public radius <- 0

                func Circle(r:double) {
                    radius <- r
                }

                func Draw() -> string {
                    return ""Drawing circle with radius "" + radius.ToStr()
                }
            }

            class Square < Drawable {
                public side <- 0

                func Square(s:double) {
                    side <- s
                }

                func Draw() -> string {
                    return ""Drawing square with side "" + side.ToStr()
                }
            }

            shapes <- {}
            shapes.Push(Circle(5.0))
            shapes.Push(Square(3.0))

            results <- {}
            for shape in shapes {
                results.Push(shape.Draw())
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var results = interpreter.Manager.GetValue(new LangId("results"));
        Assert.NotNull(results);
        // Implementation would check that polymorphic calls work correctly
    }

    [Fact]
    public void Interface_WithProperties_DefinesPropertyContracts()
    {
        // Arrange
        var code = @"
            interface Identifiable {
                Id <- string
                func GetInfo() -> string
            }

            class Employee < Identifiable {
                public Id <- """"
                public name <- """"

                func Employee(id:string, n:string) {
                    Id <- id
                    name <- n
                }

                func GetInfo() -> string {
                    return Id + "": "" + name
                }
            }

            employee <- Employee(""EMP001"", ""John Doe"")
            result1 <- employee.Id
            result2 <- employee.GetInfo()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("EMP001", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("EMP001: John Doe", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void Interface_WithGenericParameters_GenericInterface()
    {
        // Arrange
        var code = @"
            interface Container {
                func Add(item:any) -> void
                func Get(index:int) -> any
                func Size() -> int
            }

            class ListContainer < Container {
                private items <- {}

                func Add(item:any) -> void {
                    items.Push(item)
                }

                func Get(index:int) -> any {
                    return items[index]
                }

                func Size() -> int {
                    return len(items)
                }
            }

            container <- ListContainer()
            container.Add(""Item 1"")
            container.Add(42)
            container.Add(true)

            result1 <- container.Size()
            result2 <- container.Get(0)
            result3 <- container.Get(1)
            result4 <- container.Get(2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(3, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("Item 1", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(42, ((IntLangValue)result3).Value);

        Assert.NotNull(result4);
        Assert.IsType<BoolLangValue>(result4);
        Assert.True(((BoolLangValue)result4).Value);
    }

    [Fact]
    public void Interface_AbstractInterface_ForcesImplementation()
    {
        // Arrange
        var code = @"
            interface Vehicle {
                func Start() -> void
                func Stop() -> void
                func GetSpeed() -> double
            }

            class Car < Vehicle {
                public speed <- 0

                func Start() -> void {
                    speed <- 0
                }

                func Stop() -> void {
                    speed <- 0
                }

                func GetSpeed() -> double {
                    return speed
                }

                func Accelerate(amount:double) -> void {
                    speed <- speed + amount
                }
            }

            car <- Car()
            car.Start()
            car.Accelerate(50.0)
            result1 <- car.GetSpeed()
            car.Stop()
            result2 <- car.GetSpeed()
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
        Assert.Equal(50.0, ((DoubleLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(0.0, ((DoubleLangValue)result2).Value);
    }

    [Fact]
    public void Interface_Composition_CombinesMultipleBehaviors()
    {
        // Arrange
        var code = @"
            interface Comparable {
                func CompareTo(other:any) -> int
            }

            interface Serializable {
                func Serialize() -> string
            }

            class Product < Comparable, Serializable {
                public id <- 0
                public name <- """"

                func Product(i:int, n:string) {
                    id <- i
                    name <- n
                }

                func CompareTo(other:any) -> int {
                    // Compare based on id
                    if id < other.id { return -1 }
                    else if id > other.id { return 1 }
                    else { return 0 }
                }

                func Serialize() -> string {
                    return ""Product{id:"" + id.ToStr() + "",name:'"" + name + ""'}""
                }
            }

            product1 <- Product(1, ""Widget"")
            product2 <- Product(2, ""Gadget"")

            comparison <- product1.CompareTo(product2)
            serialized <- product1.Serialize()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var comparison = interpreter.Manager.GetValue(new LangId("comparison"));
        var serialized = interpreter.Manager.GetValue(new LangId("serialized"));

        Assert.NotNull(comparison);
        Assert.IsType<IntLangValue>(comparison);
        Assert.Equal(-1, ((IntLangValue)comparison).Value);

        Assert.NotNull(serialized);
        Assert.IsType<StringLangValue>(serialized);
        Assert.Equal("Product{id:1,name:'Widget'}", ((StringLangValue)serialized).Value);
    }

    [Fact]
    public void Interface_RuntimeTypeChecking_ChecksInterfaceImplementation()
    {
        // Arrange
        var code = @"
            interface Runnable {
                func Run() -> void
            }

            class Machine < Runnable {
                public name <- """"

                func Machine(n:string) {
                    name <- n
                }

                func Run() -> void {
                    // Machine running logic
                }
            }

            class SimpleObject {
                public value <- 0
            }

            machine <- Machine(""Test Machine"")
            simpleObj <- SimpleObject()
            simpleObj.value <- 42

            // Check if objects implement the interface
            machineImplements <- machine is Runnable
            simpleImplements <- simpleObj is Runnable

            machineValue <- machine.value  // This should work
            simpleValue <- simpleObj.value  // This should work
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var machineImplements = interpreter.Manager.GetValue(new LangId("machineImplements"));
        var simpleImplements = interpreter.Manager.GetValue(new LangId("simpleImplements"));

        // This would test interface type checking
        Assert.NotNull(machineImplements);
        Assert.IsType<BoolLangValue>(machineImplements);
        Assert.True(((BoolLangValue)machineImplements).Value);

        Assert.NotNull(simpleImplements);
        Assert.IsType<BoolLangValue>(simpleImplements);
        Assert.False(((BoolLangValue)simpleImplements).Value);
    }

    [Fact]
    public void Interface_Casting_SafeCastingToInterface()
    {
        // Arrange
        var code = @"
            interface Printable {
                func Print() -> string
            }

            class Document < Printable {
                public content <- """"

                func Document(text:string) {
                    content <- text
                }

                func Print() -> string {
                    return content
                }
            }

            class PlainObject {
                public data <- """"
            }

            doc <- Document(""Hello World"")
            plain <- PlainObject()
            plain.data <- ""Not printable""

            // Safe casting
            printableDoc <- doc as Printable
            printablePlain <- plain as Printable

            docResult <- if printableDoc != null then printableDoc.Print() else ""null""
            plainResult <- if printablePlain != null then printablePlain.Print() else ""null""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var docResult = interpreter.Manager.GetValue(new LangId("docResult"));
        var plainResult = interpreter.Manager.GetValue(new LangId("plainResult"));

        Assert.NotNull(docResult);
        Assert.IsType<StringLangValue>(docResult);
        Assert.Equal("Hello World", ((StringLangValue)docResult).Value);

        Assert.NotNull(plainResult);
        Assert.IsType<StringLangValue>(plainResult);
        Assert.Equal("null", ((StringLangValue)plainResult).Value);
    }

    [Fact]
    public void Interface_Callback_PassesInterfaceAsParameter()
    {
        // Arrange
        var code = @"
            interface Processor {
                func Process(data:any) -> any
            }

            class DataProcessor < Processor {
                func Process(data:any) -> any {
                    return ""Processed: "" + data.ToStr()
                }
            }

            class DataValidator < Processor {
                func Process(data:any) -> any {
                    return data != null
                }
            }

            func ExecuteWithProcessor(processor:Processor, data:any) -> any {
                return processor.Process(data)
            }

            processor1 <- DataProcessor()
            processor2 <- DataValidator()

            result1 <- ExecuteWithProcessor(processor1, ""Test Data"")
            result2 <- ExecuteWithProcessor(processor2, ""Valid Data"")
            result3 <- ExecuteWithProcessor(processor2, null)
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
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("Processed: Test Data", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<BoolLangValue>(result2);
        Assert.True(((BoolLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<BoolLangValue>(result3);
        Assert.False(((BoolLangValue)result3).Value);
    }

    [Fact]
    public void Interface_StaticMembers_StaticInterfaceMembers()
    {
        // Arrange
        var code = @"
            interface Factory {
                static func Create() -> Factory
                func GetType() -> string
            }

            class Widget < Factory {
                public id <- 0

                func Widget() {
                    id <- 42
                }

                static func Create() -> Factory {
                    return Widget()
                }

                func GetType() -> string {
                    return ""Widget""
                }
            }

            widget <- Widget.Create()
            typeInfo <- widget.GetType()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var typeInfo = interpreter.Manager.GetValue(new LangId("typeInfo"));
        Assert.NotNull(typeInfo);
        Assert.IsType<StringLangValue>(typeInfo);
        Assert.Equal("Widget", ((StringLangValue)typeInfo).Value);
    }
}