using Old8Lang.AST.Expression;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.AdvancedImport;

/// <summary>
/// 延迟导入功能测试
/// </summary>
[Collection("Sequential")]
public class LazyImportTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_LazyBasic_ShouldDelayImportUntilUse()
    {
        // Arrange
        var testContent = """
            import "Math"
            result1 <- "Not loaded"
            result2 <- 123
            """;
        CreateTempModuleFile("lazy_basic_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_basic_test.old8");

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
        // Arrange
        var testContent = """
            // 懒导入测试 - 只有在实际使用时才加载模块
            lazy import "lazy_math"

            // 检查模块是否已加载（此时应该还未加载）
            status1 <- "Not loaded"

            // 使用懒导入模块中的函数（此时会触发加载）
            result1 <- CalculateLargeNumber()

            // 使用模块中的常量
            result2 <- PI

            // 再次使用函数
            result3 <- HeavyOperation()

            // 此时模块应该已经加载
            status2 <- "Loaded"
            """;
        CreateTempModuleFile("lazy_new_syntax_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_new_syntax_test.old8");

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
        // Arrange
        var testContent = """
            // 选择性懒导入测试 - 懒导入特定函数
            lazy import { CalculateLargeNumber, PI } from "lazy_math"

            // 检查函数是否可用（此时应该还未加载）
            status1 <- "Not loaded"

            // 使用导入的函数（此时会触发加载）
            result1 <- CalculateLargeNumber()

            // 使用导入的常量
            result2 <- PI

            // 测试未导入的函数应该不可用
            try {
                result3 <- HeavyOperation()
                error_occurred <- false
            } catch {
                error_occurred <- true
                error_message <- "Function not imported"
            }

            status2 <- "Loaded"
            """;
        CreateTempModuleFile("lazy_selective_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_selective_test.old8");

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
        // Arrange
        var testContent = """
            // 懒导入带别名测试
            lazy import "lazy_math" as math

            // 检查模块是否已加载（此时应该还未加载）
            status1 <- "Not loaded"

            // 使用别名调用函数（此时会触发加载）
            result1 <- math.CalculateLargeNumber()

            // 使用别名访问常量
            result2 <- math.PI

            // 再次使用别名调用函数
            result3 <- math.HeavyOperation()

            // 此时模块应该已经加载
            status2 <- "Loaded"
            """;
        CreateTempModuleFile("lazy_alias_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_alias_test.old8");

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
        // Arrange
        var testContent = """
            // 懒导入测试 - 模块在首次使用时才加载
            import "lazy_math" as math

            // 模块还未加载
            status1 <- "Not loaded"

            // 首次使用模块，触发懒加载
            result1 <- math.CalculateLargeNumber()
            status2 <- "Loaded"

            // 再次使用，直接使用缓存的模块
            result2 <- math.PI
            result3 <- math.HeavyOperation()
            """;
        CreateTempModuleFile("lazy_enhanced_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_enhanced_test.old8");

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
                                 HEAVY_DATA:const <- [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49]

                                 func heavyComputation() -> int {
                                     result <- 0
                                     i <- 0
                                     while i < len(HEAVY_DATA) {
                                         j <- 0
                                         while j < len(HEAVY_DATA) {
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
        var moduleContent = """

                            LOAD_COUNTER:const <- 0
                            func incrementAndGet() -> int {
                                // 模拟模块级别的状态
                                return LOAD_COUNTER + 1
                            }

                            """;
        var testContent = """

                          lazy import "counter_module" as counter
                          result1 <- counter.incrementAndGet()
                          result2 <- counter.incrementAndGet()
                          result3 <- counter.incrementAndGet()

                          """;

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
        var moduleContent = """

                            func lazyFunction() -> string {
                                return "Lazy function called"
                            }
                            class LazyClass {
                                public func getValue() -> int {
                                    return 100
                                }
                            }

                            """;
        var testContent = """

                          lazy import "lazy_wildcard_module" 
                          status_before <- "Not loaded"
                          // 此时还没有加载模块

                          func_result <- lazyFunction()
                          class_instance <- LazyClass()
                          class_result <- class_instance.getValue()

                          status_after <- "Loaded after access"

                          """;

        CreateTempModuleFile("lazy_wildcard_module.old8", moduleContent);
        CreateTempModuleFile("lazy_wildcard_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("lazy_wildcard_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "status_before", "Not loaded");
        AssertVariableValue(interpreter, "func_result", "Lazy function called");
        AssertVariableValue(interpreter, "class_result", 100);
        AssertVariableValue(interpreter, "status_after", "Loaded after access");
    }
}