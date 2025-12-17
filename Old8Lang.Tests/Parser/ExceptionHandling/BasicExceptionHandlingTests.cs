using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.ExceptionHandling;

/// <summary>
/// 基础异常处理语法测试
/// </summary>
[Collection("Sequential")]
public class BasicExceptionHandlingTests
{
    #region 基础 try-catch 语法

    /// <summary>
    /// 测试基本try-catch语法
    /// </summary>
    [Fact]
    public void ParseProgram_BasicTryCatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func DivideNumbers(a:double, b:double) -> double {
    try {
        if b == 0 {
            throw ""Division by zero""
        }
        return a / b
    } catch (error) {
        PrintLine(""Error: "" + error.ToStr())
        return 0
    }
}

result1 <- DivideNumbers(10, 2)
result2 <- DivideNumbers(10, 0)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试try-catch-finally语法
    /// </summary>
    [Fact]
    public void ParseProgram_TryCatchFinally_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func FileOperation(filename:string) -> string {
    fileHandle <- null
    try {
        PrintLine(""Opening file: "" + filename)
        fileHandle <- ""FileHandle_""

        if filename == ""error.txt"" {
            throw ""File not found""
        }

        return ""File read successfully""
    } catch (error) {
        PrintLine(""File operation error: "" + error.ToStr())
        return ""Operation failed""
    } finally {
        if fileHandle != null {
            PrintLine(""Closing file handle"")
            fileHandle <- null
        }
    }
}

result1 <- FileOperation(""data.txt"")
result2 <- FileOperation(""error.txt"")";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试throw语法
    /// </summary>
    [Fact]
    public void ParseProgram_BasicThrow_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func ValidateAge(age:int) -> bool {
    if age < 0 {
        throw ""Age cannot be negative""
    } else if age > 150 {
        throw ""Age cannot be greater than 150""
    }
    return true
}

func ProcessUser(name:string, age:int) -> string {
    try {
        if ValidateAge(age) {
            return ""User "" + name + "" ("" + age.ToStr() + "") is valid""
        }
    } catch (error) {
        return ""Validation error: "" + error.ToStr()
    }
}

result1 <- ProcessUser(""Alice"", 25)
result2 <- ProcessUser(""Bob"", -5)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误语法测试

    /// <summary>
    /// 测试不完整的try-catch语法
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteTryCatch_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test() {
    try {
        someOperation()
    catch {  // 缺少右括号
        handle()
    }
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试throw缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_ThrowWithoutExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func test() {
    throw
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}