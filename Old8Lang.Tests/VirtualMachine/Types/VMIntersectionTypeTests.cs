using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Types;

/// <summary>
/// 虚拟机交叉类型测试
/// 测试交叉类型的约束检查和组合
/// </summary>
[Collection("Sequential")]
public class VMIntersectionTypeTests
{
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            var vm = new VM(bytecodeFile);
            vm.Execute();
            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void IntersectionType_InterfaceCombination_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IReadable {
                func read() -> string
            }

            interface IWritable {
                func write(data:string) -> void
            }

            class Document implements IReadable, IWritable {
                private content:string

                public func init() -> void {
                    this.content <- """"
                }

                public func read() -> string {
                    return this.content
                }

                public func write(data:string) -> void {
                    this.content <- data
                }
            }

            doc <- Document()
            doc.write(""Hello"")
            result <- doc.read()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void IntersectionType_MultipleInterfaces_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface INameable {
                func getName() -> string
            }

            interface ICountable {
                func getCount() -> int
            }

            interface IDescribable {
                func getDescription() -> string
            }

            class Item implements INameable, ICountable, IDescribable {
                private name:string
                private count:int

                public func init(n:string, c:int) -> void {
                    this.name <- n
                    this.count <- c
                }

                public func getName() -> string {
                    return this.name
                }

                public func getCount() -> int {
                    return this.count
                }

                public func getDescription() -> string {
                    return this.name + "": "" + this.count.ToStr()
                }
            }

            item <- Item(""Apple"", 5)
            result1 <- item.getName()
            result2 <- item.getCount()
            result3 <- item.getDescription()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.Equal("Apple", result1);
        Assert.Equal(5, result2);
        Assert.Equal("Apple: 5", result3);
    }

    [Fact]
    public void IntersectionType_WithGenericConstraint_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IComparable {
                func compareTo(other:object) -> int
            }

            class Number implements IComparable {
                private value:int

                public func init(v:int) -> void {
                    this.value <- v
                }

                public func getValue() -> int {
                    return this.value
                }

                public func compareTo(other:object) -> int {
                    otherNum <- other as Number
                    if this.value > otherNum.getValue() {
                        return 1
                    } elif this.value < otherNum.getValue() {
                        return -1
                    } else {
                        return 0
                    }
                }
            }

            num1 <- Number(10)
            num2 <- Number(20)
            result <- num1.compareTo(num2)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void IntersectionType_MethodOverlap_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IProcessor {
                func process() -> string
            }

            interface IValidator {
                func validate() -> bool
            }

            class DataHandler implements IProcessor, IValidator {
                private data:string

                public func init(d:string) -> void {
                    this.data <- d
                }

                public func process() -> string {
                    return ""Processed: "" + this.data
                }

                public func validate() -> bool {
                    return this.data != null && this.data != """"
                }
            }

            handler <- DataHandler(""test"")
            result1 <- handler.validate()
            result2 <- handler.process()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.True((bool)result1);
        Assert.Equal("Processed: test", result2);
    }

    [Fact]
    public void IntersectionType_WithInheritance_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface ISerializable {
                func serialize() -> string
            }

            class Base {
                protected value:int

                public func init(v:int) -> void {
                    this.value <- v
                }

                public func getValue() -> int {
                    return this.value
                }
            }

            class Derived extends Base implements ISerializable {
                public func init(v:int) -> void {
                    super.init(v)
                }

                public func serialize() -> string {
                    return ""Value: "" + this.value.ToStr()
                }
            }

            obj <- Derived(42)
            result1 <- obj.getValue()
            result2 <- obj.serialize()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(42, result1);
        Assert.Equal("Value: 42", result2);
    }

    [Fact]
    public void IntersectionType_ComplexHierarchy_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IIdentifiable {
                func getId() -> int
            }

            interface INameable {
                func getName() -> string
            }

            interface IDescribable {
                func describe() -> string
            }

            class Entity implements IIdentifiable, INameable, IDescribable {
                private id:int
                private name:string

                public func init(i:int, n:string) -> void {
                    this.id <- i
                    this.name <- n
                }

                public func getId() -> int {
                    return this.id
                }

                public func getName() -> string {
                    return this.name
                }

                public func describe() -> string {
                    return ""Entity #"" + this.id.ToStr() + "": "" + this.name
                }
            }

            entity <- Entity(1, ""Test"")
            result <- entity.describe()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("Entity #1: Test", result);
    }

    [Fact]
    public void IntersectionType_WithList_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IPrintable {
                func print() -> string
            }

            class Item implements IPrintable {
                private name:string

                public func init(n:string) -> void {
                    this.name <- n
                }

                public func print() -> string {
                    return ""Item: "" + this.name
                }
            }

            items <- {}
            items.Add(Item(""A""))
            items.Add(Item(""B""))
            items.Add(Item(""C""))

            results <- {}
            for item in items {
                results.Add(item.print())
            }

            result <- results.Count()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(3, result);
    }

    [Fact]
    public void IntersectionType_PolymorphicBehavior_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            interface IShape {
                func getArea() -> double
            }

            interface IDrawable {
                func draw() -> string
            }

            class Circle implements IShape, IDrawable {
                private radius:double

                public func init(r:double) -> void {
                    this.radius <- r
                }

                public func getArea() -> double {
                    return 3.14 * this.radius * this.radius
                }

                public func draw() -> string {
                    return ""Drawing circle with radius "" + this.radius.ToStr()
                }
            }

            circle <- Circle(5.0)
            result1 <- circle.getArea()
            result2 <- circle.draw()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(78.5, Convert.ToDouble(result1));
        Assert.Equal("Drawing circle with radius 5", result2);
    }
}
