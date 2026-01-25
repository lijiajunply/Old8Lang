using Old8Lang.Error;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.Extern;

/// <summary>
/// JavaScript Extern 功能测试（解释模式）
/// </summary>
[Collection("Sequential")]
public class JavaScriptExternTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    #region 基本函数调用

    /// <summary>
    /// 测试 JavaScript 基本算术函数
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_BasicArithmeticFunctions_ReturnsCorrectResults()
    {
        // Arrange
        var jsContent = @"
function add(a, b) {
    return a + b;
}

function multiply(a, b) {
    return a * b;
}

function subtract(a, b) {
    return a - b;
}
";
        var old8Content = @"
extern ""math.js"" {
    func add(a:int, b:int) -> int,
    func multiply(a:int, b:int) -> int,
    func subtract(a:int, b:int) -> int
}

result1 <- add(10, 20)
result2 <- multiply(6, 7)
result3 <- subtract(100, 30)
";

        CreateTempModuleFile("math.js", jsContent);
        CreateTempModuleFile("test_js_arithmetic.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_arithmetic.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 30);
        AssertVariableValue(interpreter, "result2", 42);
        AssertVariableValue(interpreter, "result3", 70);
    }

    /// <summary>
    /// 测试 JavaScript 字符串函数
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_StringFunctions_ReturnsCorrectResults()
    {
        // Arrange
        var jsContent = @"
function greet(name) {
    return 'Hello, ' + name + '!';
}

function toUpperCase(text) {
    return text.toUpperCase();
}

function concatenate(a, b) {
    return a + b;
}
";
        var old8Content = @"
extern ""string_utils.js"" {
    func greet(name:string) -> string,
    func toUpperCase(text:string) -> string,
    func concatenate(a:string, b:string) -> string
}

result1 <- greet(""Old8Lang"")
result2 <- toUpperCase(""hello"")
result3 <- concatenate(""foo"", ""bar"")
";

        CreateTempModuleFile("string_utils.js", jsContent);
        CreateTempModuleFile("test_js_string.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_string.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Hello, Old8Lang!");
        AssertVariableValue(interpreter, "result2", "HELLO");
        AssertVariableValue(interpreter, "result3", "foobar");
    }

    /// <summary>
    /// 测试 JavaScript 浮点数函数
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_DoubleFunctions_ReturnsCorrectResults()
    {
        // Arrange
        var jsContent = @"
function divide(a, b) {
    return a / b;
}

function power(base, exp) {
    return Math.pow(base, exp);
}

function squareRoot(x) {
    return Math.sqrt(x);
}
";
        var old8Content = @"
extern ""math_advanced.js"" {
    func divide(a:double, b:double) -> double,
    func power(base:double, exp:double) -> double,
    func squareRoot(x:double) -> double
}

result1 <- divide(10.0, 4.0)
result2 <- power(2.0, 3.0)
result3 <- squareRoot(16.0)
";

        CreateTempModuleFile("math_advanced.js", jsContent);
        CreateTempModuleFile("test_js_double.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_double.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 2.5);
        AssertVariableValue(interpreter, "result2", 8.0);
        AssertVariableValue(interpreter, "result3", 4.0);
    }

    #endregion

    #region 单函数导入

    /// <summary>
    /// 测试 JavaScript 单函数导入（不使用花括号）
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_SingleFunctionImport_WorksCorrectly()
    {
        // Arrange
        var jsContent = @"
function factorial(n) {
    if (n <= 1) return 1;
    return n * factorial(n - 1);
}
";
        var old8Content = @"
extern ""factorial.js"" func factorial(n:int) -> int

result <- factorial(5)
";

        CreateTempModuleFile("factorial.js", jsContent);
        CreateTempModuleFile("test_js_single.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_single.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 120);
    }

    #endregion

    #region js: 前缀支持

    /// <summary>
    /// 测试 js: 前缀导入
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_WithJsPrefix_WorksCorrectly()
    {
        // Arrange
        var jsContent = @"
function isEven(n) {
    return n % 2 === 0;
}

function isOdd(n) {
    return n % 2 !== 0;
}
";
        var old8Content = @"
extern ""js:predicates.js"" {
    func isEven(n:int) -> bool,
    func isOdd(n:int) -> bool
}

result1 <- isEven(4)
result2 <- isEven(5)
result3 <- isOdd(7)
result4 <- isOdd(8)
";

        CreateTempModuleFile("predicates.js", jsContent);
        CreateTempModuleFile("test_js_prefix.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_prefix.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", true);
        AssertVariableValue(interpreter, "result2", false);
        AssertVariableValue(interpreter, "result3", true);
        AssertVariableValue(interpreter, "result4", false);
    }

    #endregion

    #region 类型转换

    /// <summary>
    /// 测试 JavaScript 与 Old8Lang 之间的类型转换
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_TypeConversion_WorksCorrectly()
    {
        // Arrange
        var jsContent = @"
function doubleValue(x) {
    return x * 2;
}

function toString(x) {
    return x.toString();
}

function parseNumber(str) {
    return parseInt(str);
}
";
        var old8Content = @"
extern ""converters.js"" {
    func doubleValue(x:int) -> int,
    func toString(x:int) -> string,
    func parseNumber(str:string) -> int
}

result1 <- doubleValue(21)
result2 <- toString(123)
result3 <- parseNumber(""456"")
";

        CreateTempModuleFile("converters.js", jsContent);
        CreateTempModuleFile("test_js_conversion.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_conversion.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 42);
        AssertVariableValue(interpreter, "result2", "123");
        AssertVariableValue(interpreter, "result3", 456);
    }

    #endregion

    #region 错误处理

    /// <summary>
    /// 测试 JavaScript 文件不存在时的错误处理
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_NonExistentFile_ThrowsException()
    {
        // Arrange
        var old8Content = @"
extern ""nonexistent.js"" func test() -> void
";

        CreateTempModuleFile("test_js_error_notfound.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_js_error_notfound.old8", typeof(InvalidOperationError));
    }

    /// <summary>
    /// 测试 JavaScript 函数不存在时的错误处理
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_NonExistentFunction_ThrowsException()
    {
        // Arrange
        var jsContent = @"
function existingFunction() {
    return 42;
}
";
        var old8Content = @"
extern ""functions.js"" func nonExistentFunction() -> int

result <- nonExistentFunction()
";

        CreateTempModuleFile("functions.js", jsContent);
        CreateTempModuleFile("test_js_error_function.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_js_error_function.old8", typeof(InvalidOperationError));
    }

    #endregion

    #region 复杂场景

    /// <summary>
    /// 测试 JavaScript 混合类型参数和返回值
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_MixedTypes_WorksCorrectly()
    {
        // Arrange
        var jsContent = @"
function formatMessage(name, age, isStudent) {
    return name + ' is ' + age + ' years old. Student: ' + isStudent;
}

function calculate(a, b, operation) {
    if (operation === 'add') return a + b;
    if (operation === 'multiply') return a * b;
    return 0;
}
";
        var old8Content = @"
extern ""mixed.js"" {
    func formatMessage(name:string, age:int, isStudent:bool) -> string,
    func calculate(a:int, b:int, operation:string) -> int
}

result1 <- formatMessage(""Alice"", 20, true)
result2 <- calculate(5, 3, ""add"")
result3 <- calculate(5, 3, ""multiply"")
";

        CreateTempModuleFile("mixed.js", jsContent);
        CreateTempModuleFile("test_js_mixed.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_mixed.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Alice is 20 years old. Student: true");
        AssertVariableValue(interpreter, "result2", 8);
        AssertVariableValue(interpreter, "result3", 15);
    }

    /// <summary>
    /// 测试多个 JavaScript 文件导入
    /// </summary>
    [Fact]
    public void ExecuteJavaScript_MultipleFiles_WorksCorrectly()
    {
        // Arrange
        var jsContent1 = @"
function add(a, b) {
    return a + b;
}
";
        var jsContent2 = @"
function greet(name) {
    return 'Hello, ' + name;
}
";
        var old8Content = @"
extern ""math2.js"" func add(a:int, b:int) -> int
extern ""greet2.js"" func greet(name:string) -> string

result1 <- add(10, 20)
result2 <- greet(""World"")
";

        CreateTempModuleFile("math2.js", jsContent1);
        CreateTempModuleFile("greet2.js", jsContent2);
        CreateTempModuleFile("test_js_multiple.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_multiple.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 30);
        AssertVariableValue(interpreter, "result2", "Hello, World");
    }

    #endregion
}
