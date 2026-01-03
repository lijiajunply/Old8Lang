using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 测试 params 可变参数在编译器模式下的行为
/// </summary>
[Collection("Sequential")]
public class ParamsCompilerTests
{
    [Fact]
    public void TestParamsWithNoArguments()
    {
        // 测试不传入任何可变参数
        var code = @"
func sum(params args:array<int>) -> int {
    result:int <- 0
    for arg in args {
        result <- result + arg
    }
    return result
}

a:int <- sum()
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // 编译成功
        Assert.NotNull(compiledAction);

        // 执行不应抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestParamsWithMultipleArguments()
    {
        // 测试传入多个可变参数
        var code = @"
func sum(params args:array<int>) -> int {
    result:int <- 0
    for arg in args {
        result <- result + arg
    }
    return result
}

a:int <- sum(1, 2, 3, 4, 5)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestParamsWithRegularParametersAndNoVarArgs()
    {
        // 测试普通参数 + 不传可变参数
        var code = @"
func format(prefix:string, params args:array<string>) -> string {
    result:string <- prefix
    for arg in args {
        result <- result + ""_"" + arg
    }
    return result
}

a:string <- format(""start"")
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestParamsWithRegularParametersAndVarArgs()
    {
        // 测试普通参数 + 可变参数
        var code = @"
func format(prefix:string, params args:array<string>) -> string {
    result:string <- prefix
    for arg in args {
        result <- result + ""_"" + arg
    }
    return result
}

a:string <- format(""start"", ""a"", ""b"", ""c"")
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestParamsArrayLength()
    {
        // 测试访问 params 数组的长度
        var code = @"
func getCount(params items:array<int>) -> int {
    return len(items)
}

a:int <- getCount(10, 20, 30)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
