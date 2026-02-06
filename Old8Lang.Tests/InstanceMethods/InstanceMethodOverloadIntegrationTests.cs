using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.InstanceMethods;

/// <summary>
/// 实例方法重载集成测试 - 测试实际代码执行
/// </summary>
public class InstanceMethodOverloadIntegrationTests
{
    [Fact]
    public void StringSubstring_OneParameter_ShouldWork()
    {
        // Arrange
        var code = @"
            str <- ""Hello World""
            result <- str.Substring(6)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("World", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringSubstring_TwoParameters_ShouldWork()
    {
        // Arrange
        var code = @"
            str <- ""Hello World""
            result <- str.Substring(0, 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello", ((StringLangValue)result).Value);
    }

    [Fact]
    public void StringSubstring_OverloadResolution_ShouldSelectCorrectMethod()
    {
        // Arrange - 测试重载解析是否正确选择方法
        var code = @"
            str <- ""Hello World""
            result1 <- str.Substring(6)
            result2 <- str.Substring(0, 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("World", ((StringLangValue)result1).Value);
        Assert.Equal("Hello", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void ListAdd_SingleElement_ShouldWork()
    {
        // Arrange
        var code = @"
            list <- {1, 2, 3}
            list.Add(4)
            result <- len(list)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(4, ((IntLangValue)result).Value);
    }

    [Fact]
    public void InstanceMethod_ChainedCalls_ShouldWork()
    {
        // Arrange
        var code = @"
            str <- ""  Hello World  ""
            result <- str.Trim().Substring(0, 5)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello", ((StringLangValue)result).Value);
    }

    [Fact]
    public void InstanceMethod_MultipleOverloads_ShouldResolveCorrectly()
    {
        // Arrange - 测试多个重载在同一代码中的使用
        var code = @"
            str <- ""Hello World""
            sub1 <- str.Substring(6)      // 使用单参数重载
            sub2 <- str.Substring(0, 5)   // 使用双参数重载
            sub3 <- str.Substring(6)      // 再次使用单参数重载
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var sub1 = interpreter.Manager.GetValue(new LangId("sub1"));
        var sub2 = interpreter.Manager.GetValue(new LangId("sub2"));
        var sub3 = interpreter.Manager.GetValue(new LangId("sub3"));

        Assert.NotNull(sub1);
        Assert.NotNull(sub2);
        Assert.NotNull(sub3);
        Assert.Equal("World", ((StringLangValue)sub1).Value);
        Assert.Equal("Hello", ((StringLangValue)sub2).Value);
        Assert.Equal("World", ((StringLangValue)sub3).Value);
    }
}
