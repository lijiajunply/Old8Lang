using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 测试 params 可变参数在解释器模式下的行为
/// </summary>
[Collection("Sequential")]
public class ParamsInterpreterTests
{
    [Fact]
    public void TestParamsWithNoArguments()
    {
        // 测试不传入任何可变参数
        var code = @"
func sum(params args:array<int>) -> int {
    result <- 0
    for arg in args {
        result <- result + arg
    }
    return result
}

result <- sum()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        var intValue = Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, intValue.Value);
    }

    [Fact]
    public void TestParamsWithMultipleArguments()
    {
        // 测试传入多个可变参数
        var code = @"
func sum(params args:array<int>) -> int {
    result <- 0
    for arg in args {
        result <- result + arg
    }
    return result
}

result <- sum(1, 2, 3, 4, 5)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        var intValue = Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, intValue.Value); // 1 + 2 + 3 + 4 + 5 = 15
    }

    [Fact]
    public void TestParamsWithRegularParametersAndNoVarArgs()
    {
        // 测试普通参数 + 不传可变参数
        var code = @"
func format(prefix:string, params args:array<string>) -> string {
    result <- prefix
    for arg in args {
        result <- result + ""_"" + arg
    }
    return result
}

result <- format(""start"")
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        var stringValue = Assert.IsType<StringLangValue>(result);
        Assert.Equal("start", stringValue.Value);
    }

    [Fact]
    public void TestParamsWithRegularParametersAndVarArgs()
    {
        // 测试普通参数 + 可变参数
        var code = @"
func format(prefix:string, params args:array<string>) -> string {
    result <- prefix
    for arg in args {
        result <- result + ""_"" + arg
    }
    return result
}

result <- format(""start"", ""a"", ""b"", ""c"")
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        var stringValue = Assert.IsType<StringLangValue>(result);
        Assert.Equal("start_a_b_c", stringValue.Value);
    }

    [Fact]
    public void TestParamsArrayLength()
    {
        // 测试访问 params 数组的长度
        var code = @"
func getCount(params items:array<int>) -> int {
    return len(items)
}

result <- getCount(10, 20, 30)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        var intValue = Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, intValue.Value);
    }
}