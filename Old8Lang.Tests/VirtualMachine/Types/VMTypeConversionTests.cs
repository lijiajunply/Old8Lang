using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Types;

/// <summary>
/// 虚拟机类型转换测试
/// 测试各种类型转换场景
/// </summary>
[Collection("Sequential")]
public class VMTypeConversionTests
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
    public void TypeConversion_IntToDouble_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 42
            y <- x as double
            result <- y + 0.5
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(42.5, Convert.ToDouble(result));
    }

    [Fact]
    public void TypeConversion_DoubleToInt_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 42.7
            y <- x as int
            result <- y
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(43, result);
    }

    [Fact]
    public void TypeConversion_IntToString_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 42
            result <- x.ToStr()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("42", result);
    }

    [Fact]
    public void TypeConversion_StringToInt_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- ""42""
            y <- int(x)
            result <- y + 10
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(52, result);
    }

    [Fact]
    public void TypeConversion_StringToDouble_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- ""3.14""
            y <- double(x)
            result <- y * 2
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(6.28, Convert.ToDouble(result), 2);
    }

    [Fact]
    public void TypeConversion_BoolToString_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- true
            y <- false
            result1 <- x.ToStr()
            result2 <- y.ToStr()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal("true", result1);
        Assert.Equal("false", result2);
    }

    [Fact]
    public void TypeConversion_StringToBool_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- ""true""
            y <- ""false""
            result1 <- bool(x)
            result2 <- bool(y)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.True((bool)result1);
        Assert.False((bool)result2);
    }

    [Fact]
    public void TypeConversion_ImplicitConversion_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20.5
            result <- x + y
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(30.5, Convert.ToDouble(result));
    }

    [Fact]
    public void TypeConversion_ClassCast_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            class Base {
                public func getValue() -> int {
                    return 10
                }
            }

            class Derived extends Base {
                public func getDoubleValue() -> int {
                    return 20
                }
            }

            obj <- Derived()
            baseObj <- obj as Base
            result <- baseObj.getValue()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(10, result);
    }

    [Fact]
    public void TypeConversion_ArrayToList_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            list <- arr.ToList()
            result <- list.Count()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(5, result);
    }

    [Fact]
    public void TypeConversion_ListToArray_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3, 4, 5}
            arr <- list.ToArray()
            result <- arr.Length
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(5, result);
    }

    [Fact]
    public void TypeConversion_NumberToChar_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 65
            ch <- char(x)
            result <- ch.ToStr()
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal("A", result);
    }

    [Fact]
    public void TypeConversion_CharToNumber_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- 'A'
            x <- int(ch)
            result <- x
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(65, result);
    }

    [Fact]
    public void TypeConversion_NullableConversion_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 42
            y <- x as int
            result <- y != null
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.True((bool)result);
    }

    [Fact]
    public void TypeConversion_StringInterpolation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 42
            y <- 3.14
            z <- true
            result <- $""x={x}, y={y}, z={z}""
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Contains("x=42", result.ToString());
        Assert.Contains("y=3.14", result.ToString());
        Assert.Contains("z=True", result.ToString());
    }

    [Fact]
    public void TypeConversion_ComplexExpression_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- ""10""
            y <- ""20""
            result <- int(x) + int(y)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(30, result);
    }
}
