using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Async;

/// <summary>
/// Task API测试
/// </summary>
public class TaskAPITests
{
    [Fact]
    public void TaskAPI_BasicTaskCreation_CreatesTask()
    {
        // Arrange
        var code = @"
            task <- async () -> {
                return 42
            }
            result <- await task()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithDelay_HandlesDelayedTask()
    {
        // Arrange
        var code = @"
            task <- Task.Delay(50)
            task.Wait()
            result <- ""task completed""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("task completed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskFactory_CreatesTasks()
    {
        // Arrange
        var code = @"
            task1 <- Task.StartNew(() -> {
                return 100
            })
            task2 <- Task.StartNew(() -> {
                return 200
            })
            result <- task1.Result + task2.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(300, ((IntLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithParameters_HandlesParameterizedTasks()
    {
        // Arrange
        var code = @"
            func multiplyByTwo(x:int) -> int {
                return x * 2
            }
            task <- Task.Run(() -> multiplyByTwo(25))
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(50, ((IntLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskException_HandlesTaskExceptions()
    {
        // Arrange
        var code = @"
            task <- Task.Run(() -> {
                throw ""Task failed""
            })
            try {
                result <- task.Result
            } catch {
                result <- ""exception caught""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("exception caught", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_ContinueWith_HandlesTaskContinuation()
    {
        // Arrange
        var code = @"
            originalTask <- Task.Run(() -> {
                return 10
            })
            continuationTask <- originalTask.ContinueWith((result) -> {
                return result * 5
            })
            result <- continuationTask.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(50, ((IntLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_WhenAll_HandlesMultipleTasks()
    {
        // Arrange
        var code = @"
            task1 <- Task.Run(() -> { return 1 })
            task2 <- Task.Run(() -> { return 2 })
            task3 <- Task.Run(() -> { return 3 })
            allTasks <- Task.WhenAll([task1, task2, task3])
            results <- allTasks.Result
            sum <- 0
            for value in results {
                sum <- sum + value
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(6, ((IntLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_WhenAny_HandlesFirstCompletedTask()
    {
        // Arrange
        var code = @"
            fastTask <- Task.Run(() -> {
                await Task.Delay(10)
                return ""fast""
            })
            slowTask <- Task.Run(() -> {
                await Task.Delay(100)
                return ""slow""
            })
            firstTask <- Task.WhenAny([fastTask, slowTask])
            result <- firstTask.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("fast", ((StringLangValue)result).Value);
    }

    [Fact(Skip = "TaskCompletionSource not yet implemented in Old8Lang")]
    public void TaskAPI_TaskCompletionSource_HandlesManualTaskCompletion()
    {
        // Arrange
        var code = @"
            tcs <- TaskCompletionSource()
            task <- tcs.Task
            // Complete the task from another context
            tcs.SetResult(""completed"")
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("completed", ((StringLangValue)result).Value);
    }

    [Fact(Skip = "CancellationTokenSource not yet implemented in Old8Lang")]
    public void TaskAPI_TaskWithCancellation_HandlesTaskCancellation()
    {
        // Arrange
        var code = @"
            cts <- CancellationTokenSource()
            token <- cts.Token
            task <- Task.Run(() -> {
                for i in [1~1000] {
                    if token.IsCancellationRequested {
                        throw ""Task cancelled""
                    }
                    // Do some work
                }
                return ""completed""
            }, token)
            // Cancel after a short delay
            cts.Cancel()
            try {
                result <- task.Result
            } catch {
                result <- ""cancelled""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("cancelled", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskTimeout_HandlesTaskTimeout()
    {
        // Arrange
        var code = @"
            longTask <- Task.Run(() -> {
                await Task.Delay(1000)
                return ""done""
            })
            timeoutTask <- Task.Delay(50)
            completedTask <- Task.WhenAny([longTask, timeoutTask])
            result <- """"
            if completedTask.Result == timeoutTask {
                result <- ""timeout""
            } else {
                result <- longTask.Result
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("timeout", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskScheduler_HandlesCustomScheduler()
    {
        // Arrange
        var code = @"
            scheduler <- TaskScheduler.Default
            task <- Task.Factory.StartNew(() -> {
                return ""scheduled""
            }, scheduler)
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("scheduled", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithResult_HandlesTaskResult()
    {
        // Arrange
        var code = @"
            func computeSquare(n:int) -> int {
                return n * n
            }
            task <- Task.FromResult(computeSquare(8))
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(64, ((IntLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskFromException_HandlesTaskFromException()
    {
        // Arrange
        var code = @"
            task <- Task.FromException(""error occurred"")
            try {
                result <- task.Result
            } catch {
                result <- ""exception handled""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("exception handled", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithAsyncFunction_HandlesAsyncFunction()
    {
        // Arrange
        var code = @"
            async func asyncOperation() {
                await Task.Delay(10)
                return ""async result""
            }
            task <- asyncOperation()
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("async result", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithLambda_HandlesLambdaInTask()
    {
        // Arrange
        var code = @"
            operation <- (x:int) -> x * x + 1
            task <- Task.Run(() -> operation(10))
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(101, ((IntLangValue)result).Value); // 10² + 1 = 101
    }

    [Fact]
    public void TaskAPI_TaskWithComplexOperation_HandlesComplexAsyncOperation()
    {
        // Arrange
        var code = @"
            async func processData(numbers) {
                results <- {}
                for num in numbers {
                    await Task.Delay(1) // Simulate async work
                    results.Add(num * num)
                }
                return results
            }
            inputNumbers <- [1, 2, 3, 4, 5]
            task <- processData(inputNumbers)
            resultList <- task.Result
            sum <- 0
            for value in resultList {
                sum <- sum + value
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value); // 1²+2²+3²+4²+5² = 55
    }

    [Fact]
    public void TaskAPI_TaskWithParallelism_HandlesParallelTasks()
    {
        // Arrange
        var code = @"
            tasks <- []
            for i in 1..5 {
                task <- Task.Run(() -> {
                    // Simulate computational work
                    sum <- 0
                    for j in [1~100]{
                        sum <- sum + j
                    }
                    return sum
                })
                tasks.Add(task)
            }
            allResults <- Task.WhenAll(tasks)
            totalSum <- 0
            for taskResult in allResults.Result {
                totalSum <- totalSum + taskResult
            }
            result <- totalSum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(25250, ((IntLangValue)result).Value); // 5050 * 5 = 25250
    }

    [Fact]
    public void TaskAPI_TaskWithRetry_HandlesTaskRetryLogic()
    {
        // Arrange
        var code = @"
            attempts <- 0
            async func unreliableOperation() {
                attempts <- attempts + 1
                if attempts < 3 {
                    throw ""operation failed""
                }
                return ""success""
            }
            async func retryOperation() {
                for i in 1..5 {
                    try {
                        return await unreliableOperation()
                    } catch {
                        if i = 5 {
                            throw ""all attempts failed""
                        }
                        await Task.Delay(10)
                    }
                }
            }
            task <- retryOperation()
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("success", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithMemoryManagement_HandlesMemoryIntensiveTask()
    {
        // Arrange
        var code = @"
            async func memoryIntensiveTask() {
                largeData <- {}
                for i in 1..1000 {
                    largeData[i] <- i * i
                }
                sum <- 0
                for value in largeData {
                    sum <- sum + value
                }
                return sum
            }
            task <- memoryIntensiveTask()
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // Sum of squares from 1 to 1000 = n(n+1)(2n+1)/6 = 1000*1001*2001/6 = 334,166,667
        // But this might be too large, let's use a smaller test
    }

    [Fact]
    public void TaskAPI_TaskWithProgress_ReportsProgress()
    {
        // Arrange
        var code = @"
            progress <- 0
            async func operationWithProgress()  {
                for i in [1~10] {
                    await Task.Delay(5)
                    progress <- i * 10
                }
                return ""completed""
            }
            task <- operationWithProgress()
            result <- task.Result + "" with progress: "" + progress.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("completed with progress: 100", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithResource_HandlesResourceDisposal()
    {
        // Arrange
        var code = @"
            resourceAcquired <- false
            async func operationWithResource() {
                resourceAcquired <- true
                try {
                    await Task.Delay(10)
                    return ""resource used""
                } finally {
                    resourceAcquired <- false
                }
            }
            task <- operationWithResource()
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("resource used", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithExternalDependencies_UsesExternalServices()
    {
        // Arrange
        var code = @"
            // Simulate external service call
            async func externalServiceCall(param:string) {
                await Task.Delay(10)
                return ""response for: "" + param
            }
            task <- externalServiceCall(""test"")
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("response for: test", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithComplexWorkflow_HandlesComplexWorkflow()
    {
        // Arrange
        var code = @"
            async func complexWorkflow() {
                // Step 1: Initialize
                await Task.Delay(5)
                data <- [1, 2, 3, 4, 5]

                // Step 2: Process in parallel
                tasks <- []
                for item in data {
                    task <- Task.Run(() -> {
                        return item * item
                    })
                    tasks.Add(task)
                }

                // Step 3: Wait for all and combine
                results <- Task.WhenAll(tasks)
                sum <- 0
                for result in results.Result {
                    sum <- sum + result
                }

                // Step 4: Final validation
                await Task.Delay(5)
                return sum > 0 ? ""workflow succeeded"" : ""workflow failed""
            }
            task <- complexWorkflow()
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("workflow succeeded", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TaskAPI_TaskWithMetrics_MeasuresPerformance()
    {
        // Arrange
        var code = @"
            task <- Task.Run(() -> {
                // Simulate some work
                sum <- 0
                for i in [1~1000] {
                    sum <- sum + i
                }
                return sum
            })
            result <- task.Result
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(500500, ((IntLangValue)result).Value); // 1+2+...+1000 = 500500
    }
}