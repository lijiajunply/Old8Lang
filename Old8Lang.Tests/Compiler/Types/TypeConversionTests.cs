using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Types;

/// <summary>
/// 编译器模式下的类型系统测试 - 类型转换
/// </summary>
public class TypeConversionTests
{
    private readonly ITestOutputHelper _output;

    public TypeConversionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void IntToStringConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            intValue <- 42
            stringValue <- intValue as string
            
            Assert.Equal(""42"", stringValue)
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
    public void StringToIntConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            stringValue <- ""123""
            intValue <- stringValue as int
            
            Assert.Equal(123, intValue)
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
    public void DoubleToIntConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            doubleValue <- 3.7
            intValue <- doubleValue as int
            
            Assert.Equal(3, intValue)
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
    public void IntToDoubleConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            intValue <- 7
            doubleValue <- intValue as double
            
            Assert.Equal(7.0, doubleValue)
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
    public void StringToDoubleConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            stringValue <- ""3.14159""
            doubleValue <- stringValue as double
            
            Assert.True(doubleValue > 3.14 && doubleValue < 3.15)
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
    public void DoubleToStringConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            doubleValue <- 2.71828
            stringValue <- doubleValue as string
            
            Assert.Equal(""2.71828"", stringValue)
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
    public void BoolToIntConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            trueValue <- true
            falseValue <- false
            
            trueInt <- trueValue as int
            falseInt <- falseValue as int
            
            Assert.Equal(1, trueInt)
            Assert.Equal(0, falseInt)
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
    public void IntToBoolConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            zeroValue <- 0
            nonZeroValue <- 5
            
            zeroBool <- zeroValue as bool
            nonZeroBool <- nonZeroValue as bool
            
            Assert.Equal(false, zeroBool)
            Assert.Equal(true, nonZeroBool)
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
    public void NullConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            nullValue <- null
            
            // null转换为任何类型都应该是null
            stringFromNull <- nullValue as string
            intFromNull <- nullValue as int
            doubleFromNull <- nullValue as double
            boolFromNull <- nullValue as bool
            
            Assert.Equal(null, stringFromNull)
            Assert.Equal(null, intFromNull)
            Assert.Equal(null, doubleFromNull)
            Assert.Equal(null, boolFromNull)
            
            // 非null值转换为null应该保持原值（取决于实现）
            intVal <- 42
            intToNull <- intVal as int?  // 如果支持可空类型
            
            // 基本类型转换测试
            Assert.Equal(42, intVal)
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
    public void ArrayToListConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arrayValue <- [1, 2, 3, 4, 5]
            listValue <- arrayValue as list
            
            Assert.Equal(5, listValue.Count())
            Assert.Equal(1, listValue[0])
            Assert.Equal(5, listValue[4])
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
    public void ListToArrayConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            listValue <- {1, 2, 3, 4, 5}
            arrayValue <- listValue as array
            
            Assert.Equal(5, arrayValue.Length)
            Assert.Equal(1, arrayValue[0])
            Assert.Equal(5, arrayValue[4])
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
    public void StringToCharConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            singleCharString <- ""A""
            charValue <- singleCharString as char
            
            Assert.Equal('A', charValue)
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
    public void CharToStringConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            charValue <- 'Z'
            stringValue <- charValue as string
            
            Assert.Equal(""Z"", stringValue)
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
    public void ComplexConversions_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 复杂转换链
            originalInt <- 123
            asString <- originalInt as string
            asDouble <- asString as double
            backToInt <- asDouble as int
            
            // 验证转换链
            Assert.Equal(123, originalInt)
            Assert.Equal(""123"", asString)
            Assert.Equal(123.0, asDouble)
            Assert.Equal(123, backToInt)
            
            // 布尔表达式中的转换
            complexResult <- ((originalInt as string) as double) * 2
            Assert.Equal(246.0, complexResult)
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
    public void ConversionInFunctionCalls_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func processString(input:string) -> int {
                return input as int
            }
            
            func processInt(input:int) -> string {
                return input as string
            }
            
            func processDouble(input:double) -> string {
                return (input as int).ToStr()
            }
            
            // 测试函数中的类型转换
            result1 <- processString(""456"")
            result2 <- processInt(789)
            result3 <- processDouble(3.14)
            
            Assert.Equal(456, result1)
            Assert.Equal(""789"", result2)
            Assert.Equal(""3"", result3)  // 3.14 as int = 3
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
    public void ConversionWithCollections_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 集合类型转换
            stringArray <- [""one"", ""two"", ""three""]
            stringList <- stringArray as list
            
            intList <- {1, 2, 3, 4, 5}
            intArray <- intList as array
            
            // 混合集合转换
            mixedArray <- [1, ""two"", 3.0]
            mixedList <- mixedArray as list
            
            // 验证转换结果
            Assert.Equal(3, stringList.Count())
            Assert.Equal(""one"", stringList[0])
            Assert.Equal(5, intArray.Length)
            Assert.Equal(5, intArray[4])
            
            // 验证混合集合
            Assert.Equal(3, mixedList.Count())
            Assert.Equal(1, mixedList[0] as int)
            Assert.Equal(""two"", mixedList[1] as string)
            Assert.Equal(3.0, mixedList[2] as double)
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
    public void ConversionErrorHandling_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 测试无效转换的处理
            invalidString <- ""not a number""
            caughtError <- false
            
            try {
                result <- invalidString as int
                // 如果没有异常，可能返回了默认值
                if result == 0 {
                    caughtError <- true
                }
            } catch (e) {
                caughtError <- true
            }
            
            // 验证错误处理
            Assert.True(caughtError)
            
            // 测试null转换
            nullValue <- null
            stringFromNull <- nullValue as string
            intFromNull <- nullValue as int
            
            Assert.Equal(null, stringFromNull)
            Assert.Equal(null, intFromNull)
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
    public void ImplicitConversions_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 测试隐式转换场景
            intVar <- 5
            doubleVar <- 10.0
            
            // 算术运算中的隐式转换
            mixedResult <- intVar + doubleVar  // int -> double
            Assert.Equal(15.0, mixedResult)
            
            // 比较运算中的隐式转换
            isGreater <- doubleVar > intVar
            Assert.Equal(true, isGreater)
            
            // 字符串拼接中的转换
            concatString <- (intVar as string) + "" and "" + (doubleVar as string)
            Assert.Equal(""5 and 10"", concatString)
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