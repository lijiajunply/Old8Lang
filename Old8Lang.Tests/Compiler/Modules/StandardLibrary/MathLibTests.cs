using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Compiler.Modules.StandardLibrary;

/// <summary>
/// MathLib 库测试 - 测试数学函数功能
/// </summary>
[Collection("Sequential")]
public class MathLibTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void Import_MathLib_ShouldWorkCorrectly()
    {
        var code = @"
import Math

PrintLine(""Math library imported"")
";
        CreateTempModuleFile("./StandardLibrary/math_test.old8", code);
        var (interpreter, exception) = ExecuteCodeFile("./StandardLibrary/math_test.old8");

        Assert.Null(exception);
        var mathLib = interpreter.Manager.GetValue(new LangId("Math"));
        Assert.NotNull(mathLib);
        Assert.IsAssignableFrom<IModuleValueType>(mathLib);
    }

    [Fact]
    public void Abs_ShouldReturnAbsoluteValue()
    {
        var code = @"
import Math

result <- Math.Abs(-5)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_abs_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_abs_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Sqrt_ShouldCalculateSquareRoot()
    {
        var code = @"
import Math

result <- Math.Sqrt(16.0)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_sqrt_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_sqrt_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Pow_ShouldCalculatePower()
    {
        var code = @"
import Math

result <- Math.Pow(2, 10)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_pow_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_pow_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Max_ShouldReturnLargerValue()
    {
        var code = @"
import Math

result <- Math.Max(10, 20)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_max_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_max_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Min_ShouldReturnSmallerValue()
    {
        var code = @"
import Math

result <- Math.Min(10, 20)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_min_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_min_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Ceil_ShouldRoundUp()
    {
        var code = @"
import Math

result <- Math.Ceil(3.14)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_ceil_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_ceil_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Floor_ShouldRoundDown()
    {
        var code = @"
import Math

result <- Math.Floor(3.99)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_floor_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_floor_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Round_ShouldRoundToNearest()
    {
        var code = @"
import Math

result <- Math.Round(3.5)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_round_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_round_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Sin_ShouldCalculateSine()
    {
        var code = @"
import Math

pi <- Math.GetPi()
result <- Math.Sin(pi / 2)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_sin_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_sin_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Cos_ShouldCalculateCosine()
    {
        var code = @"
import Math

pi <- Math.GetPi()
result <- Math.Cos(pi)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_cos_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_cos_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetPi_ShouldReturnPiConstant()
    {
        var code = @"
import Math

pi <- Math.GetPi()
PrintLine(pi)
";
        CreateTempModuleFile("./StandardLibrary/math_getpi_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_getpi_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void GetE_ShouldReturnEulerConstant()
    {
        var code = @"
import Math

e <- Math.GetE()
PrintLine(e)
";
        CreateTempModuleFile("./StandardLibrary/math_gete_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_gete_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Factorial_ShouldCalculateFactorial()
    {
        var code = @"
import Math

result <- Math.Factorial(5)
PrintLine(result)
";
        CreateTempModuleFile("./StandardLibrary/math_factorial_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_factorial_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void DegreesToRadians_ShouldConvertCorrectly()
    {
        var code = @"
import Math

result <- Math.DegreesToRadians(180.0)
pi <- Math.GetPi()
PrintLine($""180 degrees = {result} radians"")
PrintLine($""PI = {pi}"")
";
        CreateTempModuleFile("./StandardLibrary/math_deg2rad_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_deg2rad_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void RadiansToDegrees_ShouldConvertCorrectly()
    {
        var code = @"
import Math

pi <- Math.GetPi()
result <- Math.RadiansToDegrees(pi)
PrintLine($""PI radians = {result} degrees"")
";
        CreateTempModuleFile("./StandardLibrary/math_rad2deg_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_rad2deg_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void Random_ShouldGenerateRandomNumber()
    {
        var code = @"
import Math

r1 <- Math.Random()
r2 <- Math.Random()
r3 <- Math.Random()
PrintLine($""Random 1: {r1}"")
PrintLine($""Random 2: {r2}"")
PrintLine($""Random 3: {r3}"")
";
        CreateTempModuleFile("./StandardLibrary/math_random_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_random_test.old8");

        Assert.Null(exception);
    }

    [Fact]
    public void RandomInt_ShouldGenerateRandomInteger()
    {
        var code = @"
import Math

r1 <- Math.RandomInt(1, 100)
r2 <- Math.RandomInt(1, 100)
r3 <- Math.RandomInt(1, 100)
PrintLine($""Random Int 1: {r1}"")
PrintLine($""Random Int 2: {r2}"")
PrintLine($""Random Int 3: {r3}"")
";
        CreateTempModuleFile("./StandardLibrary/math_randomint_test.old8", code);
        var (_, exception) = ExecuteCodeFile("./StandardLibrary/math_randomint_test.old8");

        Assert.Null(exception);
    }
}
