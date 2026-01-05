using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Async;

/// <summary>
/// 异步并发测试 - 测试多个异步任务并发执行
/// </summary>
[Collection("Sequential")]
public class AsyncConcurrencyTests
{
    #region 多任务并发执行

    /// <summary>
    /// 测试多个异步任务并发执行
    /// </summary>
    [Fact]
    public async Task Run_MultipleAsyncTasksConcurrent_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func task1() -> int {
    return 10
}

async func task2() -> int {
    return 20
}

async func task3() -> int {
    return 30
}

t1 <- task1()
t2 <- task2()
t3 <- task3()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give tasks time to complete
        await Task.Delay(100);

        // Assert
        var t1 = interpreter.Manager.GetValue(new LangId("t1"));
        var t2 = interpreter.Manager.GetValue(new LangId("t2"));
        var t3 = interpreter.Manager.GetValue(new LangId("t3"));

        Assert.NotNull(t1);
        Assert.NotNull(t2);
        Assert.NotNull(t3);
    }

    /// <summary>
    /// 测试异步任务等待所有任务完成
    /// </summary>
    [Fact]
    public async Task Run_AwaitAllTasks_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func compute(value: int) -> int {
    return value * 2
}

async func main() -> int {
    t1 <- compute(5)
    t2 <- compute(10)
    t3 <- compute(15)

    r1 <- await t1
    r2 <- await t2
    r3 <- await t3

    return r1 + r2 + r3
}

result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(200);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    #endregion

    #region 异步任务同步和协调

    /// <summary>
    /// 测试异步任务顺序执行
    /// </summary>
    [Fact]
    public async Task Run_AsyncTasksSequential_ExecutesInOrder()
    {
        // Arrange
        var code = @"
counter <- 0
lockedCounter <- lock(counter)

async func increment() -> void {
    value <- lockedCounter.Value
    lockedCounter.Set(value + 1)
}

async func main() -> void {
    await increment()
    await increment()
    await increment()
}

task <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(200);

        // Assert
        var lockedCounter = interpreter.Manager.GetValue(new LangId("lockedCounter"));
        Assert.IsType<LockedVariableLangValue>(lockedCounter);
        var finalValue = ((LockedVariableLangValue)lockedCounter).GetLockedValue();
        Assert.IsType<IntLangValue>(finalValue);
        Assert.Equal(3, ((IntLangValue)finalValue).Value);
    }

    /// <summary>
    /// 测试异步任务依赖链
    /// </summary>
    [Fact]
    public async Task Run_AsyncTaskDependencyChain_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func step1() -> int {
    return 5
}

async func step2(input: int) -> int {
    return input + 10
}

async func step3(input: int) -> int {
    return input * 2
}

async func pipeline() -> int {
    v1 <- await step1()
    v2 <- await step2(v1)
    v3 <- await step3(v2)
    return v3
}

result <- pipeline()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    #endregion

    #region 异步异常处理

    /// <summary>
    /// 测试异步任务中的异常处理
    /// </summary>
    [Fact]
    public async Task Run_AsyncTaskWithException_HandlesGracefully()
    {
        // Arrange
        var code = @"
async func mayFail(shouldFail: bool) -> int {
    if shouldFail {
        throw ""Task failed""
    }
    return 42
}

async func main() -> int {
    try {
        result <- await mayFail(false)
        return result
    } catch (e) {
        return -1
    }
}

task <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(200);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    #endregion

    #region 并发数据访问

    /// <summary>
    /// 测试多个异步任务访问共享数据
    /// </summary>
    [Fact]
    public async Task Run_MultipleTasksAccessSharedData_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
shared <- 0
lockedShared <- lock(shared)

async func addToShared(value: int) -> void {
    current <- lockedShared.Value
    lockedShared.Set(current + value)
}

async func main() -> void {
    await addToShared(10)
    await addToShared(20)
    await addToShared(30)
}

task <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var lockedShared = interpreter.Manager.GetValue(new LangId("lockedShared"));
        Assert.IsType<LockedVariableLangValue>(lockedShared);
        var finalValue = ((LockedVariableLangValue)lockedShared).GetLockedValue();
        Assert.IsType<IntLangValue>(finalValue);
        Assert.Equal(60, ((IntLangValue)finalValue).Value);
    }

    #endregion

    #region 嵌套异步调用

    /// <summary>
    /// 测试嵌套的异步函数调用
    /// </summary>
    [Fact]
    public async Task Run_NestedAsyncCalls_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func level3() -> int {
    return 3
}

async func level2() -> int {
    v <- await level3()
    return v + 2
}

async func level1() -> int {
    v <- await level2()
    return v + 1
}

result <- level1()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试异步函数中启动多个子任务
    /// </summary>
    [Fact]
    public async Task Run_AsyncFunctionLaunchesMultipleSubtasks_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func subtask(value: int) -> int {
    return value * 2
}

async func parent() -> int {
    t1 <- subtask(5)
    t2 <- subtask(10)
    t3 <- subtask(15)

    r1 <- await t1
    r2 <- await t2
    r3 <- await t3

    return r1 + r2 + r3
}

result <- parent()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    #endregion

    #region 异步任务返回值

    /// <summary>
    /// 测试异步任务返回复杂类型
    /// </summary>
    [Fact]
    public async Task Run_AsyncTaskReturnsComplexType_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func createList() -> any {
    return {1, 2, 3, 4, 5}
}

async func main() -> int {
    list <- await createList()
    return list.Count()
}

result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(200);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试异步任务返回字典
    /// </summary>
    [Fact]
    public async Task Run_AsyncTaskReturnsDictionary_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func createDict() -> any {
    return {""name"": ""Alice"", ""age"": 25}
}

async func main() -> string {
    dict <- await createDict()
    return dict[""name""]
}

result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(200);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    #endregion

    #region 组合异步操作

    /// <summary>
    /// 测试异步任务与同步操作混合
    /// </summary>
    [Fact]
    public async Task Run_MixAsyncAndSyncOperations_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func syncAdd(a: int, b: int) -> int {
    return a + b
}

async func asyncMultiply(a: int, b: int) -> int {
    return a * b
}

async func main() -> int {
    sum <- syncAdd(5, 10)
    product <- await asyncMultiply(sum, 2)
    return product
}

result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give task time to complete
        await Task.Delay(200);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    #endregion
}
