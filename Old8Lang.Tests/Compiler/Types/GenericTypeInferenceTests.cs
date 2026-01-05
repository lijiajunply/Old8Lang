using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Types;

/// <summary>
/// 编译器模式下的类型系统测试 - 泛型类型推断
/// </summary>
public class GenericTypeInferenceTests
{
    private readonly ITestOutputHelper _output;

    public GenericTypeInferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicGenericInference_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func process(value) {
                return value + 1
            }
            
            // 编译器应该能推断出类型
            result1 <- process(5)      // 返回 6 (int)
            result2 <- process(3.5)    // 返回 4.5 (double)
            
            Assert.Equal(6, result1)
            Assert.Equal(4.5, result2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericFunctionReturnType_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func createArray<T>(count:int, value:T) -> array<T> {
                result <- []
                i <- 0
                while i < count {
                    result <- result + [value]
                    i <- i + 1
                }
                return result
            }
            
            intArray <- createArray(3, 42)
            strArray <- createArray(2, ""test"")
            
            Assert.Equal(3, intArray.Length())
            Assert.Equal(42, intArray[0])
            Assert.Equal(42, intArray[1])
            Assert.Equal(42, intArray[2])
            
            Assert.Equal(2, strArray.Length())
            Assert.Equal(""test"", strArray[0])
            Assert.Equal(""test"", strArray[1])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericWithConstraints_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func compare<T>(a:T, b:T) -> int where T: comparable {
                if a < b {
                    return -1
                } else if a > b {
                    return 1
                } else {
                    return 0
                }
            }
            
            result1 <- compare(5, 3)      // 1
            result2 <- compare(3, 5)      // -1
            result3 <- compare(5, 5)      // 0
            
            Assert.Equal(1, result1)
            Assert.Equal(-1, result2)
            Assert.Equal(0, result3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MultipleTypeParameters_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func createPair<T, U>(first:T, second:U) -> array {
                return [first, second]
            }
            
            pair1 <- createPair(42, ""hello"")
            pair2 <- createPair(""world"", 3.14)
            
            Assert.Equal(42, pair1[0])
            Assert.Equal(""hello"", pair1[1])
            Assert.Equal(""world"", pair2[0])
            Assert.Equal(3.14, pair2[1])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void NestedGenericTypes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func createMatrix<T>(rows:int, cols:int, value:T) -> array<array<T>> {
                matrix <- []
                i <- 0
                while i < rows {
                    row <- []
                    j <- 0
                    while j < cols {
                        row <- row + [value]
                        j <- j + 1
                    }
                    matrix <- matrix + [row]
                    i <- i + 1
                }
                return matrix
            }
            
            matrix <- createMatrix(2, 3, 7)
            
            Assert.Equal(2, matrix.Length())
            Assert.Equal(3, matrix[0].Length())
            Assert.Equal(3, matrix[1].Length())
            Assert.Equal(7, matrix[0][0])
            Assert.Equal(7, matrix[0][1])
            Assert.Equal(7, matrix[0][2])
            Assert.Equal(7, matrix[1][0])
            Assert.Equal(7, matrix[1][1])
            Assert.Equal(7, matrix[1][2])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void GenericInheritance_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Container<T> {
                value: T
                
                func new(v:T) {
                    this.value <- v
                }
                
                func get() -> T {
                    return this.value
                }
                
                func set(v:T) -> void {
                    this.value <- v
                }
            }
            
            intContainer <- Container.new(42)
            strContainer <- Container.new(""hello"")
            
            Assert.Equal(42, intContainer.get())
            Assert.Equal(""hello"", strContainer.get())
            
            intContainer.set(100)
            strContainer.set(""world"")
            
            Assert.Equal(100, intContainer.get())
            Assert.Equal(""world"", strContainer.get())
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}