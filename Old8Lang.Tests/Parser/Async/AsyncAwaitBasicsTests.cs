using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Async;

/// <summary>
/// 异步编程基础语法测试
/// </summary>
[Collection("Sequential")]
public class AsyncAwaitBasicsTests
{
    #region async/await 基础语法

    /// <summary>
    /// 测试基本async函数定义
    /// </summary>
    [Fact]
    public void ParseProgram_BasicAsyncFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func fetchData() -> string {
    return ""Data loaded""
}

result <- fetchData()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试带参数的async函数
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncFunctionWithParameters_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func processData(data:string, factor:int) -> int {
    processed <- data.Length() * factor
    return processed
}

result <- processData(""hello"", 2)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试await表达式
    /// </summary>
    [Fact]
    public void ParseProgram_BasicAwaitExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func main() {
    data <- await fetchData()
    processed <- await processData(data, 2)
    PrintLine(processed)
}

async func fetchData() -> string {
    return ""Sample data""
}

async func processData(input:string, multiplier:int) -> int {
    return input.Length() * multiplier

main()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试多个await表达式
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleAwaitExpressions_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func loadUserData(userId:int) -> dict {
    userInfo <- await getUserInfo(userId)
    userPosts <- await getUserPosts(userId)
    userComments <- await getUserComments(userId)

    return {
        ""info"": userInfo,
        ""posts"": userPosts,
        ""comments"": userComments
    }
}

async func main() {
    userId <- 123
    userData <- await loadUserData(userId)
    PrintLine(""Loaded data for user: "" + userId.ToStr())
}

main()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套async/await
    /// </summary>
    [Fact]
    public void ParseProgram_NestedAsyncAwait_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func outerFunction() -> string {
    result1 <- await innerFunction1()
    result2 <- await innerFunction2(result1)
    return result2
}

async func innerFunction1() -> string {
    return ""First""
}

async func innerFunction2(input:string) -> string {
    return input + "" Second""
}

finalResult <- await outerFunction()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region async/await 返回类型

    /// <summary>
    /// 测试async函数返回不同类型
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncFunctionReturnTypes_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func getStringData() -> string {
    return ""Hello""
}

async func getIntData() -> int {
    return 42
}

async func getBoolData() -> bool {
    return true
}

async func getListData() -> list {
    return {1, 2, 3}
}

async func getDictData() -> dict {
    return {""key"": ""value""}
}

result1 <- await getStringData()
result2 <- await getIntData()
result3 <- await getBoolData()
result4 <- await getListData()
result5 <- await getDictData()";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试async函数无返回值
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncVoidFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func logMessage(message:string) -> void {
    await writeLog(message)
    await notifySystem()
}

async func writeLog(message:string) -> void {
    PrintLine(""Log: "" + message)
}

async func notifySystem() -> void {
    PrintLine(""System notified"")
}

await logMessage(""Test message"")";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region async/await 在类中

    /// <summary>
    /// 测试类中的async方法
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncClassMethods_ParsesSuccessfully()
    {
        // Arrange
        var code = """

                   class DataLoader {
                       private url

                       public func constructor(url:string) {
                           this.url <- url
                       }

                       public async func load() -> string {
                           response <- await fetchFromServer(this.url)
                           return response
                       }

                       private async func fetchFromServer(url:string) -> string {
                           return "Data from " + url
                       }

                       public async func processAndLoad() -> string {
                           raw <- await this.load()
                           processed <- "Processed: " + raw
                           return processed
                       }
                   }

                   loader <- DataLoader("http://example.com")
                   data <- await loader.processAndLoad()
                   PrintLine(data)
                   """;
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试静态async方法
    /// </summary>
    [Fact]
    public void ParseProgram_StaticAsyncMethods_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class NetworkUtils {
    public static async func download(url:string) -> string {
        response <- await httpRequest(url)
        return response
    }

    public static async func upload(url:string, data:string) -> bool {
        success <- await sendRequest(url, data)
        return success
    }

    private static async func httpRequest(url:string) -> string {
        return ""Response from "" + url
    }

    private static async func sendRequest(url:string, data:string) -> bool {
        return true
    }
}

downloaded <- await NetworkUtils.download(""http://example.com"")
uploaded <- await NetworkUtils.upload(""http://api.example.com"", ""data"")";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region async/await 错误语法

    /// <summary>
    /// 测试不完整的async函数定义
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteAsyncFunction_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
async func testFunc ";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试await在非async函数中使用
    /// </summary>
    [Fact]
    public void ParseProgram_AwaitOutsideAsyncFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func regularFunction() {
    // 语法上可能正确，但语义上await只能在async函数中使用
    result <- await someAsyncCall()
    return result
}

async func someAsyncCall() -> string {
    return ""result""
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        // 语法解析应该成功，语义检查可能报错
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试async和返回类型的位置错误
    /// </summary>
    [Fact]
    public void ParseProgram_WrongAsyncReturnTypePosition_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
func async testFunc() -> string {  // async应该在func之前
    return ""test""
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试await后缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_AwaitMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
async func testFunc() -> string {
    result <- await
    return result
}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region async/await 边界情况

    /// <summary>
    /// 测试async函数中的循环
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncFunctionWithLoop_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func loadMultipleData(urls:list) -> list {
    results <- {}
    for url in urls {
        data <- await fetchData(url)
        results.Push(data)
    }
    return results
}

async func fetchData(url:string) -> string {
    return ""Data from "" + url
}

urls <- {""url1"", ""url2"", ""url3""}
allData <- await loadMultipleData(urls)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试async函数中的条件语句
    /// </summary>
    [Fact]
    public void ParseProgram_AsyncFunctionWithCondition_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
async func conditionalLoad(dataType:string) -> string {
    if dataType == ""user"" {
        return await loadUserData()
    } else if dataType == ""post"" {
        return await loadPostData()
    } else {
        return await loadDefaultData()
    }
}

async func loadUserData() -> string {
    return ""User data""
}

async func loadPostData() -> string {
    return ""Post data""
}

async func loadDefaultData() -> string {
    return ""Default data""
}

result1 <- await conditionalLoad(""user"")
result2 <- await conditionalLoad(""post"")
result3 <- await conditionalLoad(""other"")";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}