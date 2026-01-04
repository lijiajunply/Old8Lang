using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Functions;

/// <summary>
/// 测试默认参数在编译器模式下的行为
/// </summary>
[Collection("Sequential")]
public class DefaultParametersCompilerTests
{
    [Fact]
    public void TestBasicDefaultParameter()
    {
        // 测试基本的默认参数
        var code = @"
func greet(name:string, message: ""Hello"") -> string {
    return message + "", "" + name
}

result:string <- greet(""Alice"")
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestOverrideDefaultParameter()
    {
        // 测试覆盖默认参数
        var code = @"
func greet(name:string, message: ""Hello"") -> string {
    return message + "", "" + name
}

result:string <- greet(""Bob"", ""Hi"")
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestMultipleDefaultParameters()
    {
        // 测试多个默认参数
        var code = @"
func calculate(x:int, y: 10, z: 5) -> int {
    return x + y + z
}

result1:int <- calculate(1)
result2:int <- calculate(1, 20)
result3:int <- calculate(1, 20, 30)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestDefaultParameterTypeInference_Int()
    {
        // 测试 int 类型的默认参数推断
        var code = @"
func add(x:int, y: 10) -> int {
    return x + y
}

result:int <- add(5)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestDefaultParameterTypeInference_Double()
    {
        // 测试 double 类型的默认参数推断
        var code = @"
func multiply(x:double, y: 2.5) -> double {
    return x * y
}

result:double <- multiply(4.0)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestDefaultParameterTypeInference_Bool()
    {
        // 测试 bool 类型的默认参数推断
        var code = @"
func toggle(value:bool, invert: true) -> bool {
    if invert {
        return not value
    }
    return value
}

result:bool <- toggle(false)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestMixedRequiredAndDefaultParameters()
    {
        // 测试混合必需参数和默认参数
        var code = @"
func format(prefix:string, suffix: "" (default)"", delimiter: ""-"") -> string {
    return prefix + delimiter + suffix
}

result1:string <- format(""start"")
result2:string <- format(""start"", ""end"")
result3:string <- format(""start"", ""end"", ""_"")
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestAllParametersHaveDefaults()
    {
        // 测试所有参数都有默认值
        var code = @"
func create(x: 1, y: 2, z: 3) -> int {
    return x + y + z
}

result:int <- create()
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void TestDefaultParameterPartialOverride()
    {
        // 测试部分覆盖默认参数
        var code = @"
func test(a:int, b: 10, c: 20, d: 30) -> int {
    return a + b + c + d
}

result1:int <- test(1)
result2:int <- test(1, 15)
result3:int <- test(1, 15, 25)
result4:int <- test(1, 15, 25, 35)
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        Assert.NotNull(compiledAction);

        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
