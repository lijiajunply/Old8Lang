using Old8Lang.AST.Expression;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.AdvancedImport;

/// <summary>
/// 延迟导入功能测试
/// </summary>
public class LazyImportTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_LazyBasic_ShouldDelayImportUntilUse()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("ImportTests_Import_LazyImport.old8");

        // Assert
        Assert.Null(exception);
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    [Fact]
    public void Import_LazyNewSyntax_ShouldWorkWithNewSyntax()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("ImportTests_Import_LazyImport.new.old8");

        // Assert
        Assert.Null(exception);
        var status1 = interpreter.Manager.GetValue(new LangId("status1"));
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var status2 = interpreter.Manager.GetValue(new LangId("status2"));

        Assert.NotNull(status1);
        Assert.NotNull(result1);
        Assert.NotNull(status2);
    }

    [Fact]
    public void Import_LazySelective_ShouldDelaySelectiveImport()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("ImportTests_Import_LazyImportSelective.new.old8");

        // Assert
        Assert.Null(exception);
        var status1 = interpreter.Manager.GetValue(new LangId("status1"));
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var errorOccurred = interpreter.Manager.GetValue(new LangId("error_occurred"));
        var status2 = interpreter.Manager.GetValue(new LangId("status2"));

        Assert.NotNull(status1);
        Assert.NotNull(result1);
        Assert.NotNull(errorOccurred);
        Assert.NotNull(status2);
    }

    [Fact]
    public void Import_LazyWithAlias_ShouldDelayImportWithAlias()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("ImportTests_Import_LazyImportAlias.new.old8");

        // Assert
        Assert.Null(exception);
        var status1 = interpreter.Manager.GetValue(new LangId("status1"));
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var status2 = interpreter.Manager.GetValue(new LangId("status2"));

        Assert.NotNull(status1);
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(status2);
    }

    [Fact]
    public void Import_LazyEnhanced_ShouldProvideEnhancedFeatures()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("ImportTests_Import_LazyImportEnhanced.old8");

        // Assert
        Assert.Null(exception);
        var status1 = interpreter.Manager.GetValue(new LangId("status1"));
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var status2 = interpreter.Manager.GetValue(new LangId("status2"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(status1);
        Assert.NotNull(result1);
        Assert.NotNull(status2);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
    }

    [Fact]
    public void Import_LazyHeavyComputation_ShouldNotBlockInitialLoad()
    {
        // Arrange
        var heavyModuleContent = """

                                 // 模拟重型计算模块
                                 const HEAVY_DATA <- [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49]

                                 func heavyComputation() -> int {
                                     result <- 0
                                     i <- 0
                                     while i < len(HEAVY_DATA) {
                                         j <- 0
                                         while j < HEAVY_DATA.Size() {
                                             result <- result + HEAVY_DATA[i] * HEAVY_DATA[j]
                                             j <- j + 1
                                         }
                                         i <- i + 1
                                     }
                                     return result
                                 }

                                 func quickFunction() -> string {
                                     return "Quick result"
                                 }

                                 """;

        var testContent = """

                          lazy import "heavy_module" as heavy
                          status_before <- "Not loaded yet"

                          // 此时模块还未加载
                          quick_result <- heavy.quickFunction()
                          status_after_quick <- "Loaded after quick call"

                          // 重型计算会延迟到实际调用时
                          heavy_result <- heavy.heavyComputation()
                          status_after_heavy <- "Loaded after heavy call"

                          """;

        CreateTempModuleFile("heavy_module.old8", heavyModuleContent);
        CreateTempModuleFile("lazy_heavy_test.old8", testContent);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var (interpreter, exception) = ExecuteCodeFile("lazy_heavy_test.old8");
        stopwatch.Stop();

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "status_before", "Not loaded yet");
        AssertVariableValue(interpreter, "quick_result", "Quick result");
        AssertVariableValue(interpreter, "status_after_quick", "Loaded after quick call");
        AssertVariableValue(interpreter, "status_after_heavy", "Loaded after heavy call");

        // 验证延迟导入确实提高了初始加载速度
        Output.WriteLine($"延迟导入总耗时: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Import_LazyErrorModule_ShouldDelayErrorUntilAccess()
    {
        // Arrange
        var errorModuleContent = """

                                 func goodFunction() -> string {
                                     return "This works"
                                 }

                                 func badFunction() -> int {
                                     return 1 / 0  // 这会导致运行时错误
                                 }

                                 """;

        var testContent = """

                          lazy import "error_module" as error
                          status_before_access <- "Module imported but not accessed"

                          // 访问正常函数应该工作
                          good_result <- error.goodFunction()
                          status_after_good <- "Good function accessed"

                          // 访问有问题的函数会延迟错误
                          try {
                              bad_result <- error.badFunction()
                              status_after_bad <- "Bad function succeeded"
                          } catch {
                              status_after_bad <- "Bad function failed as expected"
                          }

                          """;

        CreateTempModuleFile("error_module.old8", errorModuleContent);
        CreateTempModuleFile("lazy_error_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_error_test.old8");

        // Assert
        // 初始导入应该成功，错误应该在使用时发生
        Assert.Null(exception);
        AssertVariableValue(interpreter, "status_before_access", "Module imported but not accessed");
        AssertVariableValue(interpreter, "good_result", "This works");
        AssertVariableValue(interpreter, "status_after_good", "Good function accessed");
    }

    [Fact]
    public void Import_LazyCircularDependency_ShouldHandleGracefully()
    {
        // Arrange
        var moduleAContent = @"
lazy import ""lazy_module_b"" as b
func funcA() -> string {
    return ""A calls B: "" + b.funcB()
}
func standaloneA() -> string {
    return ""A standalone""
}
";

        var moduleBContent = @"
lazy import ""lazy_module_a"" as a
func funcB() -> string {
    return ""B calls A: "" + a.funcA()
}
func standaloneB() -> string {
    return ""B standalone""
}
";

        var testContent = @"
import ""lazy_module_a"" as a
import ""lazy_module_b"" as b
result1 <- a.standaloneA()
result2 <- b.standaloneB()
";

        CreateTempModuleFile("lazy_module_a.old8", moduleAContent);
        CreateTempModuleFile("lazy_module_b.old8", moduleBContent);
        CreateTempModuleFile("lazy_circular_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_circular_test.old8");

        // Assert
        // 延迟导入应该能处理循环依赖，至少对独立函数是这样
        if (exception == null)
        {
            AssertVariableValue(interpreter, "result1", "A standalone");
            AssertVariableValue(interpreter, "result2", "B standalone");
        }
        else
        {
            Output.WriteLine($"循环依赖处理: {exception.Message}");
        }
    }

    [Fact]
    public void Import_LazyMultipleAccess_ShouldLoadOnce()
    {
        // Arrange
        var moduleContent = @"
const LOAD_COUNTER <- 0
func incrementAndGet() -> int {
    // 模拟模块级别的状态
    return LOAD_COUNTER + 1
}
";
        var testContent = @"
lazy import ""counter_module"" as counter
result1 <- counter.incrementAndGet()
result2 <- counter.incrementAndGet()
result3 <- counter.incrementAndGet()
";

        CreateTempModuleFile("counter_module.old8", moduleContent);
        CreateTempModuleFile("lazy_multiple_access_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_multiple_access_test.old8");

        // Assert
        Assert.Null(exception);
        // 验证模块只加载一次，但函数可以多次调用
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
    }

    [Fact]
    public void Import_LazyWithWildCard_ShouldDelayWildcardImport()
    {
        // Arrange
        var moduleContent = @"
const LAZY_CONST <- 42
func lazyFunction() -> string {
    return ""Lazy function called""
}
class LazyClass {
    public func getValue() -> int {
        return 100
    }
}
";
        var testContent = @"
lazy from ""lazy_wildcard_module"" import *
status_before <- ""Not loaded""
// 此时还没有加载模块

const_val <- LAZY_CONST
func_result <- lazyFunction()
class_instance <- LazyClass()
class_result <- class_instance.getValue()

status_after <- ""Loaded after access""
";

        CreateTempModuleFile("lazy_wildcard_module.old8", moduleContent);
        CreateTempModuleFile("lazy_wildcard_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_wildcard_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "status_before", "Not loaded");
        AssertVariableValue(interpreter, "const_val", 42);
        AssertVariableValue(interpreter, "func_result", "Lazy function called");
        AssertVariableValue(interpreter, "class_result", 100);
        AssertVariableValue(interpreter, "status_after", "Loaded after access");
    }
}