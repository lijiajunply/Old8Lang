using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Exceptions;

/// <summary>
/// 编译器模式下的异常处理测试 - 嵌套异常处理
/// </summary>
public class NestedExceptionTests
{
    private readonly ITestOutputHelper _output;

    public NestedExceptionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void NestedTryCatch_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                try {
                    throw ""inner exception""
                } catch (inner) {
                    result <- result + ""inner caught: "" + inner
                }
            } catch (outer) {
                result <- result + ""outer caught: "" + outer
            }
            Assert.Equal(""inner caught: inner exception"", result)
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
    public void NestedTryCatchWithRethrow_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                try {
                    throw ""inner exception""
                } catch (inner) {
                    result <- result + ""inner caught: "" + inner
                    throw  // 重新抛出异常
                }
            } catch (outer) {
                result <- result + ""; outer caught: "" + outer
            }
            Assert.Equal(""inner caught: inner exception; outer caught: inner exception"", result)
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
    public void TripleNestedTryCatch_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                try {
                    try {
                        throw ""deepest exception""
                    } catch (deepest) {
                        result <- result + ""deepest: "" + deepest
                    }
                } catch (middle) {
                    result <- result + ""; middle: "" + middle
                }
            } catch (outer) {
                result <- result + ""; outer: "" + outer
            }
            Assert.Equal(""deepest: deepest exception"", result)
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
    public void NestedTryCatchFinally_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            try {
                result <- result + ""outer try;""
                try {
                    result <- result + ""inner try;""
                    throw ""inner error""
                } catch (inner) {
                    result <- result + ""inner catch: "" + inner
                } finally {
                    result <- result + ""inner finally;""
                }
            } finally {
                result <- result + ""outer finally;""
            }
            Assert.Equal(""outer try;inner try;inner catch: inner error;inner finally;outer finally;"", result)
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
    public void NestedExceptionInFunctions_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func innerFunction() -> void {
                try {
                    throw ""function inner error""
                } catch (e) {
                    throw ""function outer error: "" + e
                }
            }
            
            func outerFunction() -> void {
                try {
                    innerFunction()
                } catch (e) {
                    throw ""function call error: "" + e
                }
            }
            
            result <- """"
            try {
                outerFunction()
            } catch (e) {
                result <- e
            }
            
            Assert.Equal(""function call error: function outer error: function inner error"", result)
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
    public void NestedExceptionWithDifferentTypes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- {}
            
            try {
                try {
                    try {
                        throw 42  // 数字异常
                    } catch (e) {
                        result.Add(""level3: "" + e.ToStr())
                        throw ""level3 error""  // 转换为字符串异常
                    }
                } catch (e) {
                    result.Add(""level2: "" + e)
                    throw [""error"", ""array"", e]  // 转换为数组异常
                }
            } catch (e) {
                result.Add(""level1: "" + e.ToStr())
            }
            
            Assert.Equal(3, result.Count())
            Assert.Equal(""level3: 42"", result[0])
            Assert.Equal(""level2: level3 error"", result[1])
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
    public void NestedExceptionInLoops_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            
            try {
                i <- 0
                while i < 3 {
                    try {
                        if i == 1 {
                            throw ""loop error at "" + i.ToStr()
                        }
                        result <- result + ""i="" + i.ToStr() + "";""
                    } catch (e) {
                        result <- result + ""caught: "" + e + "";""
                    }
                    i <- i + 1
                }
            } catch (e) {
                result <- result + ""outer caught: "" + e
            }
            
            Assert.Equal(""i=0;caught: loop error at 1;i=2;"", result)
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
    public void DeeplyNestedExceptionPropagation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func level5() -> void {
                throw ""level 5 error""
            }
            
            func level4() -> void {
                try {
                    level5()
                } catch (e) {
                    throw ""level 4 caught: "" + e
                }
            }
            
            func level3() -> void {
                try {
                    level4()
                } catch (e) {
                    throw ""level 3 caught: "" + e
                }
            }
            
            func level2() -> void {
                try {
                    level3()
                } catch (e) {
                    throw ""level 2 caught: "" + e
                }
            }
            
            func level1() -> void {
                try {
                    level2()
                } catch (e) {
                    throw ""level 1 caught: "" + e
                }
            }
            
            result <- """"
            try {
                level1()
            } catch (e) {
                result <- e
            }
            
            expected <- ""level 1 caught: level 2 caught: level 3 caught: level 4 caught: level 5 error""
            Assert.Equal(expected, result)
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
    public void NestedExceptionWithConditionalRethrow_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- """"
            
            func processValue(value:int) -> void {
                try {
                    if value < 0 {
                        throw ""negative value: "" + value.ToStr()
                    }
                    if value > 100 {
                        throw ""value too large: "" + value.ToStr()
                    }
                } catch (e) {
                    // 只在特定条件下重新抛出
                    if value == 50 {
                        throw ""critical error: "" + e
                    } else {
                        result <- result + ""handled: "" + e + "";""
                    }
                }
            }
            
            try {
                processValue(-5)    // 被处理
                processValue(50)    // 被重新抛出
                processValue(150)   // 不会执行到这里
            } catch (e) {
                result <- result + ""rethrown: "" + e
            }
            
            Assert.Equal(""handled: negative value: -5;rethrown: critical error: negative value: -5"", result)
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
    public void NestedExceptionWithResourceManagement_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 模拟嵌套资源管理
            outer_resource_opened <- false
            inner_resource_opened <- false
            outer_resource_closed <- false
            inner_resource_closed <- false
            
            func openOuterResource() -> void {
                outer_resource_opened <- true
            }
            
            func closeOuterResource() -> void {
                outer_resource_closed <- true
            }
            
            func openInnerResource() -> void {
                inner_resource_opened <- true
            }
            
            func closeInnerResource() -> void {
                inner_resource_closed <- true
            }
            
            result <- """"
            try {
                openOuterResource()
                try {
                    openInnerResource()
                    throw ""operation failed""
                } catch (e) {
                    result <- ""inner caught: "" + e
                } finally {
                    closeInnerResource()
                }
            } catch (e) {
                result <- result + ""; outer caught: "" + e
            } finally {
                closeOuterResource()
            }
            
            Assert.True(outer_resource_opened)
            Assert.True(inner_resource_opened)
            Assert.True(inner_resource_closed)
            Assert.True(outer_resource_closed)
            Assert.Equal(""inner caught: operation failed"", result)
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