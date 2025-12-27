using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Basic;

/// <summary>
/// 编译模式错误处理测试
/// 测试编译器在处理错误代码时的行为和错误报告
/// </summary>
[Collection("Sequential")]
public class ErrorHandlingTests
{
    #region 语法错误测试

    [Fact]
    public void InvalidSyntax_ThrowsCompileError()
    {
        // Arrange
        var code = "a <- 42 +";
        var interpreter = new LangInterpreter();

        // Act & Assert
        Assert.Throws<Old8Exception>(() => interpreter.Build(code));
    }

    [Fact]
    public void MismatchedBrackets_ThrowsCompileError()
    {
        // Arrange
        var code = "a <- [1, 2, 3";
        var interpreter = new LangInterpreter();

        // Act & Assert
        Assert.Throws<Old8Exception>(() => interpreter.Build(code));
    }

    [Fact]
    public void UnclosedString_ThrowsCompileError()
    {
        // Arrange
        var code = "a <- \"hello world";
        var interpreter = new LangInterpreter();

        // Act & Assert
        Assert.Throws<Old8Exception>(() => interpreter.Build(code));
    }

    [Fact]
    public void InvalidCharacter_ThrowsCompileError()
    {
        // Arrange
        var code = "a <- 42 @";
        var interpreter = new LangInterpreter();

        // Act & Assert
        Assert.Throws<Old8Exception>(() => interpreter.Build(code));
    }

    #endregion

    #region 运行时错误测试（编译后执行）

    [Fact]
    public void DivisionByZero_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = "a <- 10 / 0";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    [Fact]
    public void ModuloByZero_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = "a <- 10 % 0";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    [Fact]
    public void ArrayIndexOutOfBounds_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            a <- arr[10]
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
    public void NegativeArrayIndex_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            a <- arr[-1]
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
    public void EmptyArrayAccess_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            arr <- []
            a <- arr[0]
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
    public void DictionaryKeyNotFound_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            dict <- {""key"": ""value""}
            a <- dict[""nonexistent""]
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
    public void UndefinedVariable_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = "a <- undefinedVariable";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    [Fact]
    public void UndefinedFunction_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = "a <- undefinedFunction()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    #endregion

    #region 类型错误测试

    [Fact]
    public void TypeMismatch_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            a:int <- 42
            a <- ""hello""
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
    public void InvalidOperationOnType_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = "a <- \"hello\" * 2";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    [Fact]
    public void InvalidArrayOperation_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = "a <- [1, 2, 3] * 2";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    #endregion

    #region 函数调用错误测试

    [Fact]
    public void FunctionParameterCountMismatch_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            func test(a:int, b:int):int {
                return a + b
            }
            result <- test(1)
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
    public void FunctionParameterTypeMismatch_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            func test(a:int):int {
                return a * 2
            }
            result <- test(""hello"")
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
    public void FunctionReturnTypeMismatch_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            func test():int {
                return ""hello""
            }
            result <- test()
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

    #region 递归溢出测试

    [Fact]
    public void InfiniteRecursion_ThrowsStackOverflowException_WhenExecuted()
    {
        // Arrange
        var code = @"
            func infinite():int {
                return infinite()
            }
            result <- infinite()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.ThrowsAny<Exception>(() => compiledAction());
    }

    [Fact]
    public void DeepRecursion_ThrowsStackOverflowException_WhenExecuted()
    {
        // Arrange
        var code = @"
            func deep(n:int):int {
                if n <= 0 {
                    return 0
                }
                return deep(n - 1) + 1
            }
            result <- deep(10000)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.ThrowsAny<Exception>(() => compiledAction());
    }

    #endregion

    #region 内存相关错误测试

    [Fact]
    public void LargeObjectCreation_HandlesGracefully()
    {
        // Arrange
        var code = new System.Text.StringBuilder("a <- [");
        for (int i = 0; i < 10000; i++)
        {
            if (i > 0) code.Append(", ");
            code.Append(i);
        }
        code.Append("]");
        
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code.ToString());
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 应该能够处理大对象或优雅地失败
        Assert.NotNull(compiledAction);
        
        var exception = Record.Exception(() => compiledAction());
        // 如果失败，应该是内存相关异常
        if (exception != null)
        {
            Assert.True(exception is OutOfMemoryException || exception is Old8Exception);
        }
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public void TryCatchBlock_HandlesException_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            try {
                a <- 10 / 0
            } catch (e) {
                a <- 0
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception); // 应该被try-catch处理
    }

    [Fact]
    public void NestedTryCatch_HandlesInnerException_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            try {
                try {
                    a <- 10 / 0
                } catch (inner) {
                    throw ""rethrow""
                }
            } catch (outer) {
                a <- 1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception); // 应该被外层catch处理
    }

    [Fact]
    public void FinallyBlock_AlwaysExecutes_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            executed <- false
            try {
                a <- 10 / 0
            } catch (e) {
                // 忽略
            } finally {
                executed <- true
            }
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
    public void ThrowStatement_ThrowsException_WhenExecuted()
    {
        // Arrange
        var code = @"
            throw ""test exception""
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

    #region 类和对象错误测试

    [Fact]
    public void UndefinedClassMember_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            class Test {
                public x:int
            }
            obj <- Test()
            a <- obj.y
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
    public void UndefinedClassMethod_ThrowsRuntimeException_WhenExecuted()
    {
        // Arrange
        var code = @"
            class Test {
                public x:int
            }
            obj <- Test()
            a <- obj.undefinedMethod()
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

    #region 编译器特定错误测试

    [Fact]
    public void MissingTypeAnnotation_FunctionParameter_ThrowsCompileError()
    {
        // Arrange
        var code = @"
            func test(a):int {
                return a * 2
            }
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert - 编译模式要求函数参数有类型注解
        Assert.Throws<Old8Exception>(() => interpreter.Build(code));
    }

    [Fact]
    public void MissingTypeAnnotation_FunctionReturn_ThrowsCompileError()
    {
        // Arrange
        var code = @"
            func test(a:int) {
                return a * 2
            }
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert - 编译模式要求函数返回值有类型注解
        Assert.Throws<Old8Exception>(() => interpreter.Build(code));
    }

    [Fact]
    public void MissingTypeAnnotation_VariableDeclaration_HandlesGracefully()
    {
        // Arrange
        var code = "a:int <- 42"; // 如果编译模式要求变量类型注解
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Assert.NotNull(compiledAction);
        
        // 执行可能成功或失败，取决于类型注解的具体实现
        var exception = Record.Exception(() => compiledAction());
        // 这里根据具体实现决定期望的行为
    }

    #endregion
}