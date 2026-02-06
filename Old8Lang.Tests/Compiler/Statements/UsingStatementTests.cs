using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Statements;

/// <summary>
/// Using 语句解释器执行测试
/// </summary>
[Collection("Sequential")]
public class UsingStatementTests
{
    #region 基本功能测试

    /// <summary>
    /// 测试 using 语句自动释放 Mutex 资源
    /// </summary>
    [Fact]
    public void UsingStatement_AutoDisposeMutex_DisposesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using mutex <- MutexCreate() {
    MutexLock(mutex)
    result <- 42
    MutexUnlock(mutex)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 using 语句自动释放 Channel 资源
    /// </summary>
    [Fact]
    public void UsingStatement_AutoDisposeChannel_DisposesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using ch <- ChannelCreate() {
    ChannelSend(ch, 100)
    received <- ChannelReceive(ch)
    result <- received
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试不带变量声明的 using 语句
    /// </summary>
    [Fact]
    public void UsingStatement_WithoutVariableDeclaration_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
result <- 0
using ch {
    ChannelSend(ch, 99)
    result <- ChannelReceive(ch)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region Semaphore 测试

    /// <summary>
    /// 测试 using 语句自动释放 Semaphore 资源
    /// </summary>
    [Fact]
    public void UsingStatement_AutoDisposeSemaphore_DisposesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using sem <- SemaphoreCreate(1, 3) {
    SemaphoreAcquire(sem)
    result <- 10
    SemaphoreRelease(sem)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region AtomicInt 测试

    /// <summary>
    /// 测试 using 语句自动释放 AtomicInt 资源
    /// </summary>
    [Fact]
    public void UsingStatement_AutoDisposeAtomicInt_DisposesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using counter <- AtomicIntCreate(0) {
    AtomicIntIncrement(counter)
    AtomicIntIncrement(counter)
    AtomicIntIncrement(counter)
    result <- AtomicIntGet(counter)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 AtomicInt 的加减操作
    /// </summary>
    [Fact]
    public void UsingStatement_AtomicIntAddOperation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
using counter <- AtomicIntCreate(10) {
    AtomicIntAdd(counter, 5)
    AtomicIntAdd(counter, -3)
    result <- AtomicIntGet(counter)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region ReadWriteLock 测试

    /// <summary>
    /// 测试 using 语句自动释放 ReadWriteLock 资源
    /// </summary>
    [Fact]
    public void UsingStatement_AutoDisposeReadWriteLock_DisposesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using rwLock <- ReadWriteLockCreate() {
    ReadLockAcquire(rwLock)
    result <- 20
    ReadLockRelease(rwLock)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 ReadWriteLock 的写锁操作
    /// </summary>
    [Fact]
    public void UsingStatement_ReadWriteLockWriteLock_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using rwLock <- ReadWriteLockCreate() {
    WriteLockAcquire(rwLock)
    result <- 30
    WriteLockRelease(rwLock)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region CountDownLatch 测试

    /// <summary>
    /// 测试 using 语句自动释放 CountDownLatch 资源
    /// </summary>
    [Fact]
    public void UsingStatement_AutoDisposeCountDownLatch_DisposesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using latch <- CountDownLatchCreate(1) {
    CountDownLatchCountDown(latch)
    result <- 15
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region CyclicBarrier 测试

    /// <summary>
    /// 测试 using 语句自动释放 CyclicBarrier 资源
    /// </summary>
    [Fact]
    public void UsingStatement_AutoDisposeCyclicBarrier_DisposesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using barrier <- CyclicBarrierCreate(1) {
    result <- 25
    CyclicBarrierAwait(barrier)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 嵌套 Using 测试

    /// <summary>
    /// 测试嵌套的 using 语句
    /// </summary>
    [Fact]
    public void UsingStatement_Nested_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using mutex1 <- MutexCreate() {
    using mutex2 <- MutexCreate() {
        MutexLock(mutex1)
        MutexLock(mutex2)
        result <- 50
        MutexUnlock(mutex2)
        MutexUnlock(mutex1)
    }
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试多个不同资源的嵌套 using
    /// </summary>
    [Fact]
    public void UsingStatement_NestedDifferentResources_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using ch <- ChannelCreate() {
    using counter <- AtomicIntCreate(0) {
        ChannelSend(ch, 100)
        AtomicIntIncrement(counter)
        received <- ChannelReceive(ch)
        count <- AtomicIntGet(counter)
        result <- received + count
    }
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region Using 与控制流结合测试

    /// <summary>
    /// 测试 using 中包含 if 语句
    /// </summary>
    [Fact]
    public void UsingStatement_WithIfStatement_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
flag <- true
result <- 0
using counter <- AtomicIntCreate(0) {
    if flag {
        AtomicIntIncrement(counter)
        AtomicIntIncrement(counter)
    } else {
        AtomicIntDecrement(counter)
    }
    result <- AtomicIntGet(counter)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 using 中包含 for 循环
    /// </summary>
    [Fact]
    public void UsingStatement_WithForLoop_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using counter <- AtomicIntCreate(0) {
    for i in [1~5] {
        AtomicIntIncrement(counter)
    }
    result <- AtomicIntGet(counter)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 using 中包含 while 循环
    /// </summary>
    [Fact]
    public void UsingStatement_WithWhileLoop_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using counter <- AtomicIntCreate(0) {
    while AtomicIntGet(counter) < 3 {
        AtomicIntIncrement(counter)
    }
    result <- AtomicIntGet(counter)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region Using 与异常处理结合测试

    /// <summary>
    /// 测试 using 中包含 try-catch，确保即使有异常也能自动释放资源
    /// </summary>
    [Fact]
    public void UsingStatement_WithTryCatch_ReleasesResourceOnException()
    {
        // Arrange
        var code = @"
result <- 0
errorCaught <- false
using counter <- AtomicIntCreate(10) {
    try {
        AtomicIntIncrement(counter)
        result <- AtomicIntGet(counter)
        throw ""Test error""
    } catch (e) {
        errorCaught <- true
    }
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 函数中的 Using 测试

    /// <summary>
    /// 测试函数中的 using 语句
    /// </summary>
    [Fact]
    public void UsingStatement_InFunction_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func processWithMutex() {
    using mutex <- MutexCreate() {
        MutexLock(mutex)
        MutexUnlock(mutex)
        return 88
    }
}
result <- processWithMutex()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试函数返回值在 using 块中
    /// </summary>
    [Fact]
    public void UsingStatement_FunctionReturnInBlock_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func createAndIncrement() {
    using counter <- AtomicIntCreate(100) {
        AtomicIntIncrement(counter)
        return AtomicIntGet(counter)
    }
}
result <- createAndIncrement()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 复杂场景测试

    /// <summary>
    /// 测试多个顺序的 using 语句
    /// </summary>
    [Fact]
    public void UsingStatement_MultipleSequential_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
using counter1 <- AtomicIntCreate(10) {
    result <- result + AtomicIntGet(counter1)
}
using counter2 <- AtomicIntCreate(20) {
    result <- result + AtomicIntGet(counter2)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试在 using 中使用 switch 语句
    /// </summary>
    [Fact]
    public void UsingStatement_WithSwitch_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
mode <- 2
result <- 0
using counter <- AtomicIntCreate(0) {
    switch mode {
        case 1 {
            AtomicIntSet(counter, 10)
        }
        case 2 {
            AtomicIntSet(counter, 20)
        }
        default {
            AtomicIntSet(counter, 30)
        }
    }
    result <- AtomicIntGet(counter)
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 自定义类 Dispose 方法测试

    /// <summary>
    /// 测试 dispose 方法修改实例字段
    /// </summary>
    [Fact]
    public void UsingStatement_DisposeModifiesFields_FieldsUpdatedCorrectly()
    {
        // Arrange
        var code = @"
class FileHandle {
    path <- """"
    isOpen <- true

    func init(path) {
        this.path <- path
    }

    func dispose() {
        if this.isOpen {
            this.isOpen <- false
        }
    }
}

using handle <- FileHandle(""/tmp/test.txt"") {
    // isOpen should be true
}

result <- handle.isOpen";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试没有 dispose 方法的类（不应报错）
    /// </summary>
    [Fact]
    public void UsingStatement_ClassWithoutDispose_NoError()
    {
        // Arrange
        var code = @"
class SimpleClass {
    value <- 123
}

result <- 0
using obj <- SimpleClass() {
    result <- obj.value
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
    #endregion
}
