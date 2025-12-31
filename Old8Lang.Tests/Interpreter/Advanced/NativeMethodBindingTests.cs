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

    /// <summary>
    /// 测试 Native 方法返回整数
    /// </summary>
    [Fact]
    public void Run_NativeMethodReturnInteger_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func MathAbs(value: int) -> int: ""System.Math::Abs""

result <- MathAbs(-42)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 Native 方法返回字符串
    /// </summary>
    [Fact]
    public void Run_NativeMethodReturnString_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func ToUpperCase(s: string) -> string: ""System.String::ToUpper""

result <- ToUpperCase(""hello"")";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("HELLO", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试 Native 方法返回布尔值
    /// </summary>
    [Fact]
    public void Run_NativeMethodReturnBoolean_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func StringIsNullOrEmpty(s: string) -> bool: ""System.String::IsNullOrEmpty""

result <- StringIsNullOrEmpty("""")";
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
    /// 测试 Native 方法返回 void
    /// </summary>
    [Fact]
    public void Run_NativeMethodReturnVoid_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
native func GCCollect() -> void: ""System.GC::Collect""

GCCollect()
result <- 42";
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

    #region Native 方法异常处理

    /// <summary>
    /// 测试 Native 方法抛出异常
    /// </summary>
    [Fact]
    public void Run_NativeMethodThrowsException_HandledCorrectly()
    {
        // Arrange
        var code = @"
native func ParseInt(s: string) -> int: ""System.Int32::Parse""

result <- 0
try {
    result <- ParseInt(""invalid"")
} catch (e) {
    result <- -1
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(-1, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 Native 方法参数类型不匹配
    /// </summary>
    [Fact]
    public void Run_NativeMethodWrongParameterType_HandledCorrectly()
    {
        // Arrange
        var code = @"
native func MathSqrt(value: double) -> double: ""System.Math::Sqrt""

result <- 0.0
try {
    result <- MathSqrt(-1.0)
} catch (e) {
    result <- -999.0
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<DoubleLangValue>(result);
        // Math.Sqrt(-1) returns NaN, not exception, so this should execute normally
        Assert.True(double.IsNaN(((DoubleLangValue)result).Value));
    }

    /// <summary>
    /// 测试 Native 方法空引用异常
    /// </summary>
    [Fact]
    public void Run_NativeMethodNullReference_HandledCorrectly()
    {
        // Arrange
        var code = @"
native func StringLength(s: string) -> int: ""System.String::get_Length""

result <- 0
nullString <- null
try {
    result <- StringLength(nullString)
} catch (e) {
    result <- -1
}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(-1, ((IntLangValue)result).Value);
    }

    #endregion
}

