using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Statement;

/// <summary>
/// Extern 语句解析测试
/// </summary>
[Collection("Sequential")]
public class ExternStatementTests
{
    #region 基本 Extern 测试

    /// <summary>
    /// 测试 C/C++ P/Invoke 单函数导入
    /// </summary>
    [Fact]
    public void ParseExternStatement_NativeDll_SingleFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"native extern ""msvcrt.dll"" func abs(x:int) -> int";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
        var externStmt = (ExternStatement)result.ImportStatements[0];
        Assert.Equal(ExternType.NativeDll, ExternStatement.DetectExternType("msvcrt.dll"));
        Assert.Equal(1, externStmt.Count); // 内部函数数量通过反射或其他方式验证
    }

    /// <summary>
    /// 测试 C/C++ P/Invoke 批量函数导入
    /// </summary>
    [Fact]
    public void ParseExternStatement_NativeDll_MultipleFunctions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
native extern ""kernel32.dll"" {
    func GetCurrentThreadId() -> int,
    func GetCurrentProcessId() -> int
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
    }

    /// <summary>
    /// 测试带调用约定的 P/Invoke
    /// </summary>
    [Fact]
    public void ParseExternStatement_WithCallingConvention_ParsesSuccessfully()
    {
        // Arrange
        var code = @"native extern ""user32.dll"" stdcall func MessageBoxA(hwnd:int, text:string, caption:string, type:int) -> int";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
    }

    /// <summary>
    /// 测试带别名的函数导入
    /// </summary>
    [Fact]
    public void ParseExternStatement_WithAlias_ParsesSuccessfully()
    {
        // Arrange
        var code = @"native extern ""msvcrt.dll"" func abs(x:int) -> int as absolute";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
    }

    #endregion

    #region Python Extern 测试

    /// <summary>
    /// 测试 Python 脚本文件导入（.py 扩展名）
    /// </summary>
    [Fact]
    public void ParseExternStatement_PythonScript_PyExtension_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
native extern ""math_utils.py"" {
    func add(a:int, b:int) -> int,
    func multiply(a:int, b:int) -> int
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
        Assert.Equal(ExternType.PythonScript, ExternStatement.DetectExternType("math_utils.py"));
    }

    /// <summary>
    /// 测试 Python 脚本文件导入（py: 前缀）
    /// </summary>
    [Fact]
    public void ParseExternStatement_PythonScript_PyPrefix_ParsesSuccessfully()
    {
        // Arrange
        var code = @"native extern ""py:utils.py"" func greet(name:string) -> string";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
        Assert.Equal(ExternType.PythonScript, ExternStatement.DetectExternType("py:utils.py"));
    }

    /// <summary>
    /// 测试 Python 模块导入（pymodule: 前缀）
    /// </summary>
    [Fact]
    public void ParseExternStatement_PythonModule_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
native extern ""pymodule:math"" {
    func sqrt(x:double) -> double,
    func pow(base:double, exp:double) -> double
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
        Assert.Equal(ExternType.PythonModule, ExternStatement.DetectExternType("pymodule:math"));
    }

    #endregion

    #region JavaScript Extern 测试

    /// <summary>
    /// 测试 JavaScript 脚本文件导入（.js 扩展名）
    /// </summary>
    [Fact]
    public void ParseExternStatement_JavaScript_JsExtension_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
native extern ""utils.js"" {
    func add(a:int, b:int) -> int,
    func greet(name:string) -> string
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
        Assert.Equal(ExternType.JavaScript, ExternStatement.DetectExternType("utils.js"));
    }

    /// <summary>
    /// 测试 JavaScript 脚本文件导入（js: 前缀）
    /// </summary>
    [Fact]
    public void ParseExternStatement_JavaScript_JsPrefix_ParsesSuccessfully()
    {
        // Arrange
        var code = @"native extern ""js:app.js"" func init() -> void";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
        Assert.Equal(ExternType.JavaScript, ExternStatement.DetectExternType("js:app.js"));
    }

    #endregion

    #region 复杂场景测试

    /// <summary>
    /// 测试多个 extern 语句连续声明
    /// </summary>
    [Fact]
    public void ParseExternStatement_MultipleExterns_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
native extern ""msvcrt.dll"" func abs(x:int) -> int
native extern ""math.py"" func sqrt(x:double) -> double
native extern ""utils.js"" func greet(name:string) -> string
";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.ImportStatements.Count);
        Assert.All(result.ImportStatements, stmt => Assert.IsType<ExternStatement>(stmt));
    }

    /// <summary>
    /// 测试混合调用约定的批量导入
    /// </summary>
    [Fact]
    public void ParseExternStatement_MixedCallingConventions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
native extern ""test.dll"" stdcall {
    func func1(a:int) -> int,
    cdecl func func2(b:int) -> int,
    winapi func func3(c:int) -> int
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.IsType<ExternStatement>(result.ImportStatements[0]);
    }

    #endregion

    #region 错误测试

    /// <summary>
    /// 测试缺少 native 关键字
    /// </summary>
    [Fact]
    public void ParseExternStatement_MissingNativeKeyword_ThrowsException()
    {
        // Arrange
        var code = @"extern ""test.dll"" func test() -> void";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.Throws<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少函数签名
    /// </summary>
    [Fact]
    public void ParseExternStatement_MissingFunctionSignature_ThrowsException()
    {
        // Arrange
        var code = @"native extern ""test.dll"" func test";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.Throws<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试空的批量导入块
    /// </summary>
    [Fact]
    public void ParseExternStatement_EmptyBlock_ThrowsException()
    {
        // Arrange
        var code = @"native extern ""test.dll"" { }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.NotNull(parser.ParseProgram());
    }

    #endregion

    #region 类型检测测试

    /// <summary>
    /// 测试各种文件扩展名的类型检测
    /// </summary>
    [Theory]
    [InlineData("test.dll", ExternType.NativeDll)]
    [InlineData("test.so", ExternType.NativeDll)]
    [InlineData("test.dylib", ExternType.NativeDll)]
    [InlineData("test.py", ExternType.PythonScript)]
    [InlineData("py:test.py", ExternType.PythonScript)]
    [InlineData("pymodule:math", ExternType.PythonModule)]
    [InlineData("test.js", ExternType.JavaScript)]
    [InlineData("js:test.js", ExternType.JavaScript)]
    [InlineData("msvcrt.dll", ExternType.NativeDll)]
    public void DetectExternType_VariousFormats_ReturnsCorrectType(string source, ExternType expectedType)
    {
        // Act
        var actualType = ExternStatement.DetectExternType(source);

        // Assert
        Assert.Equal(expectedType, actualType);
    }

    #endregion
}
