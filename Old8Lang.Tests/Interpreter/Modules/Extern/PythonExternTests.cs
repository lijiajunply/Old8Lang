using Old8Lang.Error;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Extern;

/// <summary>
/// Python Extern 功能测试（解释模式）
/// </summary>
[Collection("Sequential")]
public class PythonExternTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    #region Python 脚本文件导入

    /// <summary>
    /// 测试 Python 脚本基本函数调用
    /// </summary>
    [Fact]
    public void ExecutePython_ScriptBasicFunctions_ReturnsCorrectResults()
    {
        // Arrange
        var pyContent = @"
def add(a, b):
    return a + b

def multiply(a, b):
    return a * b

def subtract(a, b):
    return a - b
";
        var old8Content = @"
native extern ""math_utils.py"" {
    func add(a:int, b:int) -> int,
    func multiply(a:int, b:int) -> int,
    func subtract(a:int, b:int) -> int
}

result1 <- add(10, 20)
result2 <- multiply(6, 7)
result3 <- subtract(100, 30)
";

        CreateTempModuleFile("math_utils.py", pyContent);
        CreateTempModuleFile("test_py_arithmetic.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_py_arithmetic.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 30);
        AssertVariableValue(interpreter, "result2", 42);
        AssertVariableValue(interpreter, "result3", 70);
    }

    /// <summary>
    /// 测试 Python 字符串函数
    /// </summary>
    [Fact]
    public void ExecutePython_StringFunctions_ReturnsCorrectResults()
    {
        // Arrange
        var pyContent = @"
def greet(name):
    return f'Hello, {name}!'

def to_upper(text):
    return text.upper()

def concat(a, b):
    return a + b
";
        var old8Content = @"
native extern ""string_utils.py"" {
    func greet(name:string) -> string,
    func to_upper(text:string) -> string,
    func concat(a:string, b:string) -> string
}

result1 <- greet(""Old8Lang"")
result2 <- to_upper(""hello"")
result3 <- concat(""foo"", ""bar"")
";

        CreateTempModuleFile("string_utils.py", pyContent);
        CreateTempModuleFile("test_py_string.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_py_string.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Hello, Old8Lang!");
        AssertVariableValue(interpreter, "result2", "HELLO");
        AssertVariableValue(interpreter, "result3", "foobar");
    }

    /// <summary>
    /// 测试 Python 浮点数函数
    /// </summary>
    [Fact]
    public void ExecutePython_DoubleFunctions_ReturnsCorrectResults()
    {
        // Arrange
        var pyContent = @"
def divide(a, b):
    return a / b

def power(base, exp):
    return base ** exp
";
        var old8Content = @"
native extern ""math_advanced.py"" {
    func divide(a:double, b:double) -> double,
    func power(base:double, exp:double) -> double
}

result1 <- divide(10.0, 4.0)
result2 <- power(2.0, 3.0)
";

        CreateTempModuleFile("math_advanced.py", pyContent);
        CreateTempModuleFile("test_py_double.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_py_double.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 2.5);
        AssertVariableValue(interpreter, "result2", 8.0);
    }

    #endregion

    #region py: 前缀支持

    /// <summary>
    /// 测试 py: 前缀导入
    /// </summary>
    [Fact]
    public void ExecutePython_WithPyPrefix_WorksCorrectly()
    {
        // Arrange
        var pyContent = @"
def is_even(n):
    return n % 2 == 0

def is_odd(n):
    return n % 2 != 0
";
        var old8Content = @"
native extern ""py:predicates.py"" {
    func is_even(n:int) -> bool,
    func is_odd(n:int) -> bool
}

result1 <- is_even(4)
result2 <- is_even(5)
result3 <- is_odd(7)
result4 <- is_odd(8)
";

        CreateTempModuleFile("predicates.py", pyContent);
        CreateTempModuleFile("test_py_prefix.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_py_prefix.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", true);
        AssertVariableValue(interpreter, "result2", false);
        AssertVariableValue(interpreter, "result3", true);
        AssertVariableValue(interpreter, "result4", false);
    }

    #endregion

    #region Python 模块导入

    /// <summary>
    /// 测试 Python 标准库模块导入（pymodule:）
    /// </summary>
    [Fact]
    public void ExecutePython_StandardLibraryModule_WorksCorrectly()
    {
        // Arrange
        var old8Content = @"
native extern ""pymodule:math"" {
    func sqrt(x:double) -> double,
    func pow(base:double, exp:double) -> double,
    func floor(x:double) -> double,
    func ceil(x:double) -> double
}

result1 <- sqrt(16.0)
result2 <- pow(2.0, 3.0)
result3 <- floor(3.7)
result4 <- ceil(3.2)
";

        CreateTempModuleFile("test_py_module.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_py_module.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 4.0);
        AssertVariableValue(interpreter, "result2", 8.0);
        AssertVariableValue(interpreter, "result3", 3.0);
        AssertVariableValue(interpreter, "result4", 4.0);
    }

    #endregion

    #region 单函数导入

    /// <summary>
    /// 测试 Python 单函数导入（不使用花括号）
    /// </summary>
    [Fact]
    public void ExecutePython_SingleFunctionImport_WorksCorrectly()
    {
        // Arrange
        var pyContent = @"
def factorial(n):
    if n <= 1:
        return 1
    return n * factorial(n - 1)
";
        var old8Content = @"
native extern ""factorial.py"" func factorial(n:int) -> int

result <- factorial(5)
";

        CreateTempModuleFile("factorial.py", pyContent);
        CreateTempModuleFile("test_py_single.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_py_single.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 120);
    }

    #endregion

    #region 错误处理

    /// <summary>
    /// 测试 Python 文件不存在时的错误处理
    /// </summary>
    [Fact]
    public void ExecutePython_NonExistentFile_ThrowsException()
    {
        // Arrange
        var old8Content = @"
native extern ""nonexistent.py"" func test() -> void
";

        CreateTempModuleFile("test_py_error_notfound.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_py_error_notfound.old8", typeof(InvalidOperationError));
    }

    /// <summary>
    /// 测试 Python 函数不存在时的错误处理
    /// </summary>
    [Fact]
    public void ExecutePython_NonExistentFunction_ThrowsException()
    {
        // Arrange
        var pyContent = @"
def existing_function():
    return 42
";
        var old8Content = @"
native extern ""functions.py"" func non_existent_function() -> int

result <- non_existent_function()
";

        CreateTempModuleFile("functions.py", pyContent);
        CreateTempModuleFile("test_py_error_function.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_py_error_function.old8", typeof(InvalidOperationError));
    }

    /// <summary>
    /// 测试 Python 模块不存在时的错误处理
    /// </summary>
    [Fact]
    public void ExecutePython_NonExistentModule_ThrowsException()
    {
        // Arrange
        var old8Content = @"
native extern ""pymodule:nonexistent_module"" func test() -> int

result <- test()
";

        CreateTempModuleFile("test_py_error_module.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_py_error_module.old8", typeof(InvalidOperationError));
    }

    #endregion

    #region 复杂场景

    /// <summary>
    /// 测试 Python 混合类型参数和返回值
    /// </summary>
    [Fact]
    public void ExecutePython_MixedTypes_WorksCorrectly()
    {
        // Arrange
        var pyContent = @"
def format_message(name, age, is_student):
    return f'{name} is {age} years old. Student: {is_student}'

def calculate(a, b, operation):
    if operation == 'add':
        return a + b
    elif operation == 'multiply':
        return a * b
    return 0
";
        var old8Content = @"
native extern ""mixed.py"" {
    func format_message(name:string, age:int, is_student:bool) -> string,
    func calculate(a:int, b:int, operation:string) -> int
}

result1 <- format_message(""Alice"", 20, true)
result2 <- calculate(5, 3, ""add"")
result3 <- calculate(5, 3, ""multiply"")
";

        CreateTempModuleFile("mixed.py", pyContent);
        CreateTempModuleFile("test_py_mixed.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_py_mixed.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Alice is 20 years old. Student: True");
        AssertVariableValue(interpreter, "result2", 8);
        AssertVariableValue(interpreter, "result3", 15);
    }

    /// <summary>
    /// 测试多个 Python 文件和模块混合导入
    /// </summary>
    [Fact]
    public void ExecutePython_MultipleSources_WorksCorrectly()
    {
        // Arrange
        var pyContent = @"
def custom_add(a, b):
    return a + b
";
        var old8Content = @"
native extern ""custom_math.py"" func custom_add(a:int, b:int) -> int
native extern ""pymodule:math"" func sqrt(x:double) -> double

result1 <- custom_add(10, 20)
result2 <- sqrt(25.0)
";

        CreateTempModuleFile("custom_math.py", pyContent);
        CreateTempModuleFile("test_py_multiple.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_py_multiple.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 30);
        AssertVariableValue(interpreter, "result2", 5.0);
    }

    #endregion
}
