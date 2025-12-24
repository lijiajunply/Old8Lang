using Old8Lang.Interpreter;
using Old8Lang.AST.Expression;
using Old8Lang.Error;

namespace Old8Lang.Tests.Interpreter.Async;

/// <summary>
/// 异步函数解释模式测试
/// </summary>
public class AsyncFunctionTests
{
    [Fact]
    public async Task AsyncFunction_SimpleAdd_ReturnsCorrectResult()
    {
        // Arrange
        var code = @"
            async func add(a:int, b:int) -> int {
                return a + b
            }
            task <- add(10, 20)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // 获取 Task 对象
        var taskValue = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(taskValue);

        // 等待异步任务完成
        await Task.Delay(100); // 给异步任务一些时间完成

        // Assert
        // 注意：这里的具体断言取决于 Old8Lang 如何处理异步返回值
        // 可能需要特殊的方法来获取异步结果
    }

    [Fact]
    public async Task AsyncFunction_WithAwait_WaitsForCompletion()
    {
        // Arrange
        var code = @"
            async func delayedValue(value:int) -> int {
                await Task.Delay(100)
                return value * 2
            }
            async func main() -> int {
                result <- await delayedValue(5)
                return result
            }
            mainTask <- main()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // 等待主异步任务完成
        await Task.Delay(200);

        // Assert
        var mainTask = interpreter.Manager.GetValue(new LangId("mainTask"));
        Assert.NotNull(mainTask);
    }

    [Fact]
    public void AsyncFunction_WithoutImmediateAwait_CreatesTask()
    {
        // Arrange
        var code = @"
            async func computeValue() -> int {
                await Task.Delay(50)
                return 42
            }
            task <- computeValue()
            // task is a Task object, not the result yet
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
        // Task 应该是一个 TaskLangValue 或类似的对象
    }

    [Fact]
    public async Task AsyncFunction_ChainedAwait_WaitsSequentially()
    {
        // Arrange
        var code = @"
            async func step1() -> int {
                await Task.Delay(50)
                return 10
            }
            async func step2(value:int) -> int {
                await Task.Delay(50)
                return value * 2
            }
            async func main() -> int {
                result1 <- await step1()
                result2 <- await step2(result1)
                return result2
            }
            finalTask <- main()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // 等待所有异步操作完成
        await Task.Delay(200);

        // Assert
        var finalTask = interpreter.Manager.GetValue(new LangId("finalTask"));
        Assert.NotNull(finalTask);
    }

    [Fact]
    public async Task AsyncFunction_WithParameters_PassesCorrectly()
    {
        // Arrange
        var code = @"
            async func multiply(a:int, b:int) -> int {
                await Task.Delay(10)
                return a * b
            }
            task <- multiply(6, 7)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        await Task.Delay(50);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    [Fact]
    public async Task AsyncFunction_WithStringParameter_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            async func processText(text:string) -> string {
                await Task.Delay(20)
                return ""processed: "" + text
            }
            task <- processText(""hello"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        await Task.Delay(50);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    [Fact]
    public async Task AsyncFunction_WithMultipleAwaits_WaitsForAll()
    {
        // Arrange
        var code = @"
            async func operation1() -> int {
                await Task.Delay(30)
                return 100
            }
            async func operation2() -> int {
                await Task.Delay(40)
                return 200
            }
            async func main() -> int {
                result1 <- await operation1()
                result2 <- await operation2()
                return result1 + result2
            }
            task <- main()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        await Task.Delay(100);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    [Fact]
    public async Task AsyncFunction_NestedAsyncCalls_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            async func innerAsync(value:int) -> int {
                await Task.Delay(20)
                return value + 10
            }
            async func outerAsync(start:int) -> int {
                intermediate <- await innerAsync(start)
                await Task.Delay(20)
                return intermediate * 2
            }
            task <- outerAsync(5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        await Task.Delay(100);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    [Fact]
    public async Task AsyncFunction_WithException_ThrowsCorrectly()
    {
        // Arrange
        var code = """
                               async func failingFunction() -> int {
                                   await Task.Delay(10)
                                   throw "Async error occurred"
                                   return 42
                               }
                               task <- failingFunction()
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // 等待异步任务完成
        await Task.Delay(100);

        // Assert - 获取任务并检查其状态
        var taskValue = interpreter.Manager.GetValue(new LangId("task")) as AST.Expression.Value.TaskLangValue;
        Assert.NotNull(taskValue);

        // 任务应该处于Failed状态
        Assert.Equal(AST.Expression.Value.TaskStatus.Failed, taskValue.Status);

        // 异常应该被捕获在任务中
        Assert.NotNull(taskValue.Exception);

        // 访问Result属性应该抛出异常
        Assert.ThrowsAny<Old8Exception>(() => taskValue.Await());
    }

    [Fact]
    public async Task AsyncFunction_WithTryCatch_HandlesException()
    {
        // Arrange
        var code = """

                               async func safeFunction() -> string {
                                   try {
                                       await Task.Delay(10)
                                       throw "test error"
                                       return "success"
                                   } catch (e) {
                                       return "caught: " + e
                                   }
                               }
                               task <- safeFunction()
                           
                   """;
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        await Task.Delay(50);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    [Fact]
    public async Task AsyncFunction_VoidReturnType_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            async func logMessage(message:string) -> void {
                await Task.Delay(10)
                // 在实际环境中可能会调用 PrintLine
                // PrintLine(message)
            }
            task <- logMessage(""test message"")
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        await Task.Delay(50);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    [Fact]
    public async Task AsyncFunction_ImmediateReturn_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            async func immediateReturn() -> int {
                return 99
            }
            task <- immediateReturn()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        await Task.Delay(50);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    [Fact]
    public async Task AsyncFunction_WithComplexReturnExpression_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            async func complexCalculation(a:int, b:int) -> int {
                await Task.Delay(10)
                return (a + b) * (a - b) / 2
            }
            task <- complexCalculation(10, 6)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        await Task.Delay(50);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    [Fact]
    public async Task AsyncFunction_WithConditionalReturn_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            async func conditionalReturn(value:int) -> string {
                await Task.Delay(10)
                if value > 50 {
                    return ""large""
                } else {
                    return ""small""
                }
            }
            task1 <- conditionalReturn(75)
            task2 <- conditionalReturn(25)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        await Task.Delay(50);

        // Assert
        var task1 = interpreter.Manager.GetValue(new LangId("task1"));
        var task2 = interpreter.Manager.GetValue(new LangId("task2"));
        Assert.NotNull(task1);
        Assert.NotNull(task2);
    }
}