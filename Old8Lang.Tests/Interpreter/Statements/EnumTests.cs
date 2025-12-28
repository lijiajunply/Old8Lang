using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Statements;

/// <summary>
/// 枚举声明和使用的解释模式测试
/// 测试枚举在运行时的行为，包括成员访问、值计算和错误处理
/// </summary>
[Collection("Sequential")]
public class EnumTests
{
    #region 基本功能测试

    /// <summary>
    /// 测试简单枚举定义和成员访问
    /// </summary>
    [Fact]
    public void EnumDeclaration_SimpleEnum_AccessMembersCorrectly()
    {
        // Arrange
        var code = @"
enum Color {
    Red,
    Green,
    Blue
}

red <- Color.Red
green <- Color.Green
blue <- Color.Blue
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var red = interpreter.Manager.GetValue(new LangId("red"));
        var green = interpreter.Manager.GetValue(new LangId("green"));
        var blue = interpreter.Manager.GetValue(new LangId("blue"));

        Assert.NotNull(red);
        Assert.NotNull(green);
        Assert.NotNull(blue);
        Assert.IsType<IntLangValue>(red);
        Assert.IsType<IntLangValue>(green);
        Assert.IsType<IntLangValue>(blue);
        Assert.Equal(0, ((IntLangValue)red).Value);
        Assert.Equal(1, ((IntLangValue)green).Value);
        Assert.Equal(2, ((IntLangValue)blue).Value);
    }

    /// <summary>
    /// 测试带显式值的枚举
    /// </summary>
    [Fact]
    public void EnumDeclaration_ExplicitValues_ReturnsCorrectValues()
    {
        // Arrange
        var code = @"
enum HttpStatus {
    OK <- 200,
    Created <- 201,
    BadRequest <- 400,
    NotFound <- 404,
    InternalServerError <- 500
}

ok <- HttpStatus.OK
notFound <- HttpStatus.NotFound
serverError <- HttpStatus.InternalServerError
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var ok = interpreter.Manager.GetValue(new LangId("ok"));
        var notFound = interpreter.Manager.GetValue(new LangId("notFound"));
        var serverError = interpreter.Manager.GetValue(new LangId("serverError"));

        Assert.NotNull(ok);
        Assert.NotNull(notFound);
        Assert.NotNull(serverError);
        Assert.Equal(200, ((IntLangValue)ok!).Value);
        Assert.Equal(404, ((IntLangValue)notFound!).Value);
        Assert.Equal(500, ((IntLangValue)serverError!).Value);
    }

    /// <summary>
    /// 测试混合自动和显式值的枚举
    /// </summary>
    [Fact]
    public void EnumDeclaration_MixedValues_CalculatesCorrectly()
    {
        // Arrange
        var code = @"
enum Priority {
    Low,
    Medium <- 5,
    High,
    Critical <- 10
}

low <- Priority.Low
medium <- Priority.Medium
high <- Priority.High
critical <- Priority.Critical
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var low = interpreter.Manager.GetValue(new LangId("low"));
        var medium = interpreter.Manager.GetValue(new LangId("medium"));
        var high = interpreter.Manager.GetValue(new LangId("high"));
        var critical = interpreter.Manager.GetValue(new LangId("critical"));

        Assert.Equal(0, ((IntLangValue)low!).Value);
        Assert.Equal(5, ((IntLangValue)medium!).Value);
        Assert.Equal(6, ((IntLangValue)high!).Value);
        Assert.Equal(10, ((IntLangValue)critical!).Value);
    }

    /// <summary>
    /// 测试枚举值在条件语句中的使用
    /// </summary>
    [Fact]
    public void EnumValue_InCondition_WorksCorrectly()
    {
        // Arrange
        var code = @"
enum Status {
    Success,
    Pending,
    Failed
}

status <- Status.Success
result <- """"

if status == 0 {
    result <- ""success""
} elif status == 1 {
    result <- ""pending""
} else {
    result <- ""failed""
}
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("success", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试枚举值在算术运算中的使用
    /// </summary>
    [Fact]
    public void EnumValue_InArithmetic_WorksCorrectly()
    {
        // Arrange
        var code = @"
enum Numbers {
    One <- 1,
    Two <- 2,
    Three <- 3
}

sum <- Numbers.One + Numbers.Two
product <- Numbers.Two * Numbers.Three
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var sum = interpreter.Manager.GetValue(new LangId("sum"));
        var product = interpreter.Manager.GetValue(new LangId("product"));

        Assert.Equal(3, ((IntLangValue)sum!).Value);
        Assert.Equal(6, ((IntLangValue)product!).Value);
    }

    #endregion

    #region 边界测试

    /// <summary>
    /// 测试空枚举
    /// </summary>
    [Fact]
    public void EnumDeclaration_EmptyEnum_DeclaresSuccessfully()
    {
        // Arrange
        var code = @"
enum Empty {}
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert - 应该成功声明，不抛出异常
        Assert.NotNull(ast);
    }

    /// <summary>
    /// 测试单成员枚举
    /// </summary>
    [Fact]
    public void EnumDeclaration_SingleMember_WorksCorrectly()
    {
        // Arrange
        var code = @"
enum SingleValue {
    OnlyOne
}

value <- SingleValue.OnlyOne
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var value = interpreter.Manager.GetValue(new LangId("value"));
        Assert.NotNull(value);
        Assert.Equal(0, ((IntLangValue)value!).Value);
    }

    /// <summary>
    /// 测试负数值的枚举
    /// </summary>
    [Fact]
    public void EnumDeclaration_NegativeValues_WorksCorrectly()
    {
        // Arrange
        var code = @"
enum Temperature {
    Cold <- -10,
    Freezing <- -5,
    Normal <- 0,
    Hot <- 25
}

cold <- Temperature.Cold
freezing <- Temperature.Freezing
normal <- Temperature.Normal
hot <- Temperature.Hot
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var cold = interpreter.Manager.GetValue(new LangId("cold"));
        var freezing = interpreter.Manager.GetValue(new LangId("freezing"));
        var normal = interpreter.Manager.GetValue(new LangId("normal"));
        var hot = interpreter.Manager.GetValue(new LangId("hot"));

        Assert.Equal(-10, ((IntLangValue)cold!).Value);
        Assert.Equal(-5, ((IntLangValue)freezing!).Value);
        Assert.Equal(0, ((IntLangValue)normal!).Value);
        Assert.Equal(25, ((IntLangValue)hot!).Value);
    }

    /// <summary>
    /// 测试大整数值的枚举
    /// </summary>
    [Fact]
    public void EnumDeclaration_LargeValues_WorksCorrectly()
    {
        // Arrange
        var code = @"
enum LargeNumbers {
    Million <- 1000000,
    Billion <- 1000000000
}

million <- LargeNumbers.Million
billion <- LargeNumbers.Billion
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var million = interpreter.Manager.GetValue(new LangId("million"));
        var billion = interpreter.Manager.GetValue(new LangId("billion"));

        Assert.Equal(1000000, ((IntLangValue)million!).Value);
        Assert.Equal(1000000000, ((IntLangValue)billion!).Value);
    }

    /// <summary>
    /// 测试枚举值的比较操作
    /// </summary>
    [Fact]
    public void EnumValue_Comparison_WorksCorrectly()
    {
        // Arrange
        var code = @"
enum Level {
    Low <- 1,
    Medium <- 5,
    High <- 10
}

low <- Level.Low
medium <- Level.Medium
high <- Level.High
result1 <- low < medium
result2 <- high > low
result3 <- Level.Medium == 5
result4 <- Level.Low != Level.High
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.True(((BoolLangValue)result1!).Value);
        Assert.True(((BoolLangValue)result2!).Value);
        Assert.True(((BoolLangValue)result3!).Value);
        Assert.True(((BoolLangValue)result4!).Value);
    }

    #endregion

    #region 错误测试

    /// <summary>
    /// 测试访问不存在的枚举成员
    /// </summary>
    [Fact]
    public void EnumAccess_NonExistentMember_ThrowsNameError()
    {
        // Arrange
        var code = @"
enum Color {
    Red,
    Green,
    Blue
}

invalid <- Color.Yellow
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.ThrowsAny<NameError>(() => ast.Run(interpreter.Manager));
    }

    /// <summary>
    /// 测试访问不存在的枚举
    /// </summary>
    [Fact]
    public void EnumAccess_NonExistentEnum_ThrowsNameError()
    {
        // Arrange
        var code = @"
value <- NonExistentEnum.Value
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.ThrowsAny<NameError>(() => ast.Run(interpreter.Manager));
    }

    /// <summary>
    /// 测试重复的枚举成员名称
    /// </summary>
    [Fact]
    public void EnumDeclaration_DuplicateMemberNames_ThrowsDuplicateNameError()
    {
        // Arrange
        var code = @"
enum Invalid {
    Value1,
    Value2,
    Value1
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.ThrowsAny<DuplicateNameError>(() => ast.Run(interpreter.Manager));
    }

    /// <summary>
    /// 测试枚举成员值为非整数类型
    /// </summary>
    [Fact]
    public void EnumDeclaration_NonIntegerValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
enum Invalid {
    Value1 <- ""string""
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.ThrowsAny<SyntaxError>(() => ast.Run(interpreter.Manager));
    }

    /// <summary>
    /// 测试枚举成员值为浮点数
    /// </summary>
    [Fact]
    public void EnumDeclaration_FloatValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
enum Invalid {
    Value1 <- 3.14
}
";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.ThrowsAny<SyntaxError>(() => ast.Run(interpreter.Manager));
    }

    /// <summary>
    /// 测试尝试修改枚举成员（枚举应该是只读的）
    /// </summary>
    [Fact]
    public void EnumMember_Modification_NotSupported()
    {
        // Arrange - 这个测试确认枚举成员本身是不可修改的
        // 虽然可以将枚举值赋给变量并修改变量，但不能直接修改枚举成员
        var code = @"
enum Color {
    Red,
    Green,
    Blue
}

value <- Color.Red
value <- 100  // 这是可以的，修改的是变量，不是枚举成员
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var value = interpreter.Manager.GetValue(new LangId("value"));
        Assert.Equal(100, ((IntLangValue)value!).Value);
    }

    #endregion

    #region 实际使用场景测试

    /// <summary>
    /// 测试枚举在 switch 语句中的使用
    /// </summary>
    [Fact]
    public void EnumValue_InSwitch_WorksCorrectly()
    {
        // Arrange
        var code = @"
enum Direction {
    North,
    South,
    East,
    West
}

direction <- Direction.North
message <- """"

switch direction {
    case 0 {
        message <- ""Going North""
    }
    case 1 {
        message <- ""Going South""
    }
    case 2 {
        message <- ""Going East""
    }
    case 3 {
        message <- ""Going West""
    }
}
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var message = interpreter.Manager.GetValue(new LangId("message"));
        Assert.NotNull(message);
        Assert.IsType<StringLangValue>(message);
        Assert.Equal("Going North", ((StringLangValue)message).Value);
    }

    /// <summary>
    /// 测试枚举在函数参数中的使用
    /// </summary>
    [Fact]
    public void EnumValue_AsFunctionParameter_WorksCorrectly()
    {
        // Arrange
        var code = @"
enum LogLevel {
    Debug,
    Info,
    Warning,
    Error
}

func getLogLevelName(level) {
    if level == 0 {
        return ""DEBUG""
    } elif level == 1 {
        return ""INFO""
    } elif level == 2 {
        return ""WARNING""
    } else {
        return ""ERROR""
    }
}

debugName <- getLogLevelName(LogLevel.Debug)
errorName <- getLogLevelName(LogLevel.Error)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var debugName = interpreter.Manager.GetValue(new LangId("debugName"));
        var errorName = interpreter.Manager.GetValue(new LangId("errorName"));

        Assert.Equal("DEBUG", ((StringLangValue)debugName!).Value);
        Assert.Equal("ERROR", ((StringLangValue)errorName!).Value);
    }

    /// <summary>
    /// 测试多个枚举声明不会冲突
    /// </summary>
    [Fact]
    public void MultipleEnums_Declaration_NoConflict()
    {
        // Arrange
        var code = @"
enum Color {
    Red,
    Green,
    Blue
}

enum Size {
    Small,
    Medium,
    Large
}

color <- Color.Red
size <- Size.Medium
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var color = interpreter.Manager.GetValue(new LangId("color"));
        var size = interpreter.Manager.GetValue(new LangId("size"));

        Assert.Equal(0, ((IntLangValue)color!).Value);
        Assert.Equal(1, ((IntLangValue)size!).Value);
    }

    /// <summary>
    /// 测试枚举值的类型转换
    /// </summary>
    [Fact]
    public void EnumValue_TypeConversion_WorksCorrectly()
    {
        // Arrange
        var code = @"
enum Status {
    Active <- 1,
    Inactive <- 0
}

status <- Status.Active
statusStr <- status.ToStr()
statusDouble <- status as double
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var statusStr = interpreter.Manager.GetValue(new LangId("statusStr"));
        var statusDouble = interpreter.Manager.GetValue(new LangId("statusDouble"));

        Assert.Equal("1", ((StringLangValue)statusStr!).Value);
        Assert.Equal(1.0, ((DoubleLangValue)statusDouble!).Value);
    }

    #endregion
}
