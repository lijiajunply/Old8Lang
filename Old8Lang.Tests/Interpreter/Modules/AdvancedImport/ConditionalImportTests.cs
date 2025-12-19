using Old8Lang.AST.Expression;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.AdvancedImport;

/// <summary>
/// 条件导入功能测试
/// </summary>
public class ConditionalImportTests : ModuleImportTestBase
{
    public ConditionalImportTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Import_ConditionalImport_ShouldImportBasedOnCondition()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("ImportTests_Import_ConditionalImport.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<Old8Lang.AST.Expression.Value.StringLangValue>(result);
        Assert.Equal("logging imported", ((Old8Lang.AST.Expression.Value.StringLangValue)result).Value);
    }

    [Fact]
    public void Import_ConditionalWithTrueCondition_ShouldImportModule()
    {
        // Arrange
        var debugModule = """

                          func debugLog(message:string) -> string {
                              return "[DEBUG] " + message
                          }
                          
                          func debugmode() {
                              return true
                          }

                          """;

        var testContent = """

                          is_debug <- true
                          result <- ""
                          debug_mode <- true

                          if (is_debug) {
                              import "debug_module" as debug
                              result <- debug.debugLog("Debug message")
                              debug_mode <- debug.debugmode()
                          } else {
                              result <- "No debug module"
                              debug_mode <- false
                          }

                          """;

        CreateTempModuleFile("debug_module.old8", debugModule);
        CreateTempModuleFile("conditional_true_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("conditional_true_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "[DEBUG] Debug message");
        AssertVariableValue(interpreter, "debug_mode", true);
    }

    [Fact]
    public void Import_ConditionalWithFalseCondition_ShouldSkipModule()
    {
        // Arrange
        var debugModule = @"
func debugLog(message:string) -> string {
    return ""[DEBUG] "" + message
}
const DEBUG_MODE <- true
";

        var testContent = @"
is_debug <- false

if (is_debug) {
    import ""debug_module"" as debug
    result <- debug.debugLog(""Debug message"")
    debug_mode <- debug.DEBUG_MODE
} else {
    result <- ""Production mode""
    debug_mode <- false
}
";

        CreateTempModuleFile("debug_module.old8", debugModule);
        CreateTempModuleFile("conditional_false_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("conditional_false_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "Production mode");
        AssertVariableValue(interpreter, "debug_mode", false);
    }

    [Fact]
    public void Import_MultipleConditionalImports_ShouldHandleCorrectly()
    {
        // Arrange
        var loggingModule = @"
func logInfo(message:string) -> string {
    return ""[INFO] "" + message
}
func logError(message:string) -> string {
    return ""[ERROR] "" + message
}
";

        var metricsModule = @"
func trackEvent(eventName:string) -> string {
    return ""Event tracked: "" + eventName
}
func incrementCounter(counterName:string) -> string {
    return ""Counter incremented: "" + counterName
}
";

        var testContent = @"
enable_logging <- true
enable_metrics <- false

result_list <- {}

if (enable_logging) {
    import ""conditional_logging"" as logging
    result_list.Add(logging.logInfo(""Application started""))
}

if (enable_metrics) {
    import ""conditional_metrics"" as metrics
    result_list.Add(metrics.trackEvent(""startup""))
}

result <- result_list[0]  // 应该只有日志信息
";

        CreateTempModuleFile("conditional_logging.old8", loggingModule);
        CreateTempModuleFile("conditional_metrics.old8", metricsModule);
        CreateTempModuleFile("multiple_conditional_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("multiple_conditional_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "[INFO] Application started");
    }

    [Fact]
    public void Import_ConditionalWithComplexExpression_ShouldEvaluateExpression()
    {
        // Arrange
        var advancedModule = """

                             func advancedFeature() -> string {
                                 return "Advanced functionality"
                             }
                             
                             func version() {
                                return "2.0"
                             }

                             """;

        var testContent = """
                          user_level <- 3
                          feature_enabled <- user_level > 2
                          version_check <- "1.5"
                          result <- ""

                          // 复杂条件表达式
                          if (feature_enabled && version_check < "2.0") {
                              import "advanced_module" as adv
                              result <- "Using old version: " + adv.advancedFeature()
                          } else if (feature_enabled && version_check >= "2.0") {
                              import "advanced_module" as adv
                              result <- "Using latest version: " + adv.advancedFeature() + " v" + adv.version()
                          } else {
                              result <- "Basic features only"
                          }
                          """;

        CreateTempModuleFile("advanced_module.old8", advancedModule);
        CreateTempModuleFile("complex_conditional_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("complex_conditional_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "Using latest version: Advanced functionality v2.0");
    }

    [Fact]
    public void Import_ConditionalWithRuntimeVariable_ShouldAdaptToRuntime()
    {
        // Arrange
        var testModule = """

                         func testFeature() -> string {
                             return "Test feature active"
                         }
                         func prodFeature() -> string {
                             return "Production feature active"
                         }

                         """;

        var runtimeTestContent = """

                                 // 模拟运行时环境检查
                                 environment <- "test"
                                 result <- ""

                                 if (environment == "test") {
                                     import "runtime_module" as runtime
                                     result <- runtime.testFeature()
                                 } else if (environment == "production") {
                                     import "runtime_module" as runtime
                                     result <- runtime.prodFeature()
                                 } else {
                                     result <- "Unknown environment"
                                 }

                                 """;

        var productionTestContent = """

                                    // 模拟运行时环境检查
                                    environment <- "production"
                                    result <- ""

                                    if (environment == "test") {
                                        import "runtime_module" as runtime
                                        result <- runtime.testFeature()
                                    } else if (environment == "production") {
                                        import "runtime_module" as runtime
                                        result <- runtime.prodFeature()
                                    } else {
                                        result <- "Unknown environment"
                                    }

                                    """;

        CreateTempModuleFile("runtime_module.old8", testModule);
        CreateTempModuleFile("runtime_test_test.old8", runtimeTestContent);
        CreateTempModuleFile("runtime_prod_test.old8", productionTestContent);

        // Act - 测试环境
        var (testInterpreter, testException) = ExecuteCodeFile("runtime_test_test.old8");

        // Act - 生产环境
        var (prodInterpreter, prodException) = ExecuteCodeFile("runtime_prod_test.old8");

        // Assert
        Assert.Null(testException);
        Assert.Null(prodException);

        AssertVariableValue(testInterpreter, "result", "Test feature active");
        AssertVariableValue(prodInterpreter, "result", "Production feature active");
    }

    [Fact]
    public void Import_ConditionalNestedIf_ShouldHandleNestedConditions()
    {
        // Arrange
        const string level1Module = """
                                    func level1Feature() -> string {
                                        return "Level 1 feature"
                                    }
                                    """;

        const string level2Module = """
                                    func level2Feature() -> string {
                                        return "Level 2 feature"
                                    }
                                    """;

        const string testContent = """
                                   user_type <- "premium"
                                   user_region <- "us"
                                   result <- ""

                                   if (user_type == "premium") {
                                       if (user_region == "us") {
                                           import "level1_module" as l1
                                           result <- l1.level1Feature() + " - US Premium"
                                       } else if (user_region == "eu") {
                                           import "level2_module" as l2
                                           result <- l2.level2Feature() + " - EU Premium"
                                       } else {
                                           result <- "Premium - Other region"
                                       }
                                   } else {
                                       result <- "Basic user"
                                   }
                                   """;

        CreateTempModuleFile("level1_module.old8", level1Module);
        CreateTempModuleFile("level2_module.old8", level2Module);
        CreateTempModuleFile("nested_conditional_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("nested_conditional_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "Level 1 feature - US Premium");
    }

    [Fact]
    public void Import_ConditionalSwitchCase_ShouldWorkWithSwitch()
    {
        // Arrange
        var moduleA = @"
func featureA() -> string {
    return ""Feature A activated""
}
";

        var moduleB = @"
func featureB() -> string {
    return ""Feature B activated""
}
";

        var moduleC = @"
func featureC() -> string {
    return ""Feature C activated""
}
";

        var testContent = @"
feature_flag <- ""B""
result <- """"

switch (feature_flag) {
    case ""A"" {
        import ""switch_module_a"" as mod
        result <- mod.featureA()
    }
    case ""B"" {
        import ""switch_module_b"" as mod
        result <- mod.featureB()
    }
    case ""C"" {
        import ""switch_module_c"" as mod
        result <- mod.featureC()
    }
    default {
        result <- ""No feature activated""
    }
}
";

        CreateTempModuleFile("switch_module_a.old8", moduleA);
        CreateTempModuleFile("switch_module_b.old8", moduleB);
        CreateTempModuleFile("switch_module_c.old8", moduleC);
        CreateTempModuleFile("switch_conditional_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("switch_conditional_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "Feature B activated");
    }

    [Fact]
    public void Import_ConditionalWithTryCatch_ShouldHandleImportErrors()
    {
        // Arrange
        var workingModule = @"
func workingFeature() -> string {
    return ""Working feature""
}
";

        var testContent = @"
use_experimental <- false
result <- """"

try {
    if (use_experimental) {
        import ""non_experimental_module"" as experimental  // 这个模块不存在
        result <- experimental.experimentalFeature()
    } else {
        import ""working_module"" as working
        result <- working.workingFeature()
    }
} catch {
    result <- ""Import failed, using fallback""
}
";

        CreateTempModuleFile("working_module.old8", workingModule);
        CreateTempModuleFile("try_catch_conditional_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("try_catch_conditional_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "Working feature");
    }

    [Fact]
    public void Import_ConditionalLazyImport_ShouldCombineFeatures()
    {
        // Arrange
        var expensiveModule = @"
func expensiveOperation() -> string {
    // 模拟耗时操作
    return ""Expensive operation completed""
}
func getDataSize() -> int {
    HEAVY_DATA:const <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
    return HEAVY_DATA.Size()
}
";

        var testContent = @"
enable_expensive <- false
result <- """"
data_size <- 0

if (enable_expensive) {
    import ""expensive_module"" as exp
    result <- exp.expensiveOperation()
    data_size <- exp.getDataSize()
} else {
    result <- ""Expensive features disabled""
    data_size <- 0
}
";

        CreateTempModuleFile("expensive_module.old8", expensiveModule);
        CreateTempModuleFile("lazy_conditional_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_conditional_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "Expensive features disabled");
        AssertVariableValue(interpreter, "data_size", 0);
    }

    [Fact]
    public void Import_ConditionalLazyImport_WhenTrue_ShouldImportModule()
    {
        // Arrange
        var expensiveModule = @"
func expensiveOperation() -> string {
    return ""Expensive operation completed""
}
func getSimpleValue() -> int {
    return 42
}
";

        var testContent = @"
enable_expensive <- true
result <- """"
simple_value <- 0
if (enable_expensive) {
    import ""expensive_module"" as exp
    result <- exp.expensiveOperation()
    simple_value <- exp.getSimpleValue()
} else {
    result <- ""Expensive features disabled""
    simple_value <- 0
}
";

        CreateTempModuleFile("expensive_module.old8", expensiveModule);
        CreateTempModuleFile("lazy_conditional_true_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_conditional_true_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "Expensive operation completed");
        AssertVariableValue(interpreter, "simple_value", 42);
    }
}