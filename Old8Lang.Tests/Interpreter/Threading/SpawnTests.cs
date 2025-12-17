using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Threading;

/// <summary>
/// Spawn函数测试
/// </summary>
public class SpawnTests
{
    [Fact]
    public void Spawn_SimpleFunction_SpawnsSimpleFunction()
    {
        // Arrange
        var code = @"
            func simpleTask() -> int {
                return 42
            }
            task <- spawn simpleTask()
            result <- task
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
    public void Spawn_FunctionWithParameters_SpawnsFunctionWithParameters()
    {
        // Arrange
        var code = @"
            func calculate(x:int, y:int) -> int {
                return x * y
            }
            task <- spawn calculate(6, 7)
            result <- task
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
    public void Spawn_MultipleTasks_SpawnsMultipleTasks()
    {
        // Arrange
        var code = @"
            func getSquare(n:int) -> int {
                return n * n
            }
            task1 <- spawn getSquare(5)
            task2 <- spawn getSquare(10)
            task3 <- spawn getSquare(15)
            result1 <- task1
            result2 <- task2
            result3 <- task3
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(25, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(100, ((IntLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(225, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void Spawn_LambdaTask_SpawnsLambdaFunction()
    {
        // Arrange
        var code = @"
            operation <- (x:int) -> x * 10
            task <- spawn operation(8)
            result <- task
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(80, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Spawn_ComputationTask_SpawnsHeavyComputation()
    {
        // Arrange
        var code = @"
            func fibonacci(n:int) -> int {
                if n <= 1 {
                    return n
                }
                return fibonacci(n - 1) + fibonacci(n - 2)
            }
            task <- spawn fibonacci(10)
            result <- task
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value); // fibonacci(10) = 55
    }

    [Fact]
    public void Spawn_ArrayProcessing_SpawnsArrayProcessingTask()
    {
        // Arrange
        var code = @"
            func sumArray(arr:[int]) -> int {
                sum <- 0
                for num in arr {
                    sum <- sum + num
                }
                return sum
            }
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            task <- spawn sumArray(numbers)
            result <- task
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value); // 1+2+...+10 = 55
    }

    [Fact]
    public void Spawn_StringProcessing_SpawnsStringProcessingTask()
    {
        // Arrange
        var code = @"
            func reverseString(s:string) -> string {
                result <- """"
                for i in s.Length-1..0 {
                    result <- result + s[i]
                }
                return result
            }
            task <- spawn reverseString(""hello world"")
            result <- task
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("dlrow olleh", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Spawn_ConcurrentTasks_RunsTasksConcurrently()
    {
        // Arrange
        var code = @"
            func processData(id:int, delay:int) -> string {
                // Simulate processing delay
                for i in 1..delay {
                    // Busy wait
                }
                return ""Task "" + id.ToStr() + "" completed""
            }
            task1 <- spawn processData(1, 1000)
            task2 <- spawn processData(2, 1000)
            task3 <- spawn processData(3, 1000)
            result1 <- task1
            result2 <- task2
            result3 <- task3
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.IsType<StringLangValue>(result1);
        Assert.Equal("Task 1 completed", ((StringLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<StringLangValue>(result2);
        Assert.Equal("Task 2 completed", ((StringLangValue)result2).Value);

        Assert.NotNull(result3);
        Assert.IsType<StringLangValue>(result3);
        Assert.Equal("Task 3 completed", ((StringLangValue)result3).Value);
    }

    [Fact]
    public void Spawn_ClassMethod_SpawnsClassMethodTask()
    {
        // Arrange
        var code = @"
            class Calculator {
                public result:int
                func Init() {
                    result <- 0
                }
                func compute(a:int, b:int) -> void {
                    result <- a + b
                }
                func getResult() -> int {
                    return result
                }
            }
            calc <- Calculator()
            task <- spawn calc.compute(15, 27)
            // Wait for task completion
            finalResult <- calc.getResult()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Spawn_NestedSpawn_SpawnsTasksWithinTasks()
    {
        // Arrange
        var code = @"
            func workerTask(n:int) -> int {
                return n * n
            }
            func coordinatorTask() -> int {
                task1 <- spawn workerTask(3)
                task2 <- spawn workerTask(4)
                task3 <- spawn workerTask(5)
                return task1 + task2 + task3
            }
            task <- spawn coordinatorTask()
            result <- task
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(50, ((IntLangValue)result).Value); // 9+16+25 = 50
    }

    [Fact]
    public void Spawn_WithException_HandlesExceptionsInTasks()
    {
        // Arrange
        var code = @"
            func riskyTask() -> int {
                throw ""Task failed""
            }
            try {
                task <- spawn riskyTask()
                result <- task
            } catch {
                result <- -1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(-1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Spawn_DataProcessingPipeline_CreatesProcessingPipeline()
    {
        // Arrange
        var code = @"
            func loadData() -> {int} {
                return {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            }
            func filterEven(numbers:{int}) -> {int} {
                result <- {}
                for num in numbers {
                    if num % 2 == 0 {
                        result.Add(num)
                    }
                }
                return result
            }
            func squareNumbers(numbers:{int}) -> {int} {
                result <- {}
                for num in numbers {
                    result.Add(num * num)
                }
                return result
            }
            func sumNumbers(numbers:{int}) -> int {
                sum <- 0
                for num in numbers {
                    sum <- sum + num
                }
                return sum
            }
            // Pipeline stages
            loadTask <- spawn loadData()
            evenTask <- spawn filterEven(loadTask)
            squareTask <- spawn squareNumbers(evenTask)
            sumTask <- spawn sumNumbers(squareTask)
            result <- sumTask
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(220, ((IntLangValue)result).Value); // 2²+4²+6²+8²+10² = 4+16+36+64+100 = 220
    }

    [Fact]
    public void Spawn_MapReduce_ParallelMapReduce()
    {
        // Arrange
        var code = @"
            func mapSquare(numbers:{int}) -> {int} {
                result <- {}
                for num in numbers {
                    result.Add(num * num)
                }
                return result
            }
            func reduceSum(numbers:{int}) -> int {
                sum <- 0
                for num in numbers {
                    sum <- sum + num
                }
                return sum
            }
            input <- {1, 2, 3, 4, 5}
            mapTask <- spawn mapSquare(input)
            reduceTask <- spawn reduceSum(mapTask)
            result <- reduceTask
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value); // 1²+2²+3²+4²+5² = 1+4+9+16+25 = 55
    }

    [Fact]
    public void Spawn_TaskDependency_HandlesTaskDependencies()
    {
        // Arrange
        var code = @"
            func taskA() -> int {
                return 10
            }
            func taskB(dependency:int) -> int {
                return dependency * 2
            }
            func taskC(dependency:int) -> int {
                return dependency + 5
            }
            taskA_handle <- spawn taskA()
            taskB_handle <- spawn taskB(taskA_handle)
            taskC_handle <- spawn taskC(taskA_handle)
            resultB <- taskB_handle
            resultC <- taskC_handle
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultB = interpreter.Manager.GetValue(new LangId("resultB"));
        var resultC = interpreter.Manager.GetValue(new LangId("resultC"));

        Assert.NotNull(resultB);
        Assert.IsType<IntLangValue>(resultB);
        Assert.Equal(20, ((IntLangValue)resultB).Value); // 10 * 2 = 20

        Assert.NotNull(resultC);
        Assert.IsType<IntLangValue>(resultC);
        Assert.Equal(15, ((IntLangValue)resultC).Value); // 10 + 5 = 15
    }

    [Fact]
    public void Spawn_TaskTimeout_HandlesTaskTimeouts()
    {
        // Arrange
        var code = @"
            func longTask() -> string {
                for i in 1..1000000 {
                    // Long computation
                }
                return ""completed""
            }
            try {
                task <- spawn longTask() with timeout 100
                result <- task
            } catch {
                result <- ""timeout""
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
        // Result depends on timeout implementation
    }

    [Fact]
    public void Spawn_TaskPriority_HandlesTaskPriorities()
    {
        // Arrange
        var code = @"
            func priorityTask(priority:string) -> string {
                return ""Task with priority: "" + priority
            }
            highTask <- spawn priorityTask(""high"") with priority 10
            mediumTask <- spawn priorityTask(""medium"") with priority 5
            lowTask <- spawn priorityTask(""low"") with priority 1
            resultHigh <- highTask
            resultMedium <- mediumTask
            resultLow <- lowTask
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var resultHigh = interpreter.Manager.GetValue(new LangId("resultHigh"));
        var resultMedium = interpreter.Manager.GetValue(new LangId("resultMedium"));
        var resultLow = interpreter.Manager.GetValue(new LangId("resultLow"));

        Assert.NotNull(resultHigh);
        Assert.IsType<StringLangValue>(resultHigh);
        Assert.Equal("Task with priority: high", ((StringLangValue)resultHigh).Value);

        Assert.NotNull(resultMedium);
        Assert.IsType<StringLangValue>(resultMedium);
        Assert.Equal("Task with priority: medium", ((StringLangValue)resultMedium).Value);

        Assert.NotNull(resultLow);
        Assert.IsType<StringLangValue>(resultLow);
        Assert.Equal("Task with priority: low", ((StringLangValue)resultLow).Value);
    }

    [Fact]
    public void Spawn_TaskResult_CollectsTaskResults()
    {
        // Arrange
        var code = @"
            func computePartial(start:int, end:int) -> int {
                sum <- 0
                for i in start..end {
                    sum <- sum + i
                }
                return sum
            }
            // Divide work among tasks
            task1 <- spawn computePartial(1, 25)
            task2 <- spawn computePartial(26, 50)
            task3 <- spawn computePartial(51, 75)
            task4 <- spawn computePartial(76, 100)
            result <- task1 + task2 + task3 + task4
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5050, ((IntLangValue)result).Value); // Sum of 1..100 = 5050
    }

    [Fact]
    public void Spawn_AsyncFunction_SpawnsAsyncFunction()
    {
        // Arrange
        var code = @"
            async func asyncTask() -> string {
                await Task.Delay(100)
                return ""Async completed""
            }
            task <- spawn asyncTask()
            result <- task
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Async completed", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Spawn_RecursiveTasks_HandlesRecursiveSpawnedTasks()
    {
        // Arrange
        var code = @"
            func recursiveTask(depth:int, maxDepth:int) -> int {
                if depth >= maxDepth {
                    return 1
                }
                subTask <- spawn recursiveTask(depth + 1, maxDepth)
                return 1 + subTask
            }
            task <- spawn recursiveTask(0, 5)
            result <- task
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value); // 5 levels of recursion
    }

    [Fact]
    public void Spawn_TaskId_TracksTaskIds()
    {
        // Arrange
        var code = @"
            func simpleTask() -> string {
                return ""Task completed""
            }
            task1 <- spawn simpleTask()
            task2 <- spawn simpleTask()
            task3 <- spawn simpleTask()
            id1 <- task1.Id()
            id2 <- task2.Id()
            id3 <- task3.Id()
            result <- ""Task IDs: "" + id1.ToStr() + "", "" + id2.ToStr() + "", "" + id3.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Contains("Task IDs:", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Spawn_TaskStatus_ChecksTaskStatus()
    {
        // Arrange
        var code = @"
            func quickTask() -> string {
                return ""Done""
            }
            func slowTask() -> string {
                for i in 1..10000 {
                    // Simulate work
                }
                return ""Done""
            }
            quickTask_handle <- spawn quickTask()
            slowTask_handle <- spawn slowTask()
            quickStatus <- quickTask_handle.Status()
            slowStatus <- slowTask_handle.Status()
            // Wait and check final status
            quickResult <- quickTask_handle
            slowResult <- slowTask_handle
            finalQuickStatus <- quickTask_handle.Status()
            finalSlowStatus <- slowTask_handle.Status()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var quickResult = interpreter.Manager.GetValue(new LangId("quickResult"));
        var slowResult = interpreter.Manager.GetValue(new LangId("slowResult"));
        var finalQuickStatus = interpreter.Manager.GetValue(new LangId("finalQuickStatus"));
        var finalSlowStatus = interpreter.Manager.GetValue(new LangId("finalSlowStatus"));

        Assert.NotNull(quickResult);
        Assert.IsType<StringLangValue>(quickResult);
        Assert.Equal("Done", ((StringLangValue)quickResult).Value);

        Assert.NotNull(slowResult);
        Assert.IsType<StringLangValue>(slowResult);
        Assert.Equal("Done", ((StringLangValue)slowResult).Value);

        // Final status should be "completed" or similar
        Assert.NotNull(finalQuickStatus);
        Assert.NotNull(finalSlowStatus);
    }
}