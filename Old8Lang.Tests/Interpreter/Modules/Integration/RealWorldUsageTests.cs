using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Integration;

/// <summary>
/// 真实使用场景集成测试
/// </summary>
public class RealWorldUsageTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_WebApplicationScenario_ShouldWorkCorrectly()
    {
        // Arrange - 模拟一个Web应用的模块结构
        var databaseModule = @"
func connect(connectionString:string) -> string {
    return ""Connected to: "" + connectionString
}
func query(sql:string) -> string {
    return ""Query result for: "" + sql
}
func close() -> string {
    return ""Connection closed""
}
";

        var authModule = @"
func authenticate(username:string, password:string) -> bool {
    return username == ""admin"" && password == ""secret""
}
func hasPermission(user:string, permission:string) -> bool {
    return user == ""admin""
}
";

        var utilsModule = @"
func formatDate(date:string) -> string {
    return ""2024-01-01""
}
func generateToken() -> string {
    return ""jwt_token_12345""
}
";

        var appContent = @"
import ""database"" as db
import ""auth"" as auth
import ""utils"" as utils

// 模拟Web应用登录流程
func handleLogin(username:string, password:string) -> string {
    if auth.authenticate(username, password) {
        connection <- db.connect(""mysql://localhost/myapp"")
        token <- utils.generateToken()
        db.close()
        return ""Login successful. Token: "" + token
    } else {
        return ""Login failed""
    }
}

result <- handleLogin(""admin"", ""secret"")
";

        CreateTempModuleFile("database.old8", databaseModule);
        CreateTempModuleFile("auth.old8", authModule);
        CreateTempModuleFile("utils.old8", utilsModule);
        CreateTempModuleFile("webapp.old8", appContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("webapp.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        var message = ((StringLangValue)result).Value;
        Assert.Contains("Login successful", message);
        Assert.Contains("jwt_token_12345", message);
    }

    [Fact]
    public void Import_DataProcessingPipeline_ShouldWorkCorrectly()
    {
        // Arrange - 模拟数据处理管道
        var dataInputModule = @"
func readCSV(filePath:string) -> list {
    // 模拟读取CSV数据
    return {{1, 2, 3, 4, 5}}
}
func readJSON(filePath:string) -> list {
    // 模拟读取JSON数据
    return {{10, 20, 30}}
}
";

        var dataTransformModule = @"
func filter(data:list, predicate:int) -> list {
    result <- {}
    i <- 0
    while i < data.Size() {
        item <- data[i]
        if item > predicate {
            result.Add(item)
        }
        i <- i + 1
    }
    return result
}
func map(data:list, multiplier:int) -> list {
    result <- {}
    i <- 0
    while i < data.Size() {
        item <- data[i]
        result.Add(item * multiplier)
        i <- i + 1
    }
    return result
}
";

        var dataOutputModule = @"
func writeCSV(data:list, filePath:string) -> string {
    return ""Wrote "" + data.Size().ToStr() + "" items to "" + filePath
}
func calculateStats(data:list) -> dict {
    sum <- 0
    i <- 0
    while i < data.Size() {
        sum <- sum + data[i]
        i <- i + 1
    }
    avg <- sum / data.Size()
    return {""sum"": sum, ""avg"": avg, ""count"": data.Size()}
}
";

        var pipelineContent = @"
import ""data_input"" as input
import ""data_transform"" as transform
import ""data_output"" as output

// 数据处理管道
func processDataPipeline(inputFile:string) -> dict {
    // 1. 读取数据
    rawData <- input.readCSV(inputFile)

    // 2. 转换数据：过滤大于2的值，然后乘以3
    filteredData <- transform.filter(rawData, 2)
    transformedData <- transform.map(filteredData, 3)

    // 3. 输出统计信息
    stats <- output.calculateStats(transformedData)

    // 4. 写入文件（模拟）
    output.writeCSV(transformedData, ""output.csv"")

    return stats
}

result <- processDataPipeline(""data.csv"")
";

        CreateTempModuleFile("data_input.old8", dataInputModule);
        CreateTempModuleFile("data_transform.old8", dataTransformModule);
        CreateTempModuleFile("data_output.old8", dataOutputModule);
        CreateTempModuleFile("pipeline.old8", pipelineContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("pipeline.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 预期：过滤后的数据为 [3, 4, 5]，映射后为 [9, 12, 15]
        // 统计：sum=36, avg=12, count=3
    }

    [Fact]
    public void Import_GameDevelopmentScenario_ShouldWorkCorrectly()
    {
        // Arrange - 模拟游戏开发的模块系统
        var mathModule = @"
func vec2(x:double, y:double) -> dict {
    return {""x"": x, ""y"": y}
}
func distance(a:dict, b:dict) -> double {
    dx <- b[""x""] - a[""x""]
    dy <- b[""y""] - a[""y""]
    return sqrt(dx * dx + dy * dy)
}
func normalize(vec:dict) -> dict {
    length <- sqrt(vec[""x""] * vec[""x""] + vec[""y""] * vec[""y""])
    return {""x"": vec[""x""] / length, ""y"": vec[""y""] / length}
}
";

        var graphicsModule = @"
func createSprite(texturePath:string) -> dict {
    return {""texture"": texturePath, ""x"": 0, ""y"": 0}
}
func setPosition(sprite:dict, position:dict) -> dict {
    sprite[""x""] <- position[""x""]
    sprite[""y""] <- position[""y""]
    return sprite
}
func render(sprite:dict) -> string {
    return ""Rendering sprite at ("" + sprite[""x""].ToStr() + "", "" + sprite[""y""].ToStr() + "")""
}
";

        var gameModule = @"
import ""math2"" as math
import ""graphics"" as gfx

player <- gfx.createSprite(""player.png"")
enemy <- gfx.createSprite(""enemy.png"")

func updateGame() -> string {
    playerPos <- math.vec2(10.0, 20.0)
    enemyPos <- math.vec2(30.0, 40.0)

    player <- gfx.setPosition(player, playerPos)
    enemy <- gfx.setPosition(enemy, enemyPos)

    dist <- math.distance(playerPos, enemyPos)

    if dist < 15.0 {
        return ""Enemy is near! Distance: "" + dist.ToStr()
    } else {
        return ""Enemy is far away. Distance: "" + dist.ToStr()
    }
}

result <- updateGame()
";

        CreateTempModuleFile("math2.old8", mathModule);
        CreateTempModuleFile("graphics.old8", graphicsModule);
        CreateTempModuleFile("game.old8", gameModule);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("game.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        var message = ((StringLangValue)result).Value;
        Assert.Contains("Distance:", message);
        // 计算距离：sqrt((30-10)^2 + (40-20)^2) = sqrt(400 + 400) = sqrt(800) ≈ 28.28
        Assert.Contains("far away", message);
    }

    [Fact]
    public void Import_ConfigurationManagement_ShouldWorkCorrectly()
    {
        // Arrange - 模拟配置管理系统
        var configModule = @"
func loadConfig(filePath:string) -> dict {
    if filePath == ""production.json"" {
        return {
            ""database"": ""prod_server"",
            ""port"": 5432,
            ""debug"": false,
            ""maxConnections"": 100
        }
    } else if filePath == ""development.json"" {
        return {
            ""database"": ""dev_server"",
            ""port"": 5433,
            ""debug"": true,
            ""maxConnections"": 10
        }
    } else {
        return {}
    }
}
func validateConfig(config:dict) -> bool {
    hasDb <- config.ContainsKey(""database"")
    hasPort <- config.ContainsKey(""port"")
    return hasDb && hasPort
}
";

        var loggerModule = @"
func createLogger(config:dict) -> string {
    if config[""debug""] {
        return ""DEBUG logger created""
    } else {
        return ""INFO logger created""
    }
}
func log(message:string, level:string) -> string {
    return ""["" + level + ""] "" + message
}
";

        var appContent = @"
import ""config"" as config
import ""logger"" as logger

func initializeApp(environment:string) -> dict {
    configFile <- environment + "".json""
    appConfig <- config.loadConfig(configFile)

    if !config.validateConfig(appConfig) {
        return {""status"": ""error"", ""message"": ""Invalid config""}
    }

    loggerType <- logger.createLogger(appConfig)
    logger.log(""Application initialized with "" + environment, ""INFO"")

    return {
        ""status"": ""success"",
        ""config"": appConfig,
        ""logger"": loggerType
    }
}

// 测试不同环境
devResult <- initializeApp(""development"")
prodResult <- initializeApp(""production"")
";

        CreateTempModuleFile("config.old8", configModule);
        CreateTempModuleFile("logger.old8", loggerModule);
        CreateTempModuleFile("config_app.old8", appContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("config_app.old8");

        // Assert
        Assert.Null(exception);
        var devResult = interpreter.Manager.GetValue(new LangId("devResult"));
        var prodResult = interpreter.Manager.GetValue(new LangId("prodResult"));
        Assert.NotNull(devResult);
        Assert.NotNull(prodResult);
    }
}