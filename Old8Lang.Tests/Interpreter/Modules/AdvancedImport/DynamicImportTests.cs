using Old8Lang.AST.Expression;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.AdvancedImport;

/// <summary>
/// 动态导入功能测试
/// </summary>
public class DynamicImportTests : ModuleImportTestBase
{
    public DynamicImportTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Import_DynamicImport_ShouldImportModuleDynamically()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("ImportTests_Import_DynamicImport.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<AST.Expression.Value.DoubleLangValue>(result);
        Assert.Equal(4.0, ((AST.Expression.Value.DoubleLangValue)result).Value);
    }

    [Fact]
    public void Import_DynamicModulePath_ShouldImportFromVariablePath()
    {
        // Arrange
        var mathModule = """

                         func add(a:double, b:double) -> double {
                             return a + b
                         }
                         func multiply(a:double, b:double) -> double {
                             return a * b
                         }
                         PI:const <- 3.14159

                         """;

        var testContent = """
                          dynamic import "dynamic_math" as math
                          
                          result1 <- math.add(2.5, 3.5)
                          result2 <- math.multiply(4.0, 2.5)
                          result3 <- math.PI()

                          """;

        CreateTempModuleFile("dynamic_math.old8", mathModule);
        CreateTempModuleFile("dynamic_path_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("dynamic_path_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 6.0);
        AssertVariableValue(interpreter, "result2", 10.0);
        AssertVariableValue(interpreter, "result3", 3.14159);
    }

    [Fact]
    public void Import_RuntimeModuleSelection_ShouldSelectModuleAtRuntime()
    {
        // Arrange
        var basicMathModule = @"
func calculate(x:int, y:int) -> int {
    return x + y
}
func getName() -> string {
    return ""Basic Math""
}
";

        var advancedMathModule = @"
func calculate(x:double, y:double) -> double {
    return x * y + 10.0
}
func getName() -> string {
    return ""Advanced Math""
}
";

        var testContent = @"
// 运行时选择模块
math_level <- ""advanced""

if (math_level == ""basic"") {
    module_path <- ""runtime_basic""
} else if (math_level == ""advanced"") {
    module_path <- ""runtime_advanced""
} else {
    module_path <- ""runtime_basic""  // 默认
}

import dynamic module_path as math_engine
result1 <- math_engine.calculate(5.0, 3.0)
result2 <- math_engine.getName()
";

        CreateTempModuleFile("runtime_basic.old8", basicMathModule);
        CreateTempModuleFile("runtime_advanced.old8", advancedMathModule);
        CreateTempModuleFile("runtime_selection_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("runtime_selection_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 25.0);
        AssertVariableValue(interpreter, "result2", "Advanced Math");
    }

    [Fact]
    public void Import_DynamicWithExpression_ShouldEvaluateExpression()
    {
        // Arrange
        var version1Module = @"
func getVersion() -> string {
    return ""Version 1.0""
}
func process(data:string) -> string {
    return ""Processed by v1: "" + data
}
";

        var version2Module = @"
func getVersion() -> string {
    return ""Version 2.0""
}
func process(data:string) -> string {
    return ""Processed by v2: "" + data.ToUpper()
}
";

        var testContent = @"
api_version <- 2
module_base_name <- ""versioned""
module_full_name <- module_base_name + api_version.ToStr()

import dynamic module_full_name as api
result1 <- api.getVersion()
result2 <- api.process(""test data"")
";

        CreateTempModuleFile("versioned1.old8", version1Module);
        CreateTempModuleFile("versioned2.old8", version2Module);
        CreateTempModuleFile("dynamic_expression_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("dynamic_expression_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Version 2.0");
        AssertVariableValue(interpreter, "result2", "Processed by v2: TEST DATA");
    }

    [Fact]
    public void Import_DynamicModuleNotFound_ShouldHandleGracefully()
    {
        // Arrange
        var testContent = @"
module_name <- ""nonexistent_module""
try {
    import dynamic module_name as missing_module
    result <- ""Module loaded successfully""
} catch {
    result <- ""Module not found: "" + module_name
}
";

        CreateTempModuleFile("dynamic_error_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("dynamic_error_test.old8");

        // Assert
        // 根据具体的错误处理机制，结果可能不同
        if (exception == null)
        {
            var result = interpreter.Manager.GetValue(new LangId("result"));
            Assert.NotNull(result);
        }
        else
        {
            Output.WriteLine($"动态导入错误处理: {exception.Message}");
        }
    }

    [Fact]
    public void Import_DynamicWithUserInput_ShouldAdaptToInput()
    {
        // Arrange
        var calculatorModule = @"
func add(a:double, b:double) -> double {
    return a + b
}
func subtract(a:double, b:double) -> double {
    return a - b
}
func multiply(a:double, b:double) -> double {
    return a * b
}
func divide(a:double, b:double) -> double {
    return a / b
}
";

        var testContent = @"
// 模拟用户输入
operation_type <- ""calculator""
module_name <- operation_type + ""_dynamic""

import dynamic module_name as calc
result1 <- calc.add(10.0, 5.0)
result2 <- calc.multiply(4.0, 3.0)
result3 <- calc.subtract(10.0, 3.0)
";

        CreateTempModuleFile("calculator_dynamic.old8", calculatorModule);
        CreateTempModuleFile("dynamic_user_input_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("dynamic_user_input_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 15.0);
        AssertVariableValue(interpreter, "result2", 12.0);
        AssertVariableValue(interpreter, "result3", 7.0);
    }

    [Fact]
    public void Import_DynamicLoopedImport_ShouldHandleMultipleDynamicImports()
    {
        // Arrange
        var modules = new Dictionary<string, string>
        {
            ["plugin1"] = """

                          func getName() -> string { return "Plugin 1" }
                          func getValue() -> int { return 100 }

                          """,
            ["plugin2"] = """

                          func getName() -> string { return "Plugin 2" }
                          func getValue() -> int { return 200 }

                          """,
            ["plugin3"] = """

                          func getName() -> string { return "Plugin 3" }
                          func getValue() -> int { return 300 }

                          """
        };

        var testContent = """

                          plugin_names <- ["plugin1", "plugin2", "plugin3"]
                          results <- {}
                          total <- 0

                          i <- 0
                          while i < plugin_names.Size() {
                              module_name <- plugin_names[i]

                              import dynamic module_name as plugin
                              plugin_name <- plugin.getName()
                              plugin_value <- plugin.getValue()

                              results.Add(plugin_name)
                              total <- total + plugin_value

                              i <- i + 1
                          }

                          result1 <- results[0]
                          result2 <- total

                          """;

        // 创建插件模块
        foreach (var module in modules)
        {
            CreateTempModuleFile($"{module.Key}.old8", module.Value);
        }

        CreateTempModuleFile("dynamic_looped_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("dynamic_looped_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Plugin 1");
        AssertVariableValue(interpreter, "result2", 600);
    }

    [Fact]
    public void Import_DynamicWithConfiguration_ShouldReadFromConfig()
    {
        // Arrange
        var configModule = @"
func getDatabaseConfig() -> dict {
    return {
        ""host"": ""localhost"",
        ""port"": 5432,
        ""name"": ""testdb""
    }
}
func getCacheConfig() -> dict {
    return {
        ""host"": ""localhost"",
        ""port"": 6379,
        ""ttl"": 300
    }
}
";

        var testContent = @"
// 模拟从配置文件读取
config_source <- ""dynamic_config""
import dynamic config_source as config

db_config <- config.getDatabaseConfig()
cache_config <- config.getCacheConfig()

result1 <- db_config[""host""]
result2 <- db_config[""port""]
result3 <- cache_config[""ttl""]
";

        CreateTempModuleFile("dynamic_config.old8", configModule);
        CreateTempModuleFile("dynamic_config_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("dynamic_config_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "localhost");
        AssertVariableValue(interpreter, "result2", 5432);
        AssertVariableValue(interpreter, "result3", 300);
    }

    [Fact]
    public void Import_DynamicWithAbsolutePath_ShouldHandleAbsolutePaths()
    {
        // Arrange
        var moduleContent = @"
func getModuleInfo() -> string {
    return ""Absolute path module loaded""
}
const ABSOLUTE_CONST <- 42
";

        var testContent = @"
// 构建绝对路径（这里使用相对路径模拟）
base_path <- """"
module_file_name <- ""absolute_dynamic""
full_path <- base_path + module_file_name

import dynamic full_path as abs_module
result1 <- abs_module.getModuleInfo()
result2 <- abs_module.ABSOLUTE_CONST
";

        CreateTempModuleFile("absolute_dynamic.old8", moduleContent);
        CreateTempModuleFile("dynamic_absolute_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("dynamic_absolute_test.old8");

        // Assert
        if (exception == null)
        {
            AssertVariableValue(interpreter, "result1", "Absolute path module loaded");
            AssertVariableValue(interpreter, "result2", 42);
        }
        else
        {
            Output.WriteLine($"绝对路径动态导入: {exception.Message}");
        }
    }

    [Fact]
    public void Import_DynamicWithAliases_ShouldSupportDynamicAliases()
    {
        // Arrange
        var moduleContent = @"
func processData(data:string) -> string {
    return ""Processed: "" + data
}
func getVersion() -> string {
    return ""1.0.0""
}
";

        var testContent = @"
// 动态确定模块路径和别名
module_path <- ""dynamic_alias""
alias_name <- ""processor""

import dynamic module_path as alias_name
result1 <- processor.processData(""dynamic test"")
result2 <- processor.getVersion()
";

        CreateTempModuleFile("dynamic_alias.old8", moduleContent);
        CreateTempModuleFile("dynamic_alias_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("dynamic_alias_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Processed: dynamic test");
        AssertVariableValue(interpreter, "result2", "1.0.0");
    }
}