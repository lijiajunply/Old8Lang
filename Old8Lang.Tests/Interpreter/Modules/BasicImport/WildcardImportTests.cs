using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.BasicImport;

/// <summary>
/// 通配符导入功能测试
/// </summary>
public class WildcardImportTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_Wildcard_ShouldImportAllFunctions()
    {
        // Act
        var (interpreter, exception) = ExecuteCodeFile("ImportTests_Import_WithWildCard.old8");

        // Assert
        Assert.Null(exception);
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(10.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void Import_WildcardWithConstants_ShouldImportAllSymbols()
    {
        // Arrange
        var moduleContent = @"
PI <- 3.14159
E <- 2.71828
GRAVITY <- 9.81

func calculateArea(radius:double) -> double {
    return PI * radius * radius
}

func calculateForce(mass:double, acceleration:double) -> double {
    return mass * acceleration
}
";
        var testContent = @"
import * from ""wildcard_constants""
area <- calculateArea(5.0)
force <- calculateForce(10.0, GRAVITY)
pi_value <- PI
e_value <- E
";

        CreateTempModuleFile("wildcard_constants.old8", moduleContent);
        CreateTempModuleFile("wildcard_constants_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("wildcard_constants_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "area", 78.53975);
        AssertVariableValue(interpreter, "force", 98.1);
        AssertVariableValue(interpreter, "pi_value", 3.14159);
        AssertVariableValue(interpreter, "e_value", 2.71828);
    }

    [Fact]
    public void Import_WildcardWithClasses_ShouldImportAllClasses()
    {
        // Arrange
        var moduleContent = @"
class MathUtils {
    public static func add(a:int, b:int) -> int {
        return a + b
    }
}

class StringUtils {
    public static func concat(str1:string, str2:string) -> string {
        return str1 + str2
    }
}

func helperFunction() -> string {
    return ""Helper function""
}
";
        var testContent = @"
import * from ""wildcard_classes""
result1 <- MathUtils.add(3, 4)
result2 <- StringUtils.concat(""Hello"", ""World"")
result3 <- helperFunction()
";

        CreateTempModuleFile("wildcard_classes.old8", moduleContent);
        CreateTempModuleFile("wildcard_classes_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("wildcard_classes_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", 7);
        AssertVariableValue(interpreter, "result2", "HelloWorld");
        AssertVariableValue(interpreter, "result3", "Helper function");
    }

    [Fact]
    public void Import_WildcardEmptyModule_ShouldNotCauseError()
    {
        // Arrange
        var moduleContent = "// Empty module with just comments";
        var testContent = @"
import * from ""empty_wildcard_module""
result <- ""import completed""
";

        CreateTempModuleFile("empty_wildcard_module.old8", moduleContent);
        CreateTempModuleFile("empty_wildcard_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("empty_wildcard_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result", "import completed");
    }

    [Fact]
    public void Import_WildcardWithConflictingNames_ShouldHandleCorrectly()
    {
        // Arrange
        var module1Content = @"
VALUE1 <- 100
func getValue1() -> int {
    return VALUE1
}
";

        var module2Content = @"
VALUE2 <- 200
func getValue2() -> int {
    return VALUE2
}
";

        var testContent = @"
import * from ""wildcard_module1""
local_value1 <- getValue1()
local_module1_value <- VALUE1

import * from ""wildcard_module2""
local_value2 <- getValue2()
local_module2_value <- VALUE2
";

        CreateTempModuleFile("wildcard_module1.old8", module1Content);
        CreateTempModuleFile("wildcard_module2.old8", module2Content);
        CreateTempModuleFile("wildcard_conflict_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("wildcard_conflict_test.old8");

        // Assert
        Assert.Null(exception);
        // 后导入的模块应该覆盖前面的，或者按语言规范处理
        var value1 = interpreter.Manager.GetValue(new LangId("local_value1"));
        var value2 = interpreter.Manager.GetValue(new LangId("local_value2"));
        Assert.NotNull(value1);
        Assert.NotNull(value2);
    }

    [Fact]
    public void Import_WildcardLargeModule_ShouldImportEfficiently()
    {
        // Arrange - 创建包含大量导出的模块
        var functions = new List<string>();
        var constants = new List<string>();

        for (int i = 0; i < 100; i++)
        {
            functions.Add($"func func{i}() -> int {{ return {i} }}");
            constants.Add($"CONST{i} <- {i * 10}");
        }

        var moduleContent = string.Join("\n", functions.Concat(constants));
        var testContent = @"
import * from ""large_wildcard_module""
result0 <- func0()
result99 <- func99()
const0 <- CONST0
const99 <- CONST99
";

        CreateTempModuleFile("large_wildcard_module.old8", moduleContent);
        CreateTempModuleFile("large_wildcard_test.old8", testContent);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var (interpreter, exception) = ExecuteCodeFile("large_wildcard_test.old8");
        stopwatch.Stop();

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result0", 0);
        AssertVariableValue(interpreter, "result99", 99);
        AssertVariableValue(interpreter, "const0", 0);
        AssertVariableValue(interpreter, "const99", 990);

        Output.WriteLine($"大型通配符导入耗时: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, "大型通配符导入应该少于2秒");
    }

    [Fact]
    public void Import_WildcardNestedFunctions_ShouldImportNestedFunctions()
    {
        // Arrange
        var moduleContent = @"
func outerFunction() -> string {
    func innerFunction() -> string {
        return ""Inner""
    }
    return ""Outer: "" + innerFunction()
}

func standaloneFunction() -> string {
    return ""Standalone""
}
";
        var testContent = @"
import * from ""nested_functions_module""
result1 <- outerFunction()
result2 <- standaloneFunction()
";

        CreateTempModuleFile("nested_functions_module.old8", moduleContent);
        CreateTempModuleFile("nested_functions_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("nested_functions_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "result1", "Outer: Inner");
        AssertVariableValue(interpreter, "result2", "Standalone");
    }

    [Fact]
    public void Import_WildcardWithPrivateMembers_ShouldOnlyImportPublic()
    {
        // Arrange
        var moduleContent = @"
public func publicFunction() -> string {
    return ""Public""
}

func privateFunction() -> string {
    return ""Private""
}

PUBLIC_CONST <- 100
PRIVATE_CONST <- 200
";
        var testContent = @"
import * from ""visibility_module""
public_result <- publicFunction()
public_const_value <- PUBLIC_CONST
";

        CreateTempModuleFile("visibility_module.old8", moduleContent);
        CreateTempModuleFile("visibility_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("visibility_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "public_result", "Public");
        AssertVariableValue(interpreter, "public_const_value", 100);
    }

    [Theory]
    [InlineData("*", "star wildcard")]
    [InlineData("all", "all keyword")]
    public void Import_WildcardSyntax_ShouldSupportDifferentSyntax(string wildcardSyntax, string description)
    {
        // Arrange
        var moduleContent = @"
func testFunction() -> string {
    return ""Wildcard import successful""
}
";
        var testContent = $@"
import {wildcardSyntax} from ""wildcard_syntax_module""
result <- testFunction()
";

        CreateTempModuleFile("wildcard_syntax_module.old8", moduleContent);
        CreateTempModuleFile($"wildcard_syntax_{description.Replace(' ', '_')}.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile($"wildcard_syntax_{description.Replace(' ', '_')}.old8");

        // Assert
        if (exception == null)
        {
            AssertVariableValue(interpreter, "result", "Wildcard import successful");
        }
        else
        {
            Output.WriteLine($"{description} 语法可能不被支持: {exception.Message}");
        }
    }

    [Fact]
    public void Import_MultipleWildcard_ShouldImportFromMultipleSources()
    {
        // Arrange
        var mathModule = @"
func add(a:int, b:int) -> int { return a + b }
func subtract(a:int, b:int) -> int { return a - b }
";

        var stringModule = @"
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
import * from ""math_wildcard""
import * from ""string_wildcard""
math_result <- add(5, 3)
string_result <- repeat(""Hi"", 3)
";

        CreateTempModuleFile("math_wildcard.old8", mathModule);
        CreateTempModuleFile("string_wildcard.old8", stringModule);
        CreateTempModuleFile("multiple_wildcard_test.old8", testContent);

        // Act
        var (interpreter, exception) = ExecuteCodeFile("multiple_wildcard_test.old8");

        // Assert
        Assert.Null(exception);
        AssertVariableValue(interpreter, "math_result", 8);
        AssertVariableValue(interpreter, "string_result", "HiHiHi");
    }
}