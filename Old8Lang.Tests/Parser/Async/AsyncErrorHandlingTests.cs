using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Async;

/// <summary>
/// 异步编程错误处理测试
/// </summary>
[Collection("Sequential")]
public class AsyncErrorHandlingTests
{
    #region async/await try-catch

    /// <summary>
    /// 测试async函数中的try-catch
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncTryCatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func safeLoadData(url:string) -> string {
    try {
        data <- await fetchData(url)
        return data
    } catch (error) {
        return ""Error: "" + error.ToStr()
    }
}

async func fetchData(url:string) -> string {
    if url == ""bad_url"" {
        throw ""Invalid URL""
    }
    return ""Data from "" + url
}

result1 <- await safeLoadData(""good_url"")
result2 <- await safeLoadData(""bad_url"")";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试async函数中的多层try-catch
    /// </summary>
    [Fact]
    public void ParseProgram_NestedAsyncTryCatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func robustLoad() -> string {
    try {
        user <- await loadUser()
        try {
            profile <- await loadProfile(user)
            return profile
        } catch (profileError) {
            return ""Profile error: "" + profileError.ToStr()
        }
    } catch (userError) {
        return ""User error: "" + userError.ToStr()
    }
}

async func loadUser() -> string {
    return ""User123""
}

async func loadProfile(user:string) -> string {
    if user == ""error"" {
        throw ""Profile not found""
    }
    return ""Profile for "" + user
}

result <- await robustLoad()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试async函数中的finally块
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncTryCatchFinally_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func processDataWithCleanup() -> string {
    connection <- null
    try {
        connection <- await openConnection()
        data <- await fetchData(connection)
        return data
    } catch (error) {
        return ""Error: "" + error.ToStr()
    } finally {
        if connection != null {
            await closeConnection(connection)
        }
    }
}

async func openConnection() -> string {
    return ""Connection established""
}

async func fetchData(connection:string) -> string {
    return ""Data processed""
}

async func closeConnection(connection:string) -> void {
    PrintLine(""Connection closed"")
}

result <- await processDataWithCleanup()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region async 异常传播

    /// <summary>
    /// 测试async函数中的异常传播
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncExceptionPropagation_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func processData() -> string {
    try {
        step1 <- await dangerousOperation1()
        step2 <- await dangerousOperation2(step1)
        return step2
    } catch (error) {
        return ""Caught: "" + error.ToStr()
    }
}

async func dangerousOperation1() -> string {
    // 可能抛出异常
    return ""Step1 result""
}

async func dangerousOperation2(input:string) -> string {
    if input == ""error"" {
        throw ""Operation failed""
    }
    return ""Step2 result""
}

result <- await processData()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试async函数调用链中的异常处理
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncCallChainExceptionHandling_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func topFunction() -> string {
    try {
        result <- await middleFunction()
        return result
    } catch (error) {
        return ""Top level caught: "" + error.ToStr()
    }
}

async func middleFunction() -> string {
    try {
        result <- await bottomFunction()
        return result
    } catch (error) {
        // 重新抛出异常或包装后抛出
        throw ""Middle wrapper: "" + error.ToStr()
    }
}

async func bottomFunction() -> string {
    throw ""Original error""
}

finalResult <- await topFunction()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region async 超时和取消

    /// <summary>
    /// 测试async函数超时处理
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncTimeoutHandling_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func loadWithTimeout(url:string, timeoutMs:int) -> string {
    startTime <- CurrentTime()
    try {
        result <- await fetchDataWithTimeout(url, timeoutMs)
        return result
    } catch (error) {
        return ""Timeout or error: "" + error.ToStr()
    }
}

async func fetchDataWithTimeout(url:string, timeoutMs:int) -> string {
    // 模拟长时间操作
    await simulateDelay(timeoutMs + 1000)  // 故意超时
    return ""Data from "" + url
}

async func simulateDelay(ms:int) -> void {
    // 模拟延迟
    PrintLine(""Delaying for "" + ms.ToStr() + "" ms"")
}

result <- await loadWithTimeout(""http://example.com"", 5000)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region async 错误边界情况

    /// <summary>
    /// 测试async函数中的自定义异常
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncCustomExceptions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class CustomError {
    public message
    public code

    public func constructor(message:string, code:int) {
        this.message <- message
        this.code <- code
    }

    public func ToString() -> string {
        return ""Error "" + this.code.ToStr() + "": "" + this.message
    }
}

async func validateAndProcess(data:int) -> string {
    try {
        await validateInput(data)
        result <- await processData(data)
        return result
    } catch (CustomError error) {
        return ""Custom error: "" + error.ToString()
    } catch (error) {
        return ""Generic error: "" + error.ToStr()
    }
}

async func validateInput(data:int) -> void {
    if data < 0 {
        throw CustomError(""Negative input not allowed"", 1001)
    }
}

async func processData(data:int) -> string {
    return ""Processed: "" + data.ToStr()
}

result1 <- await validateAndProcess(10)
result2 <- await validateAndProcess(-5)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试async函数中的资源清理错误
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncResourceCleanupError_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func processWithResourceManagement() -> string {
    resource1 <- null
    resource2 <- null

    try {
        resource1 <- await acquireResource1()
        resource2 <- await acquireResource2()

        result <- await processWithResources(resource1, resource2)
        return result
    } catch (error) {
        return ""Processing failed: "" + error.ToStr()
    } finally {
        try {
            if resource1 != null {
                await releaseResource1(resource1)
            }
        } catch (cleanupError) {
            PrintLine(""Resource1 cleanup error: "" + cleanupError.ToStr())
        }

        try {
            if resource2 != null {
                await releaseResource2(resource2)
            }
        } catch (cleanupError) {
            PrintLine(""Resource2 cleanup error: "" + cleanupError.ToStr())
        }
    }
}

async func acquireResource1() -> string {
    return ""Resource1""
}

async func acquireResource2() -> string {
    return ""Resource2""
}

async func processWithResources(res1:string, res2:string) -> string {
    return res1 + "" + "" + res2
}

async func releaseResource1(resource:string) -> void {
    PrintLine(""Resource1 released"")
}

async func releaseResource2(resource:string) -> void {
    PrintLine(""Resource2 released"")
}

result <- await processWithResourceManagement()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region async 错误语法测试

    /// <summary>
    /// 测试不完整的try-catch在async函数中
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteAsyncTryCatch_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
async func testFunc() -> string {
    try {
        data <- await fetchData()
        return data
    catch  // 缺少括号和参数
    }";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试错误的throw语法在async函数中
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidThrowInAsync_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
async func testFunc() -> string {
    await someOperation()
    throw  // 缺少异常值
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}