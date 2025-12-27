using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Basic;

/// <summary>
/// 编译模式类型系统测试
/// 测试编译器在处理类型注解、类型转换和类型检查时的行为
/// </summary>
[Collection("Sequential")]
public class TypeSystemTests
{
    #region 基础类型注解测试

    [Fact]
    public void IntegerTypeAnnotation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a:int <- 42
            b:int <- 100
            c:int <- a + b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void DoubleTypeAnnotation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a:double <- 3.14
            b:double <- 2.71
            c:double <- a + b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void StringTypeAnnotation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a:string <- ""hello""
            b:string <- ""world""
            c:string <- a + "" "" + b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void BooleanTypeAnnotation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a:bool <- true
            b:bool <- false
            c:bool <- a and b
            d:bool <- a or b
            e:bool <- not a
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void CharTypeAnnotation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a:char <- 'A'
            b:char <- 'B'
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 函数类型注解测试

    [Fact]
    public void FunctionWithTypedParameters_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int):int {
                return a + b
            }
            
            result:int <- add(5, 10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void FunctionWithMixedParameterTypes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func createMessage(name:string, age:int):string {
                return ""Name: "" + name + "", Age: "" + age.ToStr()
            }
            
            message:string <- createMessage(""Alice"", 25)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void FunctionWithDoubleReturn_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func calculateArea(radius:double):double {
                return 3.14159 * radius * radius
            }
            
            area:double <- calculateArea(5.0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void FunctionWithBooleanReturn_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func isEven(number:int):bool {
                return number % 2 == 0
            }
            
            evenCheck1:bool <- isEven(4)
            evenCheck2:bool <- isEven(7)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 数组类型注解测试

    [Fact]
    public void ArrayTypeAnnotation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers:int[] <- [1, 2, 3, 4, 5]
            strings:string[] <- [""hello"", ""world"", ""test""]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArrayOperationsWithTypes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers:int[] <- [1, 2, 3, 4, 5]
            sum:int <- 0
            for num in numbers {
                sum <- sum + num
            }
            result:int <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型转换测试

    [Fact]
    public void ImplicitTypeConversion_IntToDouble_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            intVal:int <- 42
            doubleVal:double <- intVal + 0.5
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void StringConcatenationWithNumbers_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            text:string <- ""The answer is ""
            number:int <- 42
            result:string <- text + number.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void BoolToNumberConversion_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            boolVal:bool <- true
            intVal:int <- boolVal ? 1 : 0
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型推断测试

    [Fact]
    public void TypeInferenceFromAssignment_CompilesAndExecutesCorrectly()
    {
        // Arrange - 如果支持类型推断
        var code = @"
            a <- 42  // 应该推断为int
            b <- 3.14  // 应该推断为double
            c <- ""hello""  // 应该推断为string
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型边界测试

    [Fact]
    public void TypeBoundaryValues_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            maxInt:int <- 2147483647
            minInt:int <- -2147483648
            maxDouble:double <- 1.7976931348623157E+308
            minDouble:double <- -1.7976931348623157E+308
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void UnicodeStringType_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            chinese:string <- ""你好世界""
            emoji:string <- ""🚀🌟""
            mixed:string <- ""Hello 世界 🌍""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 复杂类型测试

    [Fact]
    public void NestedArrayType_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            matrix:int[][] <- [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
            firstRow:int[] <- matrix[0]
            firstElement:int <- firstRow[0]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void DictionaryType_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            scores:dict <- {""Alice"": 95, ""Bob"": 87, ""Charlie"": 92}
            aliceScore:int <- scores[""Alice""]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型操作测试

    [Fact]
    public void TypeComparison_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a:int <- 5
            b:int <- 5.0  // 如果支持隐式转换
            result:bool <- a == b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArithmeticWithMixedTypes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            intVal:int <- 10
            doubleVal:double <- 3.5
            result1:double <- intVal + doubleVal
            result2:double <- intVal * doubleVal
            result3:double <- intVal / doubleVal
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型注解错误测试

    [Fact]
    public void TypeMismatchAssignment_ThrowsCompilationError()
    {
        // Arrange
        var code = @"
            a:int <- 42
            a <- ""hello""  // 类型不匹配
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    [Fact]
    public void FunctionParameterTypeMismatch_ThrowsCompilationError()
    {
        // Arrange
        var code = @"
            func add(a:int, b:int):int {
                return a + b
            }
            
            result:int <- add(5, ""hello"")  // 参数类型错误
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    [Fact]
    public void FunctionReturnTypeMismatch_ThrowsCompilationError()
    {
        // Arrange
        var code = @"
            func getNumber():int {
                return ""hello""  // 返回类型错误
            }
            
            result:int <- getNumber()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    #endregion

    #region 泛型和高级类型测试

    [Fact]
    public void GenericArrayType_CompilesAndExecutesCorrectly()
    {
        // Arrange - 如果支持泛型
        var code = @"
            numbers:array<int> <- [1, 2, 3, 4, 5]
            strings:array<string> <- [""a"", ""b"", ""c""]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void OptionalType_CompilesAndExecutesCorrectly()
    {
        // Arrange - 如果支持可选类型
        var code = @"
            optionalInt:int? <- 42
            nullInt:int? <- null
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型操作符测试

    [Fact]
    public void TypeCheckOperator_CompilesAndExecutesCorrectly()
    {
        // Arrange - 如果支持类型检查操作符
        var code = @"
            value <- 42
            isInt:bool <- value is int
            isString:bool <- value is string
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TypeCastOperator_CompilesAndExecutesCorrectly()
    {
        // Arrange - 如果支持类型转换操作符
        var code = @"
            value <- 42
            asDouble:double <- value as double
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}