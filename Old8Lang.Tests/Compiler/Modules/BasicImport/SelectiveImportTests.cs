using Old8Lang.AST.Expression;
using Old8Lang.Error;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.BasicImport;

/// <summary>
/// 选择性导入功能测试
/// </summary>
[Collection("Sequential")]
public class SelectiveImportTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_SelectiveFunctions_ShouldImportOnlySpecifiedFunctions()
    {
        // Arrange
        var testContent = """
            // 选择导入测试 - 从模块中导入特定功能
            import { CalculateLargeNumber, PI } from "lazy_math"

            // 直接使用导入的函数，不需要模块前缀
            result1 <- CalculateLargeNumber()
            result2 <- PI

            // 测试未导入的函数应该不可用（会报错）
            try {
                result3 <- HeavyOperation()
                error_occurred <- false
            } catch {
                error_occurred <- true
                error_message <- "Function not imported"
            }
            """;
        CreateTempModuleFile("selective_import_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("selective_import_test.old8");

        // Assert
        Assert.Null(exception);
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var errorOccurred = interpreter.Manager.GetValue(new LangId("error_occurred"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(errorOccurred);
    }

    [Fact]
    public void Import_SelectiveFromModule_ShouldImportSpecificItems()
    {
        // Arrange
        var testContent = """
            // 从模块选择导入测试
            import CalculateLargeNumber, HeavyOperation from "lazy_math"

            // 直接使用导入的函数，无需模块前缀
            result1 <- CalculateLargeNumber()
            result2 <- HeavyOperation()

            // 测试未导入的常量应该不可用
            try {
                result3 <- PI
                error_occurred <- false
            } catch {
                error_occurred <- true
                error_message <- "Constant not imported"
            }
            """;
        CreateTempModuleFile("selective_from_module_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("selective_from_module_test.old8");

        // Assert
        Assert.Null(exception);
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    [Fact]
    public void Import_MixedSelective_ShouldImportFunctionsAndConstants()
    {
        // Arrange
        var moduleContent = """

                            func add(a:double, b:double) -> double {
                                return a + b
                            }
                            func multiply(a:double, b:double) -> double {
                                return a * b
                            }

                            """;
        var testContent = @"

                          import {add, multiply} from ""mixed_module""
                          result1 <- add(2.0, 3.0)
                          result2 <- multiply(4.0, 5.0)

                          ";

        CreateTempModuleFile("mixed_module.old8", moduleContent);
        CreateTempModuleFile("mixed_selective_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("mixed_selective_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 5.0);
        AssertVariableValue(interpreter, "result2", 20.0);
    }

    [Fact]
    public void Import_SelectiveNonExistentFunction_ShouldThrowError()
    {
        // Arrange
        var moduleContent = """

                            func existingFunction() -> int { return 42 }

                            """;
        var testContent = """

                          import {nonExistentFunction} from "module"
                          result <- nonExistentFunction()

                          """;

        CreateTempModuleFile("module.old8", moduleContent);
        CreateTempModuleFile("selective_error_test.old8", testContent);

        // Act & Assert
        AssertExecutionThrows("selective_error_test.old8", typeof(ImportError));
    }

    [Theory]
    [InlineData("func1", "single function")]
    [InlineData("func1, func2", "two functions")]
    [InlineData("func1, func2, func3, func4, func5", "multiple functions")]
    public void Import_SelectiveMultipleFunctions_ShouldImportAll(string importList, string description)
    {
        // Arrange
        var moduleContent = @"
func func1() -> int { return 1 }
func func2() -> int { return 2 }
func func3() -> int { return 3 }
func func4() -> int { return 4 }
func func5() -> int { return 5 }
";
        var testContent = $@"
import {{{importList}}} from ""multi_func_module""
result <- ""imported ""
";

        CreateTempModuleFile("multi_func_module.old8", moduleContent);
        CreateTempModuleFile($"multi_func_test_{description.Replace(' ', '_')}.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile($"multi_func_test_{description.Replace(' ', '_')}.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "imported ");
    }

    [Fact]
    public void Import_SelectiveWithAlias_ShouldImportWithNewName()
    {
        // Arrange
        var moduleContent = @"
func veryLongFunctionName() -> string {
    return ""success""
}
anotherLongName <- 100
";
        var testContent = @"
import {veryLongFunctionName as shortFunc, anotherLongName as shortNum} from ""alias_module""
result1 <- shortFunc()
result2 <- shortNum
";

        CreateTempModuleFile("alias_module.old8", moduleContent);
        CreateTempModuleFile("selective_alias_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("selective_alias_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "success");
        AssertVariableValue(interpreter, "result2", 100);
    }

    [Fact]
    public void Import_SelectiveClasses_ShouldImportClassDefinitions()
    {
        // Arrange
        var moduleContent = """

                            class Calculator {
                                public func add(a:int, b:int) -> int {
                                    return a + b
                                }

                                public func multiply(a:int, b:int) -> int {
                                    return a * b
                                }
                            }

                            class Helper {
                                public func getName() -> string {
                                    return "Helper"
                                }
                            }

                            """;
        var testContent = """

                          import {Calculator} from "class_module"
                          calc <- Calculator()
                          result1 <- calc.add(3, 4)
                          result2 <- calc.multiply(5, 6)

                          // Helper should not be available
                          // helper <- Helper()  // This should cause an error

                          """;

        CreateTempModuleFile("class_module.old8", moduleContent);
        CreateTempModuleFile("selective_class_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("selective_class_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 7);
        AssertVariableValue(interpreter, "result2", 30);
    }

    [Fact]
    public void Import_SelectiveSameFunctionFromMultipleModules_ShouldNotConflict()
    {
        // Arrange
        var module1Content = @"
func process(data:int) -> string {
    return ""Module1: "" + data.ToStr()
}
";

        var module2Content = @"
func process(data:int) -> string {
    return ""Module2: "" + data.ToStr()
}
";

        var testContent = @"
import {process as process1} from ""module1""
import {process as process2} from ""module2""
result1 <- process1(42)
result2 <- process2(24)
";

        CreateTempModuleFile("module1.old8", module1Content);
        CreateTempModuleFile("module2.old8", module2Content);
        CreateTempModuleFile("selective_no_conflict_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("selective_no_conflict_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Module1: 42");
        AssertVariableValue(interpreter, "result2", "Module2: 24");
    }

    [Fact]
    public void Import_SelectiveNestedModule_ShouldImportFromNestedPath()
    {
        // Arrange
        CreateTempModuleFile("nested/submodule/module.old8", """

                                                             func deepFunction() -> string {
                                                                 return "Deep import successful"
                                                             }

                                                             """);

        var testContent = """

                           import { deepFunction } from "./nested/submodule/module"
                           result <- deepFunction()

                           """;

        CreateTempModuleFile("nested_selective_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("nested_selective_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "Deep import successful");
    }
}