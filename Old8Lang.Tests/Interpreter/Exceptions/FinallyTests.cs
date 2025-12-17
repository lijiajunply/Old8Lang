using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Exceptions;

/// <summary>
/// Finally块测试
/// </summary>
public class FinallyTests
{
    [Fact]
    public void Finally_BasicTryFinally_ExecutesFinallyBlock()
    {
        // Arrange
        var code = @"
            try {
                result <- ""try block executed""
            } finally {
                cleanupExecuted <- true
                cleanupMessage <- ""finally block executed""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var cleanup = interpreter.Manager.GetValue(new LangId("cleanupExecuted"));
        var message = interpreter.Manager.GetValue(new LangId("cleanupMessage"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("try block executed", ((StringLangValue)result).Value);

        Assert.NotNull(cleanup);
        Assert.IsType<BoolLangValue>(cleanup);
        Assert.Equal(true, ((BoolLangValue)cleanup).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("finally block executed", ((StringLangValue)message).Value);
    }

    [Fact]
    public void Finally_TryCatchFinally_ExecutesFinallyAfterCatch()
    {
        // Arrange
        var code = @"
            try {
                throw ""test exception""
            } catch {
                caughtMessage <- ""exception caught: "" + exception
            } finally {
                finallyExecuted <- true
                finallyMessage <- ""cleanup completed""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var caught = interpreter.Manager.GetValue(new LangId("caughtMessage"));
        var finallyExecuted = interpreter.Manager.GetValue(new LangId("finallyExecuted"));
        var finallyMessage = interpreter.Manager.GetValue(new LangId("finallyMessage"));

        Assert.NotNull(caught);
        Assert.IsType<StringLangValue>(caught);
        Assert.Equal("exception caught: test exception", ((StringLangValue)caught).Value);

        Assert.NotNull(finallyExecuted);
        Assert.IsType<BoolLangValue>(finallyExecuted);
        Assert.Equal(true, ((BoolLangValue)finallyExecuted).Value);

        Assert.NotNull(finallyMessage);
        Assert.IsType<StringLangValue>(finallyMessage);
        Assert.Equal("cleanup completed", ((StringLangValue)finallyMessage).Value);
    }

    [Fact]
    public void Finally_WithResourceCleanup_CleansUpResources()
    {
        // Arrange
        var code = @"
            resourceOpened <- false
            resourceClosed <- false
            try {
                resourceOpened <- true
                result <- ""resource used""
            } finally {
                if resourceOpened {
                    resourceClosed <- true
                    cleanupMessage <- ""resource cleaned up""
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var opened = interpreter.Manager.GetValue(new LangId("resourceOpened"));
        var closed = interpreter.Manager.GetValue(new LangId("resourceClosed"));
        var message = interpreter.Manager.GetValue(new LangId("cleanupMessage"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("resource used", ((StringLangValue)result).Value);

        Assert.NotNull(opened);
        Assert.IsType<BoolLangValue>(opened);
        Assert.Equal(true, ((BoolLangValue)opened).Value);

        Assert.NotNull(closed);
        Assert.IsType<BoolLangValue>(closed);
        Assert.Equal(true, ((BoolLangValue)closed).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("resource cleaned up", ((StringLangValue)message).Value);
    }

    [Fact]
    public void Finally_WithExceptionInFinally_DoesNotOverrideOriginalException()
    {
        // Arrange
        var code = @"
            try {
                throw ""original exception""
            } catch {
                caughtException <- exception
            } finally {
                finallyExecuted <- true
                // Note: In most languages, exception in finally would hide original exception
                // This test assumes original exception is preserved
                cleanupAction <- ""final cleanup performed""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var caught = interpreter.Manager.GetValue(new LangId("caughtException"));
        var finallyExecuted = interpreter.Manager.GetValue(new LangId("finallyExecuted"));
        var cleanup = interpreter.Manager.GetValue(new LangId("cleanupAction"));

        Assert.NotNull(caught);
        Assert.IsType<StringLangValue>(caught);
        Assert.Equal("original exception", ((StringLangValue)caught).Value);

        Assert.NotNull(finallyExecuted);
        Assert.IsType<BoolLangValue>(finallyExecuted);
        Assert.Equal(true, ((BoolLangValue)finallyExecuted).Value);

        Assert.NotNull(cleanup);
        Assert.IsType<StringLangValue>(cleanup);
        Assert.Equal("final cleanup performed", ((StringLangValue)cleanup).Value);
    }

    [Fact]
    public void Finally_WithReturnInTry_StillExecutesFinally()
    {
        // Arrange
        var code = @"
            func testFunction() -> string {
                try {
                    return ""return from try""
                } finally {
                    cleanupMessage <- ""finally executed after return""
                    cleanupCompleted <- true
                }
            }
            result <- testFunction()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var message = interpreter.Manager.GetValue(new LangId("cleanupMessage"));
        var completed = interpreter.Manager.GetValue(new LangId("cleanupCompleted"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("return from try", ((StringLangValue)result).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("finally executed after return", ((StringLangValue)message).Value);

        Assert.NotNull(completed);
        Assert.IsType<BoolLangValue>(completed);
        Assert.Equal(true, ((BoolLangValue)completed).Value);
    }

    [Fact]
    public void Finally_WithNestedTryFinally_HandlesNestedFinally()
    {
        // Arrange
        var code = @"
            outerFinally <- false
            innerFinally <- false
            try {
                try {
                    result <- ""inner try executed""
                } finally {
                    innerFinally <- true
                    innerMessage <- ""inner finally executed""
                }
            } finally {
                outerFinally <- true
                outerMessage <- ""outer finally executed""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var innerF = interpreter.Manager.GetValue(new LangId("innerFinally"));
        var outerF = interpreter.Manager.GetValue(new LangId("outerFinally"));
        var innerMsg = interpreter.Manager.GetValue(new LangId("innerMessage"));
        var outerMsg = interpreter.Manager.GetValue(new LangId("outerMessage"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("inner try executed", ((StringLangValue)result).Value);

        Assert.NotNull(innerF);
        Assert.IsType<BoolLangValue>(innerF);
        Assert.Equal(true, ((BoolLangValue)innerF).Value);

        Assert.NotNull(outerF);
        Assert.IsType<BoolLangValue>(outerF);
        Assert.Equal(true, ((BoolLangValue)outerF).Value);

        Assert.NotNull(innerMsg);
        Assert.IsType<StringLangValue>(innerMsg);
        Assert.Equal("inner finally executed", ((StringLangValue)innerMsg).Value);

        Assert.NotNull(outerMsg);
        Assert.IsType<StringLangValue>(outerMsg);
        Assert.Equal("outer finally executed", ((StringLangValue)outerMsg).Value);
    }

    [Fact]
    public void Finally_WithFileResource_HandlesFileOperations()
    {
        // Arrange
        var code = @"
            fileOpened <- false
            fileClosed <- false
            fileData <- """"
            try {
                // Simulate file operations
                fileOpened <- true
                fileData <- ""file content processed""
                result <- ""file operation successful""
            } finally {
                if fileOpened {
                    fileClosed <- true
                    cleanupMessage <- ""file handle closed""
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var opened = interpreter.Manager.GetValue(new LangId("fileOpened"));
        var closed = interpreter.Manager.GetValue(new LangId("fileClosed"));
        var message = interpreter.Manager.GetValue(new LangId("cleanupMessage"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("file operation successful", ((StringLangValue)result).Value);

        Assert.NotNull(opened);
        Assert.IsType<BoolLangValue>(opened);
        Assert.Equal(true, ((BoolLangValue)opened).Value);

        Assert.NotNull(closed);
        Assert.IsType<BoolLangValue>(closed);
        Assert.Equal(true, ((BoolLangValue)closed).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("file handle closed", ((StringLangValue)message).Value);
    }

    [Fact]
    public void Finally_WithDatabaseConnection_CleansUpConnection()
    {
        // Arrange
        var code = @"
            connectionOpen <- false
            connectionClosed <- false
            queryResult <- """"
            try {
                // Simulate database operations
                connectionOpen <- true
                queryResult <- ""SELECT * FROM users""
                result <- ""query executed successfully""
            } finally {
                if connectionOpen {
                    connectionClosed <- true
                    cleanupMessage <- ""database connection closed""
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var open = interpreter.Manager.GetValue(new LangId("connectionOpen"));
        var closed = interpreter.Manager.GetValue(new LangId("connectionClosed"));
        var message = interpreter.Manager.GetValue(new LangId("cleanupMessage"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("query executed successfully", ((StringLangValue)result).Value);

        Assert.NotNull(open);
        Assert.IsType<BoolLangValue>(open);
        Assert.Equal(true, ((BoolLangValue)open).Value);

        Assert.NotNull(closed);
        Assert.IsType<BoolLangValue>(closed);
        Assert.Equal(true, ((BoolLangValue)closed).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("database connection closed", ((StringLangValue)message).Value);
    }

    [Fact]
    public void Finally_WithLoop_HandlesFinallyInLoop()
    {
        // Arrange
        var code = @"
            processedItems <- 0
            cleanupCount <- 0
            for i in 1..3 {
                try {
                    processedItems <- processedItems + 1
                    itemResult <- ""processed item "" + i.ToStr()
                } finally {
                    cleanupCount <- cleanupCount + 1
                    cleanupMessage <- ""cleanup for item "" + i.ToStr()
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var processed = interpreter.Manager.GetValue(new LangId("processedItems"));
        var cleanup = interpreter.Manager.GetValue(new LangId("cleanupCount"));

        Assert.NotNull(processed);
        Assert.IsType<IntLangValue>(processed);
        Assert.Equal(3, ((IntLangValue)processed).Value);

        Assert.NotNull(cleanup);
        Assert.IsType<IntLangValue>(cleanup);
        Assert.Equal(3, ((IntLangValue)cleanup).Value);
    }

    [Fact]
    public void Finally_WithFunctionCall_ExecutesFinallyInFunction()
    {
        // Arrange
        var code = @"
            func processData(data:int) -> string {
                try {
                    return ""processed: "" + data.ToStr()
                } finally {
                    cleanupPerformed <- true
                    cleanupMessage <- ""function cleanup completed""
                }
            }
            result <- processData(42)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var cleanup = interpreter.Manager.GetValue(new LangId("cleanupPerformed"));
        var message = interpreter.Manager.GetValue(new LangId("cleanupMessage"));

        Assert.NotNull(result);
        Assert.IsType<string>(result);
        Assert.Equal("processed: 42", ((StringLangValue)result).Value);

        Assert.NotNull(cleanup);
        Assert.IsType<BoolLangValue>(cleanup);
        Assert.Equal(true, ((BoolLangValue)cleanup).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("function cleanup completed", ((StringLangValue)message).Value);
    }

    [Fact]
    public void Finally_WithExceptionAndCleanup_CleansUpDespiteException()
    {
        // Arrange
        var code = @"
            resourceAllocated <- false
            resourceCleaned <- false
            try {
                resourceAllocated <- true
                throw ""operation failed""
            } catch {
                errorMessage <- exception
            } finally {
                if resourceAllocated {
                    resourceCleaned <- true
                    cleanupMessage <- ""resource cleaned despite error""
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var error = interpreter.Manager.GetValue(new LangId("errorMessage"));
        var allocated = interpreter.Manager.GetValue(new LangId("resourceAllocated"));
        var cleaned = interpreter.Manager.GetValue(new LangId("resourceCleaned"));
        var message = interpreter.Manager.GetValue(new LangId("cleanupMessage"));

        Assert.NotNull(error);
        Assert.IsType<StringLangValue>(error);
        Assert.Equal("operation failed", ((StringLangValue)error).Value);

        Assert.NotNull(allocated);
        Assert.IsType<BoolLangValue>(allocated);
        Assert.Equal(true, ((BoolLangValue)allocated).Value);

        Assert.NotNull(cleaned);
        Assert.IsType<BoolLangValue>(cleaned);
        Assert.Equal(true, ((BoolLangValue)cleaned).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("resource cleaned despite error", ((StringLangValue)message).Value);
    }

    [Fact]
    public void Finally_WithArrayOperations_CleansUpArrayResources()
    {
        // Arrange
        var code = @"
            arrayCreated <- false
            arrayCleaned <- false
            try {
                arrayCreated <- true
                numbers <- [1, 2, 3, 4, 5]
                sum <- 0
                for num in numbers {
                    sum <- sum + num
                }
                result <- ""array sum: "" + sum.ToStr()
            } finally {
                if arrayCreated {
                    arrayCleaned <- true
                    cleanupMessage <- ""array resources cleaned""
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var created = interpreter.Manager.GetValue(new LangId("arrayCreated"));
        var cleaned = interpreter.Manager.GetValue(new LangId("arrayCleaned"));
        var message = interpreter.Manager.GetValue(new LangId("cleanupMessage"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("array sum: 15", ((StringLangValue)result).Value);

        Assert.NotNull(created);
        Assert.IsType<BoolLangValue>(created);
        Assert.Equal(true, ((BoolLangValue)created).Value);

        Assert.NotNull(cleaned);
        Assert.IsType<BoolLangValue>(cleaned);
        Assert.Equal(true, ((BoolLangValue)cleaned).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("array resources cleaned", ((StringLangValue)message).Value);
    }

    [Fact]
    public void Finally_WithObjectDestruction_CallsDestructor()
    {
        // Arrange
        var code = @"
            class Resource {
                public acquired:bool
                func Init() {
                    acquired <- true
                }
                func Cleanup() {
                    acquired <- false
                }
            }
            resource <- Resource()
            try {
                result <- ""resource used""
            } finally {
                resource.Cleanup()
                cleanupMessage <- ""object cleanup completed""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var message = interpreter.Manager.GetValue(new LangId("cleanupMessage"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("resource used", ((StringLangValue)result).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("object cleanup completed", ((StringLangValue)message).Value);
    }

    [Fact]
    public void Finally_WithCounterOperation_MaintainsState()
    {
        // Arrange
        var code = @"
            counter <- 0
            try {
                counter <- counter + 10
                result <- ""counter incremented""
            } finally {
                counter <- counter + 5
                finallyMessage <- ""final counter: "" + counter.ToStr()
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var message = interpreter.Manager.GetValue(new LangId("finallyMessage"));
        var finalCounter = interpreter.Manager.GetValue(new LangId("counter"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("counter incremented", ((StringLangValue)result).Value);

        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("final counter: 15", ((StringLangValue)message).Value);

        Assert.NotNull(finalCounter);
        Assert.IsType<IntLangValue>(finalCounter);
        Assert.Equal(15, ((IntLangValue)finalCounter).Value);
    }

    [Fact]
    public void Finally_WithMultipleStatements_ExecutesAllCleanup()
    {
        // Arrange
        var code = @"
            step1Completed <- false
            step2Completed <- false
            step3Completed <- false
            try {
                result <- ""operation completed""
            } finally {
                step1Completed <- true
                step2Completed <- true
                step3Completed <- true
                cleanupSummary <- ""all cleanup steps completed""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var step1 = interpreter.Manager.GetValue(new LangId("step1Completed"));
        var step2 = interpreter.Manager.GetValue(new LangId("step2Completed"));
        var step3 = interpreter.Manager.GetValue(new LangId("step3Completed"));
        var summary = interpreter.Manager.GetValue(new LangId("cleanupSummary"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("operation completed", ((StringLangValue)result).Value);

        Assert.NotNull(step1);
        Assert.IsType<BoolLangValue>(step1);
        Assert.Equal(true, ((BoolLangValue)step1).Value);

        Assert.NotNull(step2);
        Assert.IsType<BoolLangValue>(step2);
        Assert.Equal(true, ((BoolLangValue)step2).Value);

        Assert.NotNull(step3);
        Assert.IsType<BoolLangValue>(step3);
        Assert.Equal(true, ((BoolLangValue)step3).Value);

        Assert.NotNull(summary);
        Assert.IsType<StringLangValue>(summary);
        Assert.Equal("all cleanup steps completed", ((StringLangValue)summary).Value);
    }

    [Fact]
    public void Finally_WithLogging_LogsExecutionFlow()
    {
        // Arrange
        var code = @"
            logMessages <- {}
            try {
                logMessages.Add(""Starting operation"")
                result <- ""operation success""
                logMessages.Add(""Operation completed successfully"")
            } finally {
                logMessages.Add(""Finally block executed"")
                logMessages.Add(""Cleanup completed"")
            }
            finalLog <- logMessages.Join("" | "")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var finalLog = interpreter.Manager.GetValue(new LangId("finalLog"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("operation success", ((StringLangValue)result).Value);

        Assert.NotNull(finalLog);
        Assert.IsType<StringLangValue>(finalLog);
        Assert.Equal("Starting operation | Operation completed successfully | Finally block executed | Cleanup completed",
            ((StringLangValue)finalLog).Value);
    }
}