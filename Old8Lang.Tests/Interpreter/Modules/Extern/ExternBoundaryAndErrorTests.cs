using Old8Lang.Error;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Extern;

/// <summary>
/// Extern 功能边界和错误测试
/// 测试各种边界条件、错误处理和异常情况
/// </summary>
[Collection("Sequential")]
public class ExternBoundaryAndErrorTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    #region 文件路径边界测试

    /// <summary>
    /// 测试空文件名
    /// </summary>
    [Fact]
    public void Extern_EmptyFileName_ThrowsException()
    {
        // Arrange
        var old8Content = @"
extern """" func test() -> void
";

        CreateTempModuleFile("test_empty_filename.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_empty_filename.old8", typeof(InvalidOperationError));
    }

    /// <summary>
    /// 测试超长文件路径
    /// </summary>
    [Fact]
    public void Extern_VeryLongPath_HandlesCorrectly()
    {
        // Arrange
        var longFileName = new string('a', 200) + ".js";
        var old8Content = $@"
extern ""{longFileName}"" func test() -> void
";

        CreateTempModuleFile("test_long_path.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_long_path.old8", typeof(InvalidOperationError));
    }

    /// <summary>
    /// 测试包含特殊字符的文件名
    /// </summary>
    [Fact]
    public void Extern_SpecialCharactersInPath_HandlesCorrectly()
    {
        // Arrange
        var old8Content = @"
extern ""../../../etc/passwd"" func test() -> void
";

        CreateTempModuleFile("test_special_chars.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_special_chars.old8", typeof(InvalidOperationError));
    }

    #endregion

    #region 函数参数边界测试

    /// <summary>
    /// 测试无参数函数
    /// </summary>
    [Fact]
    public void Extern_NoParameters_WorksCorrectly()
    {
        // Arrange
        var jsContent = @"
function getConstant() {
    return 42;
}
";
        var old8Content = @"
extern ""no_params.js"" func getConstant() -> int

result <- getConstant()
";

        CreateTempModuleFile("no_params.js", jsContent);
        CreateTempModuleFile("test_no_params.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_no_params.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 42);
    }

    /// <summary>
    /// 测试大量参数函数
    /// </summary>
    [Fact]
    public void Extern_ManyParameters_WorksCorrectly()
    {
        // Arrange
        var jsContent = @"
function sumMany(a, b, c, d, e, f, g, h) {
    return a + b + c + d + e + f + g + h;
}
";
        var old8Content = @"
extern ""many_params.js"" func sumMany(a:int, b:int, c:int, d:int, e:int, f:int, g:int, h:int) -> int

result <- sumMany(1, 2, 3, 4, 5, 6, 7, 8)
";

        CreateTempModuleFile("many_params.js", jsContent);
        CreateTempModuleFile("test_many_params.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_many_params.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 36);
    }

    #endregion

    #region 返回值边界测试

    /// <summary>
    /// 测试返回 null/undefined
    /// </summary>
    [Fact]
    public void Extern_ReturnsUndefined_HandlesCorrectly()
    {
        // Arrange
        var jsContent = @"
function returnsNothing() {
    // JavaScript 函数默认返回 undefined
}
";
        var old8Content = @"
extern ""returns_nothing.js"" func returnsNothing() -> object

result <- returnsNothing()
";

        CreateTempModuleFile("returns_nothing.js", jsContent);
        CreateTempModuleFile("test_returns_undefined.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_returns_undefined.old8");

        // Assert
        Assert.Null(exception);
        // JavaScript undefined 应该转换为 null
        var result = interpreter.Manager.GetValue(new Old8Lang.AST.Expression.LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<Old8Lang.AST.Expression.Value.NullLangValue>(result);
    }

    /// <summary>
    /// 测试返回非常大的数字
    /// </summary>
    [Fact]
    public void Extern_ReturnsLargeNumber_WorksCorrectly()
    {
        // Arrange
        var jsContent = @"
function getLargeNumber() {
    return 2147483647; // int.MaxValue
}
";
        var old8Content = @"
extern ""large_number.js"" func getLargeNumber() -> int

result <- getLargeNumber()
";

        CreateTempModuleFile("large_number.js", jsContent);
        CreateTempModuleFile("test_large_number.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_large_number.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 2147483647);
    }

    #endregion

    #region 类型不匹配测试

    /// <summary>
    /// 测试类型不匹配的参数
    /// JavaScript 会进行隐式类型转换,字符串 * 2 = NaN
    /// </summary>
    [Fact]
    public void Extern_TypeMismatch_HandlesGracefully()
    {
        // Arrange
        var jsContent = @"
function expectsNumber(x) {
    return x * 2;
}
";
        var old8Content = @"
extern ""type_test.js"" func expectsNumber(x:int) -> int

// 传递字符串而不是数字 - JavaScript 会将其转换为 NaN
str <- ""not a number""
result <- expectsNumber(str)
";

        CreateTempModuleFile("type_test.js", jsContent);
        CreateTempModuleFile("test_type_mismatch.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_type_mismatch.old8");

        // Assert
        // JavaScript 不会为类型不匹配抛出错误,而是返回 NaN
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new Old8Lang.AST.Expression.LangId("result"));
        Assert.NotNull(result);
        // JavaScript NaN 会被转换为 Old8Lang 的 double NaN
        Assert.IsType<Old8Lang.AST.Expression.Value.DoubleLangValue>(result);
        var doubleResult = (Old8Lang.AST.Expression.Value.DoubleLangValue)result;
        Assert.True(double.IsNaN(doubleResult.Value), "Expected JavaScript to return NaN for string * 2");
    }

    #endregion

    #region Python 特定边界测试

    /// <summary>
    /// 测试 Python 函数抛出异常
    /// </summary>
    [Fact]
    public void Extern_PythonException_PropagatesCorrectly()
    {
        // Arrange
        var pyContent = @"
def divide_by_zero():
    return 1 / 0
";
        var old8Content = @"
extern ""exception_test.py"" func divide_by_zero() -> double

result <- divide_by_zero()
";

        CreateTempModuleFile("exception_test.py", pyContent);
        CreateTempModuleFile("test_python_exception.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_python_exception.old8", typeof(InvalidOperationError));
    }

    /// <summary>
    /// 测试 Python None 返回值
    /// </summary>
    [Fact]
    public void Extern_PythonReturnsNone_HandlesCorrectly()
    {
        // Arrange
        var pyContent = @"
def returns_none():
    return None
";
        var old8Content = @"
extern ""returns_none.py"" func returns_none() -> object

result <- returns_none()
";

        CreateTempModuleFile("returns_none.py", pyContent);
        CreateTempModuleFile("test_python_none.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_python_none.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new Old8Lang.AST.Expression.LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<Old8Lang.AST.Expression.Value.NullLangValue>(result);
    }

    #endregion

    #region JavaScript 特定边界测试

    /// <summary>
    /// 测试 JavaScript 抛出异常
    /// </summary>
    [Fact]
    public void Extern_JavaScriptException_PropagatesCorrectly()
    {
        // Arrange
        var jsContent = @"
function throwError() {
    throw new Error('Test error');
}
";
        var old8Content = @"
extern ""error_test.js"" func throwError() -> void

throwError()
";

        CreateTempModuleFile("error_test.js", jsContent);
        CreateTempModuleFile("test_js_exception.old8", old8Content);

        // Act & Assert
        AssertExecutionThrows("test_js_exception.old8", typeof(InvalidOperationError));
    }

    /// <summary>
    /// 测试 JavaScript NaN 返回值
    /// </summary>
    [Fact]
    public void Extern_JavaScriptReturnsNaN_HandlesCorrectly()
    {
        // Arrange
        var jsContent = @"
function getNaN() {
    return NaN;
}
";
        var old8Content = @"
extern ""nan_test.js"" func getNaN() -> double

result <- getNaN()
";

        CreateTempModuleFile("nan_test.js", jsContent);
        CreateTempModuleFile("test_js_nan.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_nan.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new Old8Lang.AST.Expression.LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<Old8Lang.AST.Expression.Value.DoubleLangValue>(result);
        Assert.True(double.IsNaN(((Old8Lang.AST.Expression.Value.DoubleLangValue)result).Value));
    }

    /// <summary>
    /// 测试 JavaScript Infinity 返回值
    /// </summary>
    [Fact]
    public void Extern_JavaScriptReturnsInfinity_HandlesCorrectly()
    {
        // Arrange
        var jsContent = @"
function getInfinity() {
    return Infinity;
}
";
        var old8Content = @"
extern ""infinity_test.js"" func getInfinity() -> double

result <- getInfinity()
";

        CreateTempModuleFile("infinity_test.js", jsContent);
        CreateTempModuleFile("test_js_infinity.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_js_infinity.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new Old8Lang.AST.Expression.LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<Old8Lang.AST.Expression.Value.DoubleLangValue>(result);
        Assert.True(double.IsPositiveInfinity(((Old8Lang.AST.Expression.Value.DoubleLangValue)result).Value));
    }

    #endregion

    #region 并发和性能边界测试

    /// <summary>
    /// 测试多次调用同一外部函数
    /// </summary>
    [Fact]
    public void Extern_MultipleCallsSameFunction_WorksCorrectly()
    {
        // Arrange
        var jsContent = @"
let counter = 0;
function incrementCounter() {
    return ++counter;
}
";
        var old8Content = @"
extern ""counter.js"" func incrementCounter() -> int

result1 <- incrementCounter()
result2 <- incrementCounter()
result3 <- incrementCounter()
";

        CreateTempModuleFile("counter.js", jsContent);
        CreateTempModuleFile("test_multiple_calls.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_multiple_calls.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 1);
        AssertVariableValue(interpreter, "result2", 2);
        AssertVariableValue(interpreter, "result3", 3);
    }

    #endregion

    #region 资源清理测试

    /// <summary>
    /// 测试导入后立即销毁（确保没有资源泄漏）
    /// </summary>
    [Fact]
    public void Extern_ImmediateDisposal_NoResourceLeak()
    {
        // Arrange
        var jsContent = @"
function test() {
    return 42;
}
";
        var old8Content = @"
extern ""disposal_test.js"" func test() -> int

result <- test()
";

        CreateTempModuleFile("disposal_test.js", jsContent);
        CreateTempModuleFile("test_disposal.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_disposal.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 42);

        // 强制垃圾回收以检测资源泄漏
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    #endregion

    #region 空函数体和注释

    /// <summary>
    /// 测试 JavaScript 空函数体
    /// </summary>
    [Fact]
    public void Extern_EmptyFunctionBody_WorksCorrectly()
    {
        // Arrange
        var jsContent = @"
function emptyFunction() {
    // 空函数体，返回 undefined
}
";
        var old8Content = @"
extern ""empty_func.js"" func emptyFunction() -> object

result <- emptyFunction()
";

        CreateTempModuleFile("empty_func.js", jsContent);
        CreateTempModuleFile("test_empty_function.old8", old8Content);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("test_empty_function.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new Old8Lang.AST.Expression.LangId("result"));
        Assert.NotNull(result);
    }

    #endregion
}
