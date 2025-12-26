using Old8Lang.AST.Expression;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Integration;

/// <summary>
/// 复杂导入场景测试
/// </summary>
public class ComplexImportScenariosTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_ImportChain_ShouldHandleImportChains()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("ImportTests_Import_ImportChain.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<AST.Expression.Value.StringLangValue>(result);
        Assert.Equal("Combined data from all submodules", ((AST.Expression.Value.StringLangValue)result).Value);
    }

    [Fact]
    public void Import_MultiLevelDependencyTree_ShouldResolveCorrectly()
    {
        // Arrange - 创建多级依赖树
        var leaf1Content = @"
func leaf1Function() -> string { return ""Leaf1"" }
const LEAF1_CONST <- 100
";

        var leaf2Content = @"
func leaf2Function() -> string { return ""Leaf2"" }
const LEAF2_CONST <- 200
";

        var leaf3Content = @"
func leaf3Function() -> string { return ""Leaf3"" }
const LEAF3_CONST <- 300
";

        var middle1Content = @"
import ""tree_leaf1""
import ""tree_leaf2""
func middle1Function() -> string {
    return ""Middle1: "" + leaf1Function() + "" + "" + leaf2Function()
}
func getMiddle1Sum() -> int {
    return LEAF1_CONST + LEAF2_CONST
}
";

        var middle2Content = @"
import ""tree_leaf3""
func middle2Function() -> string {
    return ""Middle2: "" + leaf3Function()
}
func getMiddle2Value() -> int {
    return LEAF3_CONST
}
";

        var rootContent = @"
import ""tree_middle1""
import ""tree_middle2""
func rootFunction() -> string {
    return ""Root: "" + middle1Function() + "" + "" + middle2Function()
}
func getTotalSum() -> int {
    return getMiddle1Sum() + getMiddle2Value()
}
";

        var testContent = @"
import ""tree_root"" as root
result1 <- root.rootFunction()
result2 <- root.getTotalSum()
";

        CreateTempModuleFile("tree_leaf1.old8", leaf1Content);
        CreateTempModuleFile("tree_leaf2.old8", leaf2Content);
        CreateTempModuleFile("tree_leaf3.old8", leaf3Content);
        CreateTempModuleFile("tree_middle1.old8", middle1Content);
        CreateTempModuleFile("tree_middle2.old8", middle2Content);
        CreateTempModuleFile("tree_root.old8", rootContent);
        CreateTempModuleFile("dependency_tree_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("dependency_tree_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Root: Middle1: Leaf1 Leaf2 + Middle2: Leaf3");
        AssertVariableValue(interpreter, "result2", 600);
    }

    [Fact]
    public void Import_DiamondDependencyPattern_ShouldHandleCorrectly()
    {
        // Arrange - 创建钻石依赖模式 A -> B,C -> D
        var moduleDContent = @"
const SHARED_VALUE <- 1000
func getSharedValue() -> int { return SHARED_VALUE }
func incrementShared() -> int { return SHARED_VALUE + 1 }
";

        var moduleBContent = @"
import ""diamond_d""
func getBValue() -> int {
    return getSharedValue() + 100
}
";

        var moduleCContent = @"
import ""diamond_d""
func getCValue() -> int {
    return getSharedValue() + 200
}
";

        var moduleAContent = @"
import ""diamond_b"" as b
import ""diamond_c"" as c
func getCombinedValue() -> int {
    return b.getBValue() + c.getCValue()
}
func getSharedFromBoth() -> int {
    // 确保共享模块只加载一次
    return getSharedValue()  // 这里应该能直接访问 diamond_d 的函数
}
";

        var testContent = @"
import ""diamond_a"" as a
result1 <- a.getCombinedValue()
result2 <- a.getSharedFromBoth()
";

        CreateTempModuleFile("diamond_d.old8", moduleDContent);
        CreateTempModuleFile("diamond_b.old8", moduleBContent);
        CreateTempModuleFile("diamond_c.old8", moduleCContent);
        CreateTempModuleFile("diamond_a.old8", moduleAContent);
        CreateTempModuleFile("diamond_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("diamond_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 1400); // (1000+100) + (1000+200)
        AssertVariableValue(interpreter, "result2", 1000);
    }

    [Fact]
    public void Import_ConditionalImportNetwork_ShouldEvaluateConditions()
    {
        // Arrange - 创建条件导入网络
        var baseModuleContent = @"
func baseFunction() -> string { return ""Base"" }
BASE_CONST:const <- 10
";

        var enhancedModuleContent = @"
import ""conditional_base""
func enhancedFunction() -> string { return ""Enhanced: "" + baseFunction() }
ENHANCED_CONST:const <- 100
";

        var testContent = @"
// 根据条件选择不同的模块
is_debug_mode <- true

if (is_debug_mode) {
    import ""conditional_enhanced"" as mod
} else {
    import ""conditional_base"" as mod
}

result_function <- mod.baseFunction()
const_value <- if is_debug_mode then { mod.ENHANCED_CONST } else { mod.BASE_CONST }
";

        CreateTempModuleFile("conditional_base.old8", baseModuleContent);
        CreateTempModuleFile("conditional_enhanced.old8", enhancedModuleContent);
        CreateTempModuleFile("conditional_network_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("conditional_network_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result_function", "Base");
        AssertVariableValue(interpreter, "const_value", 100);
    }

    [Fact]
    public void Import_PluginArchitecturePattern_ShouldWork()
    {
        // Arrange - 模拟插件架构
        var pluginInterfaceContent = @"
// 插件接口定义
func initialize() -> string { return ""Plugin initialized"" }
func execute(input:string) -> string { return ""Processed: "" + input }
func cleanup() -> string { return ""Plugin cleaned up"" }
";

        var pluginAContent = @"
// 插件A实现
import ""plugin_interface""
func initialize() -> string { return ""Plugin A initialized"" }
func execute(input:string) -> string { return ""Plugin A processed: "" + input }
func cleanup() -> string { return ""Plugin A cleaned up"" }
";

        var pluginBContent = @"
// 插件B实现
import ""plugin_interface""
func initialize() -> string { return ""Plugin B initialized"" }
func execute(input:string) -> string { return ""Plugin B processed: "" + input.ToUpper() }
func cleanup() -> string { return ""Plugin B cleaned up"" }
";

        var pluginLoaderContent = @"
func loadPlugin(pluginName:string) -> dict {
    if (pluginName == ""A"") {
        return {
            ""name"": ""Plugin A"",
            ""initialize"": () -> { import ""plugin_a"" as p; return p.initialize(); },
            ""execute"": (input:string) -> { import ""plugin_a"" as p; return p.execute(input); }
        }
    } else if (pluginName == ""B"") {
        return {
            ""name"": ""Plugin B"",
            ""initialize"": () -> { import ""plugin_b"" as p; return p.initialize(); },
            ""execute"": (input:string) -> { import ""plugin_b"" as p; return p.execute(input); }
        }
    } else {
        return {}
    }
}
";

        var testContent = @"
import ""plugin_loader"" as loader
pluginA <- loader.loadPlugin(""A"")
pluginB <- loader.loadPlugin(""B"")

result1 <- pluginA[""execute""](""test data"")
result2 <- pluginB[""execute""](""test data"")
";

        CreateTempModuleFile("plugin_interface.old8", pluginInterfaceContent);
        CreateTempModuleFile("plugin_a.old8", pluginAContent);
        CreateTempModuleFile("plugin_b.old8", pluginBContent);
        CreateTempModuleFile("plugin_loader.old8", pluginLoaderContent);
        CreateTempModuleFile("plugin_architecture_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("plugin_architecture_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Plugin A processed: test data");
        AssertVariableValue(interpreter, "result2", "Plugin B processed: TEST DATA");
    }

    [Fact]
    public void Import_MicroservicePattern_ShouldSimulateServiceCommunication()
    {
        // Arrange - 模拟微服务架构
        var authServiceContent = @"
func authenticate(token:string) -> bool {
    return token == ""valid_token""
}
func getUserInfo(userId:int) -> dict {
    return {""id"": userId, ""name"": ""User "" + userId.ToStr()}
}
";

        var dataServiceContent = @"
func getData(userId:int) -> list {
    return {{""item1""}, {""item2""}, {""item3""}}
}
func saveData(userId:int, data:list) -> bool {
    return true
}
";

        var notificationServiceContent = @"
func sendNotification(userId:int, message:string) -> bool {
    return true
}
func getNotifications(userId:int) -> list {
    return {{""Notification 1""}, {""Notification 2""}}
}
";

        var apiGatewayContent = @"
import ""microservice_auth"" as auth
import ""microservice_data"" as data
import ""microservice_notification"" as notification

func getUserDashboard(token:string, userId:int) -> dict {
    if (!auth.authenticate(token)) {
        return {""error"": ""Invalid token""}
    }

    userInfo <- auth.getUserInfo(userId)
    user_data <- data.getData(userId)
    notifications <- notification.getNotifications(userId)

    return {
        ""user"": userInfo,
        ""data"": user_data,
        ""notifications"": notifications
    }
}
";

        var testContent = @"
import ""microservice_api_gateway"" as gateway
result <- gateway.getUserDashboard(""valid_token"", 123)
";

        CreateTempModuleFile("microservice_auth.old8", authServiceContent);
        CreateTempModuleFile("microservice_data.old8", dataServiceContent);
        CreateTempModuleFile("microservice_notification.old8", notificationServiceContent);
        CreateTempModuleFile("microservice_api_gateway.old8", apiGatewayContent);
        CreateTempModuleFile("microservice_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("microservice_test.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    [Fact]
    public void Import_ModuleFactoryPattern_ShouldCreateDynamicModules()
    {
        // Arrange - 模块工厂模式
        var factoryContent = @"
func createCalculator(type:string) -> dict {
    if (type == ""basic"") {
        return {
            ""add"": (a:int, b:int) -> { return a + b },
            ""subtract"": (a:int, b:int) -> { return a - b },
            ""multiply"": (a:int, b:int) -> { return a * b }
        }
    } else if (type == ""scientific"") {
        return {
            ""add"": (a:double, b:double) -> { return a + b },
            ""subtract"": (a:double, b:double) -> { return a - b },
            ""multiply"": (a:double, b:double) -> { return a * b },
            ""sqrt"": (x:double) -> { return x ^ 0.5 },
            ""power"": (base:double, exp:double) -> { return base ^ exp }
        }
    } else {
        return {}
    }
}
";

        var testContent = @"
import ""module_factory"" as factory

basic_calc <- factory.createCalculator(""basic"")
scientific_calc <- factory.createCalculator(""scientific"")

result1 <- basic_calc[""add""](5, 3)
result2 <- basic_calc[""multiply""](4, 6)
result3 <- scientific_calc[""sqrt""](16)
result4 <- scientific_calc[""power""](2, 3)
";

        CreateTempModuleFile("module_factory.old8", factoryContent);
        CreateTempModuleFile("factory_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("factory_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 8);
        AssertVariableValue(interpreter, "result2", 24);
        AssertVariableValue(interpreter, "result3", 4.0);
        AssertVariableValue(interpreter, "result4", 8.0);
    }

    [Fact]
    public void Import_EagerVsLazyMixing_ShouldWorkCorrectly()
    {
        // Arrange - 混合使用即时导入和延迟导入
        var eagerModuleContent = @"
EAGER_VALUE:const <- 100
func eagerFunction() -> string { return ""Eager"" }
";

        var lazyModuleContent = @"
LAZY_VALUE:const <- 200
func lazyFunction() -> string { return ""Lazy"" }
";

        var testContent = @"
// 即时导入
import ""eager_module"" as eager

// 延迟导入
lazy import ""lazy_module"" as laz

// 访问即时导入的模块
eager_result <- eager.eagerFunction()
eager_const <- eager.EAGER_VALUE

// 此时延迟模块还未加载
status_before_lazy <- ""Lazy not loaded yet""

// 访问延迟导入的模块
lazy_result <- laz.lazyFunction()
lazy_const <- laz.LAZY_VALUE
status_after_lazy <- ""Lazy loaded now""
";

        CreateTempModuleFile("eager_module.old8", eagerModuleContent);
        CreateTempModuleFile("lazy_module.old8", lazyModuleContent);
        CreateTempModuleFile("eager_lazy_mix_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("eager_lazy_mix_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "eager_result", "Eager");
        AssertVariableValue(interpreter, "eager_const", 100);
        AssertVariableValue(interpreter, "status_before_lazy", "Lazy not loaded yet");
        AssertVariableValue(interpreter, "lazy_result", "Lazy");
        AssertVariableValue(interpreter, "lazy_const", 200);
        AssertVariableValue(interpreter, "status_after_lazy", "Lazy loaded now");
    }
}