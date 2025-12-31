using Xunit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Advanced;

/// <summary>
/// 本地方法绑定测试 - 测试 Native 语句绑定 C# 方法
/// </summary>
[Collection("Sequential")]
public class NativeMethodBindingTests
{
    #region Native 方法基础测试

    /// <summary>
    /// 测试 Native 语句绑定基础方法
    /// </summary>
    [Fact]
    public void Run_NativeMethodBasicBinding_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func GetEnvironmentVariable(name: string) -> string: ""System.Environment::GetEnvironmentVariable""

path <- GetEnvironmentVariable(""PATH"")
result <- path != null";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    /// <summary>
    /// 测试 Native 语句绑定静态方法
    /// </summary>
    [Fact]
    public void Run_NativeStaticMethodBinding_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func MathMax(a: double, b: double) -> double: ""System.Math::Max""

result <- MathMax(10.5, 20.3)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(20.3, ((DoubleLangValue)result).Value);
    }

    /// <summary>
    /// 测试 Native 语句绑定泛型方法
    /// </summary>
    [Fact]
    public void Run_NativeGenericMethodBinding_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func ParseInt(s: string) -> int: ""System.Int32::Parse""

result <- ParseInt(""42"")";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    #endregion

    #region Native 方法参数测试

    /// <summary>
    /// 测试 Native 方法多参数调用
    /// </summary>
    [Fact]
    public void Run_NativeMethodMultipleParameters_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func StringFormat(format: string, arg0: any, arg1: any) -> string: ""System.String::Format""

result <- StringFormat(""{0} + {1}"", 10, 20)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("10 + 20", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试 Native 方法无参数调用
    /// </summary>
    [Fact]
    public void Run_NativeMethodNoParameters_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func GetCurrentDateTime() -> any: ""System.DateTime::get_Now""

dateTime <- GetCurrentDateTime()
result <- dateTime != null";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    /// <summary>
    /// 测试 Native 方法不同类型参数
    /// </summary>
    [Fact]
    public void Run_NativeMethodDifferentParameterTypes_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func MathPow(base: double, exponent: double) -> double: ""System.Math::Pow""

result <- MathPow(2.0, 3.0)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(8.0, ((DoubleLangValue)result).Value);
    }

    #endregion

    #region Native 方法返回值测试

    #endregion

    #region Native 方法异常处理

    #endregion
}
