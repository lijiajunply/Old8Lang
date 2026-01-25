using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.BasicImport;

/// <summary>
/// 别名导入功能测试
/// </summary>
[Collection("Sequential")]
public class AliasImportTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_ModuleWithAlias_ShouldUseAliasCorrectly()
    {
        // Arrange
        var testContent = """
            import "Math" as m
            result <- m.sqrt(25)
            """;
        CreateTempModuleFile("with_alias_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("with_alias_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 5.0);
    }

    [Fact]
    public void Import_FunctionWithAlias_ShouldCallFunctionViaAlias()
    {
        // Arrange
        var moduleContent = @"
func calculateArea(radius:double) -> double {
    return 3.14159 * radius * radius
}
func calculatePerimeter(radius:double) -> double {
    return 2 * 3.14159 * radius
}
";
        var testContent = @"
import ""geometry"" as geo
area <- geo.calculateArea(5.0)
perimeter <- geo.calculatePerimeter(5.0)
";

        CreateTempModuleFile("geometry.old8", moduleContent);
        CreateTempModuleFile("geometry_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("geometry_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "area", 78.53975);
        AssertVariableValue(interpreter, "perimeter", 31.4159);
    }

    [Fact]
    public void Import_MultipleAliases_ShouldWorkIndependently()
    {
        // Arrange
        var mathContent = @"
func add(a:double, b:double) -> double { return a + b }
func multiply(a:double, b:double) -> double { return a * b }
";

        var stringContent = @"
func concat(str1:string, str2:string) -> string { return str1 + str2 }
func repeat(str:string, n:int) -> string {
    result <- str
    i <- 1
    while i < n {
        result <- result + str
        i <- i + 1
    }
    return result
}
";

        var testContent = @"
import ""math_module"" as m
import ""string_module"" as s
math_result <- m.add(3.0, 4.0)
string_result <- s.repeat(""Hello"", 3)
";

        CreateTempModuleFile("math_module.old8", mathContent);
        CreateTempModuleFile("string_module.old8", stringContent);
        CreateTempModuleFile("multi_alias_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("multi_alias_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "math_result", 7.0);
        AssertVariableValue(interpreter, "string_result", "HelloHelloHello");
    }

    [Fact]
    public void Import_FunctionAlias_ShouldCreateFunctionAlias()
    {
        // Arrange
        var testContent = """
            import "Math"
            angle <- GetPi() / 4
            result <- Sin(angle) / Cos(angle)
            """;
        CreateTempModuleFile("alias_function_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("alias_function_test.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(1.0, ((DoubleLangValue)result).Value, 0.1);
    }

    [Fact]
    public void Import_AliasWithSameNameAsExisting_ShouldNotConflict()
    {
        // Arrange
        var moduleContent = """

                            func processData(data:double) -> double {
                                return data * 2
                            }

                            """;
        var testContent = """

                          func processData(data:string) -> string {
                              return "local: " + data
                          }

                          import "process_module" as proc
                          local_result <- processData("test")
                          module_result <- proc.processData(5.0)

                          """;

        CreateTempModuleFile("process_module.old8", moduleContent);
        CreateTempModuleFile("alias_conflict_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("alias_conflict_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "local_result", "local: test");
        AssertVariableValue(interpreter, "module_result", 10.0);
    }

    [Theory]
    [InlineData("m", "import \"math1\" as m")]
    [InlineData("math_lib", "import \"math1\" as math_lib")]
    [InlineData("_private", "import \"math1\" as _private")]
    [InlineData("module123", "import \"math1\" as module123")]
    public void Import_DifferentAliasNames_ShouldWork(string aliasName, string importStatement)
    {
        // Arrange
        var moduleContent = @"
func getValue() -> int { return 42 }
";
        var testContent = $@"
{importStatement}
result <- {aliasName}.getValue()
";

        CreateTempModuleFile("math1.old8", moduleContent);
        CreateTempModuleFile($"alias_{aliasName}_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile($"alias_{aliasName}_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 42);
    }

    [Fact]
    public void Import_ChainedAliasAccess_ShouldWork()
    {
        // Arrange
        var nestedModuleContent = """

                                  func getValue() -> int { return 100 }

                                  """;

        var parentModuleContent = """

                                  import "nested_module" as nested
                                  func getNestedValue() -> int {
                                      return nested.getValue()
                                  }

                                  """;

        var testContent = """

                          import "parent_module" as parent
                          result <- parent.getNestedValue()

                          """;

        CreateTempModuleFile("nested_module.old8", nestedModuleContent);
        CreateTempModuleFile("parent_module.old8", parentModuleContent);
        CreateTempModuleFile("chained_alias_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("chained_alias_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", 100);
    }
}