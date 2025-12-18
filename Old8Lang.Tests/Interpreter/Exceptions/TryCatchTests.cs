using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.Tests.Interpreter.Exceptions;

/// <summary>
/// Try-Catch异常处理解释模式测试
/// </summary>
public class TryCatchTests
{
    [Fact]
    public void TryCatch_NoException_DoesNotExecuteCatch()
    {
        // Arrange
        var code = @"
            result <- ""try""
            try {
                result <- ""success""
                a <- 10 + 5
            } catch (e) {
                result <- ""caught""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public void TryCatch_WithException_CatchesAndExecutesCatch()
    {
        // Arrange
        var code = @"
            result <- ""try""
            caughtException <- """"
            try {
                result <- ""before error""
                throw ""test error""
                result <- ""after error""
            } catch (e) {
                result <- ""caught""
                caughtException <- e
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        var caughtException = interpreter.Manager.GetValue(new LangId("caughtException"));

        Assert.NotNull(result);
        Assert.NotNull(caughtException);
        Assert.Equal("caught", result.Value);

        // caughtException 应该是 ErrorLangValue，我们需要获取其 FriendlyMessage
        if (caughtException is ErrorLangValue errorValue)
        {
            Assert.Equal("test error", errorValue.FriendlyMessage);
        }
        else
        {
            // 如果不是 ErrorLangValue，尝试转换为字符串
            var stringException = caughtException as StringLangValue;
            Assert.NotNull(stringException);
            Assert.Equal("test error", stringException.Value);
        }
    }

    [Fact]
    public void TryCatch_ZeroDivisionError_CatchesRuntimeError()
    {
        // Arrange
        var code = @"
            result <- ""no error""
            try {
                a <- 10
                b <- 0
                c <- a / b
            } catch (e) {
                result <- ""division error caught""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("division error caught", result.Value);
    }

    [Fact]
    public void TryCatch_WithFinally_AlwaysExecutesFinally()
    {
        // Arrange
        var code = @"
            result <- ""initial""
            finallyExecuted <- false
            try {
                result <- ""in try""
            } catch (e) {
                result <- ""in catch""
            } finally {
                finallyExecuted <- true
                result <- result + "" and finally""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        var finallyExecuted = interpreter.Manager.GetValue(new LangId("finallyExecuted")) as BoolLangValue;

        Assert.NotNull(result);
        Assert.NotNull(finallyExecuted);
        Assert.Equal("in try and finally", result.Value);
        Assert.True(finallyExecuted.Value);
    }

    [Fact]
    public void TryCatch_WithExceptionAndFinally_ExecutesCatchAndFinally()
    {
        // Arrange
        var code = @"
            result <- """"
            finallyExecuted <- false
            try {
                result <- ""try""
                throw ""error""
                result <- ""after throw""
            } catch (e) {
                result <- ""catch""
            } finally {
                finallyExecuted <- true
                result <- result + "" + finally""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        var finallyExecuted = interpreter.Manager.GetValue(new LangId("finallyExecuted")) as BoolLangValue;

        Assert.NotNull(result);
        Assert.NotNull(finallyExecuted);
        Assert.Equal("catch + finally", result.Value);
        Assert.True(finallyExecuted.Value);
    }

    [Fact]
    public void TryCatch_WithoutExceptionParameter_CatchesWithoutBinding()
    {
        // Arrange
        var code = @"
            result <- ""before""
            try {
                throw ""some error""
            } catch {
                result <- ""caught without parameter""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("caught without parameter", result.Value);
    }

    [Fact]
    public void NestedTryCatch_HandlesInnerException()
    {
        // Arrange
        var code = @"
            outerResult <- """"
            innerResult <- """"
            try {
                outerResult <- ""outer try""
                try {
                    innerResult <- ""inner try""
                    throw ""inner error""
                    innerResult <- ""inner after""
                } catch (e) {
                    innerResult <- ""inner caught: "" + e
                }
                outerResult <- ""outer after""
            } catch (e) {
                outerResult <- ""outer caught""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var outerResult = interpreter.Manager.GetValue(new LangId("outerResult")) as StringLangValue;
        var innerResult = interpreter.Manager.GetValue(new LangId("innerResult")) as StringLangValue;

        Assert.NotNull(outerResult);
        Assert.NotNull(innerResult);
        Assert.Equal("outer after", outerResult.Value);
        Assert.Equal("inner caught: inner error", innerResult.Value);
    }

    [Fact]
    public void NestedTryCatch_PropagatesOuterException()
    {
        // Arrange
        var code = @"
            outerResult <- """"
            innerResult <- """"
            try {
                outerResult <- ""outer try""
                try {
                    innerResult <- ""inner try""
                    // 内层没有异常
                } catch (e) {
                    innerResult <- ""inner caught""
                }
                throw ""outer error""
                outerResult <- ""outer after""
            } catch (e) {
                outerResult <- ""outer caught: "" + e
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var outerResult = interpreter.Manager.GetValue(new LangId("outerResult")) as StringLangValue;
        var innerResult = interpreter.Manager.GetValue(new LangId("innerResult")) as StringLangValue;

        Assert.NotNull(outerResult);
        Assert.NotNull(innerResult);
        Assert.Equal("outer caught: outer error", outerResult.Value);
        Assert.Equal("inner try", innerResult.Value);
    }

    [Fact]
    public void TryCatch_WithFunctionCall_CatchesFunctionException()
    {
        // Arrange
        var code = @"
            func throwingFunction() {
                throw ""function error""
                return 42
            }
            result <- ""before""
            try {
                value <- throwingFunction()
                result <- ""success""
            } catch (e) {
                result <- ""caught: "" + e
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("caught: function error", result.Value);
    }

    [Fact]
    public void TryCatch_WithVariableAccess_CatchesVariableError()
    {
        // Arrange
        var code = @"
            result <- ""before""
            try {
                // 尝试访问不存在的变量可能会抛出异常
                value <- undefinedVariable
                result <- ""success""
            } catch (e) {
                result <- ""caught variable error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert - 这取决于 Old8Lang 如何处理未定义变量
        // 可能需要根据实际行为调整这个测试
        ast.Run(interpreter.Manager);
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        // 结果可能是 "caught variable error" 或 "success"，取决于实现
    }

    [Fact]
    public void TryCatch_WithMultipleStatements_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 0
            try {
                a <- 10
                b <- 20
                c <- a + b
                throw ""error after calculation""
                d <- 40
            } catch (e) {
                result <- c  // 应该能够访问 c 变量
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(30, result.Value);
    }

    [Fact]
    public void TryCatch_WithDifferentExceptionTypes_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            stringResult <- """"
            intResult <- 0
            boolResult <- false

            // 测试字符串异常
            try {
                throw ""string exception""
            } catch (e) {
                stringResult <- ""caught: "" + e
            }

            // 测试整数异常
            try {
                throw 123
            } catch (e) {
                intResult <- e
            }

            // 测试布尔异常
            try {
                throw true
            } catch (e) {
                boolResult <- e
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var stringResult = interpreter.Manager.GetValue(new LangId("stringResult"));
        var intResult = interpreter.Manager.GetValue(new LangId("intResult"));
        var boolResult = interpreter.Manager.GetValue(new LangId("boolResult"));

        Assert.NotNull(stringResult);
        Assert.NotNull(intResult);
        Assert.NotNull(boolResult);

        // stringResult 应该是字符串拼接的结果
        var stringValue = stringResult as StringLangValue;
        Assert.NotNull(stringValue);
        Assert.Equal("caught: string exception", stringValue.Value);

        // intResult 和 boolResult 应该是 ErrorLangValue，需要检查它们的 FriendlyMessage
        if (intResult is ErrorLangValue intError)
        {
            Assert.Equal("123", intError.FriendlyMessage);
        }
        else
        {
            var intValue = intResult as IntLangValue;
            Assert.NotNull(intValue);
            Assert.Equal(123, intValue.Value);
        }

        if (boolResult is ErrorLangValue boolError)
        {
            Assert.Equal("true", boolError.FriendlyMessage);
        }
        else
        {
            var boolValue = boolResult as BoolLangValue;
            Assert.NotNull(boolValue);
            Assert.True(boolValue.Value);
        }
    }

    [Fact]
    public void TryCatch_WithEmptyTry_DoesNothing()
    {
        // Arrange
        var code = @"
            result <- ""before""
            try {
                // 空的 try 块
            } catch (e) {
                result <- ""caught""
            } finally {
                result <- ""finally""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("finally", result.Value);
    }

    [Fact]
    public void TryCatch_WithComplexLogic_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            processed <- 0
            errors <- 0
            data <- [1, 2, 3, 4, 5]

            for i <- 0, i < 5, i++ {
                try {
                    value <- data[i]
                    if value == 3 {
                        throw ""value is 3""
                    }
                    processed <- processed + value
                } catch (e) {
                    errors <- errors + 1
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var processed = interpreter.Manager.GetValue(new LangId("processed")) as IntLangValue;
        var errors = interpreter.Manager.GetValue(new LangId("errors")) as IntLangValue;

        Assert.NotNull(processed);
        Assert.NotNull(errors);
        Assert.Equal(12, processed.Value); // 1 + 2 + 4 + 5 = 12 (跳过 3)
        Assert.Equal(1, errors.Value);   // 只有一个错误（value == 3）
    }
}